// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.DirectoryServices.AccountManagement;
using System.Security;
using System.Text.Json;
using System.Text.Json.Serialization;

using Json.Schema;
using Json.Schema.Generation;

namespace OpenDsc.Resource.Windows.Group;

[DscResource("OpenDsc.Windows/Group", "0.1.0", Description = "Manage local Windows groups", Tags = ["windows", "group", "security"])]
[ExitCode(0, Description = "Success")]
[ExitCode(1, Exception = typeof(Exception), Description = "Error")]
[ExitCode(2, Exception = typeof(JsonException), Description = "Invalid JSON")]
[ExitCode(3, Exception = typeof(SecurityException), Description = "Access denied")]
[ExitCode(4, Exception = typeof(ArgumentException), Description = "Invalid argument")]
[ExitCode(5, Exception = typeof(UnauthorizedAccessException), Description = "Unauthorized access")]
[ExitCode(6, Exception = typeof(PrincipalExistsException), Description = "Group already exists")]
public sealed class Resource(JsonSerializerContext context) : DscResource<Schema>(context), IGettable<Schema>, ISettable<Schema>, IDeletable<Schema>, IExportable<Schema>
{
    public override string GetSchema()
    {
        var config = new SchemaGeneratorConfiguration()
        {
            PropertyNameResolver = PropertyNameResolvers.CamelCase
        };

        var builder = new JsonSchemaBuilder().FromType<Schema>(config);
        builder.Schema("https://json-schema.org/draft/2020-12/schema");
        var schema = builder.Build();

        return JsonSerializer.Serialize(schema);
    }

    public Schema Get(Schema? instance)
    {
        ArgumentNullException.ThrowIfNull(instance);

        using var context = new PrincipalContext(ContextType.Machine);
        var group = GroupPrincipal.FindByIdentity(context, IdentityType.SamAccountName, instance.GroupName);

        if (group == null)
        {
            return new Schema()
            {
                GroupName = instance.GroupName,
                Exist = false
            };
        }

        using (group)
        {
            var members = new List<string>();
            foreach (var member in group.Members)
            {
                members.Add(member.SamAccountName);
                member.Dispose();
            }

            return new Schema()
            {
                GroupName = group.SamAccountName,
                Description = group.Description,
                Members = members.Count > 0 ? [.. members] : null
            };
        }
    }

    public SetResult<Schema>? Set(Schema? instance)
    {
        ArgumentNullException.ThrowIfNull(instance);

        using var context = new PrincipalContext(ContextType.Machine);
        var group = GroupPrincipal.FindByIdentity(context, IdentityType.SamAccountName, instance.GroupName);

        if (group == null)
        {
            group = new GroupPrincipal(context)
            {
                SamAccountName = instance.GroupName,
                Description = instance.Description
            };

            if (instance.Members != null && instance.Members.Length > 0)
            {
                AddMembers(context, group, instance.Members);
            }

            group.Save();
            group.Dispose();
            return null;
        }

        using (group)
        {
            bool changed = false;
            var principalsToDispose = new List<Principal>();

            if (instance.Description != null && group.Description != instance.Description)
            {
                group.Description = instance.Description;
                changed = true;
            }

            if (instance.Members != null)
            {
                var currentMembers = new HashSet<string>(GetCurrentMembers(group), StringComparer.OrdinalIgnoreCase);
                var desiredMembers = new HashSet<string>(instance.Members, StringComparer.OrdinalIgnoreCase);

                if (instance.Purge == true)
                {
                    var toRemove = currentMembers.Except(desiredMembers).ToList();
                    foreach (var member in toRemove)
                    {
                        var principal = RemoveMember(context, group, member);
                        if (principal != null)
                        {
                            principalsToDispose.Add(principal);
                        }
                        changed = true;
                    }
                }

                var toAdd = desiredMembers.Except(currentMembers).ToList();
                foreach (var member in toAdd)
                {
                    var principal = AddMember(context, group, member);
                    principalsToDispose.Add(principal);
                    changed = true;
                }
            }

            if (changed)
            {
                group.Save();
            }

            foreach (var principal in principalsToDispose)
            {
                principal.Dispose();
            }
        }

        return null;
    }

