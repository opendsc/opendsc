// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Text.Json;
using System.Text.Json.Serialization;

using Json.Schema;
using Json.Schema.Generation;

namespace OpenDsc.Resource.Windows.UserRight;

[DscResource("OpenDsc.Windows/UserRight", "0.1.0",
    SetReturn = SetReturn.State,
    Description = "Manage Windows user rights assignments",
    Tags = ["windows", "security", "rights", "privileges"])]
[ExitCode(0, Description = "Success")]
[ExitCode(1, Exception = typeof(Exception), Description = "Error")]
[ExitCode(2, Exception = typeof(JsonException), Description = "Invalid JSON")]
[ExitCode(3, Exception = typeof(System.Security.SecurityException), Description = "Access denied")]
[ExitCode(4, Exception = typeof(ArgumentException), Description = "Invalid argument")]
public sealed class Resource(JsonSerializerContext context)
    : DscResource<Schema>(context),
      IGettable<Schema>,
      ISettable<Schema>,
      IDeletable<Schema>,
      IExportable<Schema>
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

    public Schema Get(Schema instance)
    {
        if (instance.Rights == null || instance.Rights.Length == 0)
        {
            return new Schema
            {
                Principal = instance.Principal,
                Exist = false,
                Rights = []
            };
        }

        var principalSid = LsaHelper.ResolvePrincipalToSid(instance.Principal).Value;
        var hasRights = new List<UserRight>();

        foreach (var right in instance.Rights)
        {
            var principals = LsaHelper.GetPrincipalsWithRight(right);
            var hasPrincipal = principals.Any(p =>
            {
                var sid = LsaHelper.ResolvePrincipalToSid(p);
                return sid.Value.Equals(principalSid, StringComparison.OrdinalIgnoreCase);
            });

            if (hasPrincipal)
            {
                hasRights.Add(right);
            }
        }

        if (hasRights.Count == 0)
        {
            return new Schema
            {
                Principal = instance.Principal,
                Rights = instance.Rights,
                Exist = false
            };
        }

        return new Schema
        {
            Principal = instance.Principal,
            Rights = hasRights.ToArray()
        };
    }

    public SetResult<Schema>? Set(Schema instance)
    {
        if (instance.Rights == null || instance.Rights.Length == 0)
        {
            throw new ArgumentException("Rights array is required and must contain at least one right.");
        }

        var changed = false;
        var principalSid = LsaHelper.ResolvePrincipalToSid(instance.Principal).Value;

        foreach (var right in instance.Rights)
        {
            var currentPrincipals = LsaHelper.GetPrincipalsWithRight(right);
            var currentPrincipalSids = currentPrincipals.Select(p => LsaHelper.ResolvePrincipalToSid(p).Value).ToHashSet(StringComparer.OrdinalIgnoreCase);

            var hasPrincipal = currentPrincipalSids.Contains(principalSid);

            if (!hasPrincipal)
            {
                LsaHelper.GrantRight(instance.Principal, right);
                changed = true;
            }

            if (instance.Purge == true)
            {
                var toRemove = currentPrincipals.Where(p =>
                {
                    var sid = LsaHelper.ResolvePrincipalToSid(p).Value;
                    return !sid.Equals(principalSid, StringComparison.OrdinalIgnoreCase);
                }).ToList();

                foreach (var principal in toRemove)
                {
                    LsaHelper.RevokeRight(principal, right);
                    changed = true;
                }
            }
        }

        var actualState = Get(instance);

        if (changed && instance.Rights.Any(r => RequiresServiceRestart(r)))
        {
            actualState.Metadata = new Dictionary<string, object>
            {
                ["_restartRequired"] = new[]
                {
                    new { service = GetAffectedService(instance.Rights.First(r => RequiresServiceRestart(r))) }
                }
            };
        }

        return new SetResult<Schema>(actualState);
    }

    public void Delete(Schema instance)
    {
        if (instance.Rights == null || instance.Rights.Length == 0)
        {
            return;
        }

        foreach (var right in instance.Rights)
        {
            LsaHelper.RevokeRight(instance.Principal, right);
        }
    }

    public IEnumerable<Schema> Export()
    {
        var knownRights = Enum.GetValues<UserRight>();

        var principalRights = new Dictionary<string, List<UserRight>>(StringComparer.OrdinalIgnoreCase);

        foreach (var right in knownRights)
        {
            var principals = LsaHelper.GetPrincipalsWithRight(right);

            foreach (var principal in principals)
            {
                var normalizedPrincipal = LsaHelper.ResolvePrincipalToSid(principal).Value;

                if (!principalRights.ContainsKey(normalizedPrincipal))
                {
                    principalRights[normalizedPrincipal] = new List<UserRight>();
                }

                principalRights[normalizedPrincipal].Add(right);
            }
        }

        foreach (var kvp in principalRights)
        {
            var friendlyName = LsaHelper.TranslateSidToName(new System.Security.Principal.SecurityIdentifier(kvp.Key));

            yield return new Schema
            {
                Principal = friendlyName,
                Rights = kvp.Value.ToArray()
            };
        }
    }

    private static bool RequiresServiceRestart(UserRight right)
    {
        return right is UserRight.SeServiceLogonRight or
               UserRight.SeBatchLogonRight or
               UserRight.SeInteractiveLogonRight;
    }

    private static string GetAffectedService(UserRight right)
    {
        return right switch
        {
            UserRight.SeServiceLogonRight => "Services that run under affected accounts",
            UserRight.SeBatchLogonRight => "Task Scheduler",
            UserRight.SeInteractiveLogonRight => "Interactive logon sessions",
            _ => "Unknown"
        };
    }
}
