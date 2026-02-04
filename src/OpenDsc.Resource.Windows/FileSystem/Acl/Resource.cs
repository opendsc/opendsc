// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Security;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text.Json;
using System.Text.Json.Serialization;

using Json.Schema;
using Json.Schema.Generation;

using SysFileSystemRights = System.Security.AccessControl.FileSystemRights;

namespace OpenDsc.Resource.Windows.FileSystem.Acl;

[DscResource("OpenDsc.Windows.FileSystem/AccessControlList", "0.1.0", Description = "Manage Windows file and directory permissions", Tags = ["windows", "filesystem", "acl", "permissions", "security"])]
[ExitCode(0, Description = "Success")]
[ExitCode(1, Exception = typeof(Exception), Description = "Error")]
[ExitCode(2, Exception = typeof(JsonException), Description = "Invalid JSON")]
[ExitCode(3, Exception = typeof(SecurityException), Description = "Access denied")]
[ExitCode(4, Exception = typeof(ArgumentException), Description = "Invalid argument")]
[ExitCode(5, Exception = typeof(UnauthorizedAccessException), Description = "Unauthorized access")]
[ExitCode(6, Exception = typeof(FileNotFoundException), Description = "File or directory not found")]
[ExitCode(7, Exception = typeof(DirectoryNotFoundException), Description = "Directory not found")]
[ExitCode(8, Exception = typeof(IdentityNotMappedException), Description = "Identity not found")]
public sealed class Resource(JsonSerializerContext context) : DscResource<Schema>(context), IGettable<Schema>, ISettable<Schema>
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

        if (!File.Exists(instance.Path) && !Directory.Exists(instance.Path))
        {
            throw new FileNotFoundException($"The file or directory '{instance.Path}' does not exist.");
        }

        var fileInfo = new FileInfo(instance.Path);
        FileSecurity? fileSecurity = null;
        DirectorySecurity? directorySecurity = null;

        if ((fileInfo.Attributes & FileAttributes.Directory) == FileAttributes.Directory)
        {
            directorySecurity = new DirectoryInfo(instance.Path).GetAccessControl(AccessControlSections.Access | AccessControlSections.Owner | AccessControlSections.Group);
        }
        else
        {
            fileSecurity = fileInfo.GetAccessControl(AccessControlSections.Access | AccessControlSections.Owner | AccessControlSections.Group);
        }

        var security = (FileSystemSecurity)(fileSecurity ?? (object)directorySecurity!);

        var owner = security.GetOwner(typeof(SecurityIdentifier))?.Translate(typeof(NTAccount))?.Value;
        var group = security.GetGroup(typeof(SecurityIdentifier))?.Translate(typeof(NTAccount))?.Value;

        var accessRules = new List<AccessRule>();
        var authRules = security.GetAccessRules(true, true, typeof(SecurityIdentifier));

        foreach (FileSystemAccessRule rule in authRules)
        {
            var identity = rule.IdentityReference;
            if (identity is SecurityIdentifier sid)
            {
                identity = sid.Translate(typeof(NTAccount));
            }

            accessRules.Add(new AccessRule
            {
                Identity = identity.Value,
                Rights = EnumHelper.ConvertFromSystem(rule.FileSystemRights),
                InheritanceFlags = EnumHelper.ExpandFlags(rule.InheritanceFlags),
                PropagationFlags = EnumHelper.ExpandFlags(rule.PropagationFlags),
                AccessControlType = rule.AccessControlType
            });
        }

        return new Schema()
        {
            Path = instance.Path,
            Owner = owner,
            Group = group,
            AccessRules = [.. accessRules]
        };
    }

    public SetResult<Schema>? Set(Schema? instance)
    {
        ArgumentNullException.ThrowIfNull(instance);

        if (!File.Exists(instance.Path) && !Directory.Exists(instance.Path))
        {
            throw new FileNotFoundException($"The file or directory '{instance.Path}' does not exist.");
        }

        var fileInfo = new FileInfo(instance.Path);
        FileSecurity? fileSecurity = null;
        DirectorySecurity? directorySecurity = null;
        bool isDirectory = (fileInfo.Attributes & FileAttributes.Directory) == FileAttributes.Directory;

        if (isDirectory)
        {
            directorySecurity = new DirectoryInfo(instance.Path).GetAccessControl(AccessControlSections.All);
        }
        else
        {
            fileSecurity = fileInfo.GetAccessControl(AccessControlSections.All);
        }

        var security = (FileSystemSecurity)(fileSecurity ?? (object)directorySecurity!);
        bool changed = false;

        if (instance.Owner != null)
        {
            var ownerIdentity = ResolveIdentity(instance.Owner);
            var currentOwner = security.GetOwner(typeof(SecurityIdentifier));

            if (!ownerIdentity.Equals(currentOwner))
            {
                security.SetOwner(ownerIdentity);
                changed = true;
            }
        }

        if (instance.Group != null)
        {
            var groupIdentity = ResolveIdentity(instance.Group);
            var currentGroup = security.GetGroup(typeof(SecurityIdentifier));

            if (!groupIdentity.Equals(currentGroup))
            {
                security.SetGroup(groupIdentity);
                changed = true;
            }
        }

        if (instance.AccessRules != null)
        {
            var currentRules = GetCurrentAccessRules(security);
            var desiredRules = instance.AccessRules.Select(r => new
            {
                Identity = ResolveIdentity(r.Identity),
                Rights = EnumHelper.ConvertToSystem(EnumHelper.CombineFlags(r.Rights)),
                InheritanceFlags = EnumHelper.CombineFlags(r.InheritanceFlags),
                PropagationFlags = EnumHelper.CombineFlags(r.PropagationFlags),
                r.AccessControlType
            }).ToList();

            if (instance.Purge == true)
            {
                foreach (var currentRule in currentRules)
                {
                    var matches = desiredRules.Any(dr =>
                        dr.Identity.Equals(currentRule.Identity) &&
                        dr.Rights == currentRule.Rights &&
                        dr.InheritanceFlags == currentRule.InheritanceFlags &&
                        dr.PropagationFlags == currentRule.PropagationFlags &&
                        dr.AccessControlType == currentRule.AccessControlType);

                    if (!matches)
                    {
                        var sysRule = new FileSystemAccessRule(
                            currentRule.Identity,
                            currentRule.Rights,
                            currentRule.InheritanceFlags,
                            currentRule.PropagationFlags,
                            currentRule.AccessControlType);

                        security.RemoveAccessRule(sysRule);
                        changed = true;
                    }
                }
            }

            foreach (var desiredRule in desiredRules)
            {
                var matches = currentRules.Any(cr =>
                    cr.Identity.Equals(desiredRule.Identity) &&
                    cr.Rights == desiredRule.Rights &&
                    cr.InheritanceFlags == desiredRule.InheritanceFlags &&
                    cr.PropagationFlags == desiredRule.PropagationFlags &&
                    cr.AccessControlType == desiredRule.AccessControlType);

                if (!matches)
                {
                    var sysRule = new FileSystemAccessRule(
                        desiredRule.Identity,
                        desiredRule.Rights,
                        desiredRule.InheritanceFlags,
                        desiredRule.PropagationFlags,
                        desiredRule.AccessControlType);

                    security.AddAccessRule(sysRule);
                    changed = true;
                }
            }
        }

        if (changed)
        {
            if (isDirectory)
            {
                new DirectoryInfo(instance.Path).SetAccessControl((DirectorySecurity)security);
            }
            else
            {
                fileInfo.SetAccessControl((FileSecurity)security);
            }
        }

        return null;
    }

    private static IdentityReference ResolveIdentity(string identity)
    {
        if (identity.StartsWith("S-1-", StringComparison.OrdinalIgnoreCase))
        {
            return new SecurityIdentifier(identity);
        }

        try
        {
            var account = new NTAccount(identity);
            return (SecurityIdentifier)account.Translate(typeof(SecurityIdentifier));
        }
        catch (Exception ex)
        {
            throw new IdentityNotMappedException($"Unable to resolve identity '{identity}'.", ex);
        }
    }

    private static List<(IdentityReference Identity, SysFileSystemRights Rights, InheritanceFlags InheritanceFlags, PropagationFlags PropagationFlags, AccessControlType AccessControlType)> GetCurrentAccessRules(FileSystemSecurity security)
    {
        var rules = new List<(IdentityReference, SysFileSystemRights, InheritanceFlags, PropagationFlags, AccessControlType)>();
        var authRules = security.GetAccessRules(true, false, typeof(SecurityIdentifier));

        foreach (FileSystemAccessRule rule in authRules)
        {
            rules.Add((
                rule.IdentityReference,
                rule.FileSystemRights,
                rule.InheritanceFlags,
                rule.PropagationFlags,
                rule.AccessControlType
            ));
        }

        return rules;
    }
}