    public void Delete(Schema? instance)
    {
        ArgumentNullException.ThrowIfNull(instance);

        using var context = new PrincipalContext(ContextType.Machine);
        var group = GroupPrincipal.FindByIdentity(context, IdentityType.SamAccountName, instance.GroupName);

        if (group != null)
        {
            using (group)
            {
                group.Delete();
            }
        }
    }

    public IEnumerable<Schema> Export(Schema? filter)
    {
        using var context = new PrincipalContext(ContextType.Machine);
        using var searcher = new PrincipalSearcher(new GroupPrincipal(context));

        foreach (var principal in searcher.FindAll())
        {
            if (principal is GroupPrincipal group)
            {
                using (group)
                {
                    var members = new List<string>();
                    foreach (var member in group.Members)
                    {
                        members.Add(member.SamAccountName);
                        member.Dispose();
                    }

                    yield return new Schema
                    {
                        GroupName = group.SamAccountName,
                        Description = group.Description,
                        Members = members.Count > 0 ? [.. members] : null
                    };
                }
            }
            else
            {
                principal.Dispose();
            }
        }
    }

    private static List<string> GetCurrentMembers(GroupPrincipal group)
    {
        var members = new List<string>();
        foreach (var member in group.Members)
        {
            members.Add(member.SamAccountName);
            member.Dispose();
        }
        return members;
    }

    private static Principal AddMember(PrincipalContext context, GroupPrincipal group, string memberName)
    {
        Principal? member;

        if (memberName.StartsWith("S-", StringComparison.OrdinalIgnoreCase))
        {
            member = Principal.FindByIdentity(context, IdentityType.Sid, memberName);
        }
        else if (memberName.Contains('@'))
        {
            member = Principal.FindByIdentity(context, IdentityType.UserPrincipalName, memberName);
        }
        else if (memberName.StartsWith("CN=", StringComparison.OrdinalIgnoreCase))
        {
            member = Principal.FindByIdentity(context, IdentityType.DistinguishedName, memberName);
        }
        else
        {
            member = UserPrincipal.FindByIdentity(context, IdentityType.SamAccountName, memberName);
            member ??= GroupPrincipal.FindByIdentity(context, IdentityType.SamAccountName, memberName);
        }

        if (member == null)
        {
            throw new ArgumentException($"Member '{memberName}' not found.");
        }

        group.Members.Add(member);
        return member;
    }

    private static void AddMembers(PrincipalContext context, GroupPrincipal group, string[] members)
    {
        foreach (var memberName in members)
        {
            AddMember(context, group, memberName);
        }
    }

    private static Principal? RemoveMember(PrincipalContext context, GroupPrincipal group, string memberName)
    {
        Principal? member;

        if (memberName.StartsWith("S-", StringComparison.OrdinalIgnoreCase))
        {
            member = Principal.FindByIdentity(context, IdentityType.Sid, memberName);
        }
        else if (memberName.Contains('@'))
        {
            member = Principal.FindByIdentity(context, IdentityType.UserPrincipalName, memberName);
        }
        else if (memberName.StartsWith("CN=", StringComparison.OrdinalIgnoreCase))
        {
            member = Principal.FindByIdentity(context, IdentityType.DistinguishedName, memberName);
        }
        else
        {
            member = UserPrincipal.FindByIdentity(context, IdentityType.SamAccountName, memberName);
            member ??= GroupPrincipal.FindByIdentity(context, IdentityType.SamAccountName, memberName);
        }

        if (member != null)
        {
            group.Members.Remove(member);
        }

        return member;
    }
}
