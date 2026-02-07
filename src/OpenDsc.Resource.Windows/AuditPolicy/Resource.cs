// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.ComponentModel;
using System.Security;
using System.Text.Json;
using System.Text.Json.Serialization;

using Json.Schema;
using Json.Schema.Generation;

namespace OpenDsc.Resource.Windows.AuditPolicy;

[DscResource("OpenDsc.Windows/AuditPolicy", "0.1.0", Description = "Manage Windows audit policy for system security event auditing", Tags = ["windows", "audit", "security"])]
[ExitCode(0, Description = "Success")]
[ExitCode(1, Exception = typeof(Exception), Description = "Error")]
[ExitCode(2, Exception = typeof(SecurityException), Description = "Access denied - requires SeSecurityPrivilege or AUDIT_SET_SYSTEM_POLICY access")]
[ExitCode(3, Exception = typeof(ArgumentException), Description = "Invalid parameter")]
[ExitCode(4, Exception = typeof(Win32Exception), Description = "Win32 API error")]
[ExitCode(5, Exception = typeof(UnknownSubcategoryException), Description = "Unknown audit subcategory name")]
public sealed class Resource(JsonSerializerContext context)
    : DscResource<Schema>(context),
      IGettable<Schema>,
      ISettable<Schema>
{
    private static readonly Dictionary<string, Guid> SubcategoryToGuid = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Security State Change"] = new Guid("0cce9210-69ae-11d9-bed3-505054503030"),
        ["Security System Extension"] = new Guid("0cce9211-69ae-11d9-bed3-505054503030"),
        ["System Integrity"] = new Guid("0cce9212-69ae-11d9-bed3-505054503030"),
        ["IPsec Driver"] = new Guid("0cce9213-69ae-11d9-bed3-505054503030"),
        ["Other System Events"] = new Guid("0cce9214-69ae-11d9-bed3-505054503030"),
        ["Logon"] = new Guid("0cce9215-69ae-11d9-bed3-505054503030"),
        ["Logoff"] = new Guid("0cce9216-69ae-11d9-bed3-505054503030"),
        ["Account Lockout"] = new Guid("0cce9217-69ae-11d9-bed3-505054503030"),
        ["IPsec Main Mode"] = new Guid("0cce9218-69ae-11d9-bed3-505054503030"),
        ["IPsec Quick Mode"] = new Guid("0cce9219-69ae-11d9-bed3-505054503030"),
        ["IPsec Extended Mode"] = new Guid("0cce921a-69ae-11d9-bed3-505054503030"),
        ["Special Logon"] = new Guid("0cce921b-69ae-11d9-bed3-505054503030"),
        ["Other Logon/Logoff Events"] = new Guid("0cce921c-69ae-11d9-bed3-505054503030"),
        ["File System"] = new Guid("0cce921d-69ae-11d9-bed3-505054503030"),
        ["Registry"] = new Guid("0cce921e-69ae-11d9-bed3-505054503030"),
        ["Kernel Object"] = new Guid("0cce921f-69ae-11d9-bed3-505054503030"),
        ["SAM"] = new Guid("0cce9220-69ae-11d9-bed3-505054503030"),
        ["Certification Services"] = new Guid("0cce9221-69ae-11d9-bed3-505054503030"),
        ["Application Generated"] = new Guid("0cce9222-69ae-11d9-bed3-505054503030"),
        ["Handle Manipulation"] = new Guid("0cce9223-69ae-11d9-bed3-505054503030"),
        ["File Share"] = new Guid("0cce9224-69ae-11d9-bed3-505054503030"),
        ["Filtering Platform Packet Drop"] = new Guid("0cce9225-69ae-11d9-bed3-505054503030"),
        ["Filtering Platform Connection"] = new Guid("0cce9226-69ae-11d9-bed3-505054503030"),
        ["Other Object Access Events"] = new Guid("0cce9227-69ae-11d9-bed3-505054503030"),
        ["Sensitive Privilege Use"] = new Guid("0cce9228-69ae-11d9-bed3-505054503030"),
        ["Non Sensitive Privilege Use"] = new Guid("0cce9229-69ae-11d9-bed3-505054503030"),
        ["Other Privilege Use Events"] = new Guid("0cce922a-69ae-11d9-bed3-505054503030"),
        ["Process Creation"] = new Guid("0cce922b-69ae-11d9-bed3-505054503030"),
        ["Process Termination"] = new Guid("0cce922c-69ae-11d9-bed3-505054503030"),
        ["DPAPI Activity"] = new Guid("0cce922d-69ae-11d9-bed3-505054503030"),
        ["RPC Events"] = new Guid("0cce922e-69ae-11d9-bed3-505054503030"),
        ["Audit Policy Change"] = new Guid("0cce922f-69ae-11d9-bed3-505054503030"),
        ["Authentication Policy Change"] = new Guid("0cce9230-69ae-11d9-bed3-505054503030"),
        ["Authorization Policy Change"] = new Guid("0cce9231-69ae-11d9-bed3-505054503030"),
        ["MPSSVC Rule-Level Policy Change"] = new Guid("0cce9232-69ae-11d9-bed3-505054503030"),
        ["Filtering Platform Policy Change"] = new Guid("0cce9233-69ae-11d9-bed3-505054503030"),
        ["Other Policy Change Events"] = new Guid("0cce9234-69ae-11d9-bed3-505054503030"),
        ["User Account Management"] = new Guid("0cce9235-69ae-11d9-bed3-505054503030"),
        ["Computer Account Management"] = new Guid("0cce9236-69ae-11d9-bed3-505054503030"),
        ["Security Group Management"] = new Guid("0cce9237-69ae-11d9-bed3-505054503030"),
        ["Distribution Group Management"] = new Guid("0cce9238-69ae-11d9-bed3-505054503030"),
        ["Application Group Management"] = new Guid("0cce9239-69ae-11d9-bed3-505054503030"),
        ["Other Account Management Events"] = new Guid("0cce923a-69ae-11d9-bed3-505054503030"),
        ["Directory Service Access"] = new Guid("0cce923b-69ae-11d9-bed3-505054503030"),
        ["Directory Service Changes"] = new Guid("0cce923c-69ae-11d9-bed3-505054503030"),
        ["Directory Service Replication"] = new Guid("0cce923d-69ae-11d9-bed3-505054503030"),
        ["Detailed Directory Service Replication"] = new Guid("0cce923e-69ae-11d9-bed3-505054503030"),
        ["Credential Validation"] = new Guid("0cce923f-69ae-11d9-bed3-505054503030"),
        ["Kerberos Service Ticket Operations"] = new Guid("0cce9240-69ae-11d9-bed3-505054503030"),
        ["Other Account Logon Events"] = new Guid("0cce9241-69ae-11d9-bed3-505054503030"),
        ["Kerberos Authentication Service"] = new Guid("0cce9242-69ae-11d9-bed3-505054503030"),
        ["Network Policy Server"] = new Guid("0cce9243-69ae-11d9-bed3-505054503030"),
        ["Detailed File Share"] = new Guid("0cce9244-69ae-11d9-bed3-505054503030"),
        ["Removable Storage"] = new Guid("0cce9245-69ae-11d9-bed3-505054503030"),
        ["Central Policy Staging"] = new Guid("0cce9246-69ae-11d9-bed3-505054503030"),
        ["User / Device Claims"] = new Guid("0cce9247-69ae-11d9-bed3-505054503030"),
        ["Plug and Play Events"] = new Guid("0cce9248-69ae-11d9-bed3-505054503030"),
        ["Group Membership"] = new Guid("0cce9249-69ae-11d9-bed3-505054503030"),
        ["Token Right Adjusted Events"] = new Guid("0cce924a-69ae-11d9-bed3-505054503030")
    };

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

        var guid = GetSubcategoryGuid(instance.Subcategory);
        var policyInfo = AuditPolicyApi.QueryAuditPolicy(guid);

        return new Schema
        {
            Subcategory = instance.Subcategory,
            Setting = ConvertToAuditSetting(policyInfo.AuditingInformation)
        };
    }

    public SetResult<Schema>? Set(Schema? instance)
    {
        ArgumentNullException.ThrowIfNull(instance);

        var guid = GetSubcategoryGuid(instance.Subcategory);
        var currentPolicy = AuditPolicyApi.QueryAuditPolicy(guid);

        var desiredFlags = ConvertToAuditFlags(instance.Setting ?? AuditSetting.None);

        if (currentPolicy.AuditingInformation != desiredFlags)
        {
            AuditPolicyApi.SetAuditPolicy(guid, currentPolicy.AuditCategoryGuid, desiredFlags);
        }

        return null;
    }

    private static Guid GetSubcategoryGuid(string subcategory)
    {
        if (!SubcategoryToGuid.TryGetValue(subcategory, out var guid))
        {
            throw new UnknownSubcategoryException($"Unknown audit subcategory: '{subcategory}'. Valid subcategories are: {string.Join(", ", SubcategoryToGuid.Keys)}");
        }

        return guid;
    }

    private static AuditSetting ConvertToAuditSetting(uint flags)
    {
        var setting = AuditSetting.None;

        if ((flags & AuditPolicyApi.POLICY_AUDIT_EVENT_SUCCESS) != 0)
        {
            setting |= AuditSetting.Success;
        }
        if ((flags & AuditPolicyApi.POLICY_AUDIT_EVENT_FAILURE) != 0)
        {
            setting |= AuditSetting.Failure;
        }

        return setting;
    }

    private static uint ConvertToAuditFlags(AuditSetting setting)
    {
        if (setting == AuditSetting.None)
        {
            return AuditPolicyApi.POLICY_AUDIT_EVENT_NONE;
        }

        uint flags = 0;
        if (setting.HasFlag(AuditSetting.Success))
        {
            flags |= AuditPolicyApi.POLICY_AUDIT_EVENT_SUCCESS;
        }
        if (setting.HasFlag(AuditSetting.Failure))
        {
            flags |= AuditPolicyApi.POLICY_AUDIT_EVENT_FAILURE;
        }

        return flags;
    }
}

public sealed class UnknownSubcategoryException : Exception
{
    public UnknownSubcategoryException(string message) : base(message) { }
}
