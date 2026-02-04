// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Text.Json;
using System.Text.Json.Serialization;

using Json.Schema;
using Json.Schema.Generation;

namespace OpenDsc.Resource.Windows.UserRight;

[DscResource("OpenDsc.Windows/UserRight", "0.1.0",
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

    public Schema Get(Schema? instance)
    {
        ArgumentNullException.ThrowIfNull(instance);

        var principalSid = LsaHelper.ResolvePrincipalToSid(instance.Principal).Value;
        var hasRights = new List<UserRight>();

        foreach (var right in Enum.GetValues<UserRight>())
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

        return new Schema
        {
            Principal = instance.Principal,
            Rights = hasRights.ToArray()
        };
    }

    public SetResult<Schema>? Set(Schema? instance)
    {
        ArgumentNullException.ThrowIfNull(instance);

        var principalSid = LsaHelper.ResolvePrincipalToSid(instance.Principal).Value;

        foreach (var right in instance.Rights)
        {
            var currentPrincipals = LsaHelper.GetPrincipalsWithRight(right);

            var principalToSid = currentPrincipals.ToDictionary(
                p => p,
                p => LsaHelper.ResolvePrincipalToSid(p).Value,
                StringComparer.OrdinalIgnoreCase);

            var hasPrincipal = principalToSid.Values.Contains(principalSid, StringComparer.OrdinalIgnoreCase);

            if (!hasPrincipal)
            {
                LsaHelper.GrantRight(instance.Principal, right);
            }

            if (instance.Purge == true)
            {
                var toRemove = principalToSid
                    .Where(kvp => !kvp.Value.Equals(principalSid, StringComparison.OrdinalIgnoreCase))
                    .Select(kvp => kvp.Key)
                    .ToList();

                foreach (var principal in toRemove)
                {
                    LsaHelper.RevokeRight(principal, right);
                }
            }
        }

        return null;
    }

    public IEnumerable<Schema> Export(Schema? filter)
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
}
