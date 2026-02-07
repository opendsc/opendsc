// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Serialization;

using Json.Schema;
using Json.Schema.Generation;

using static OpenDsc.Resource.Windows.AccountLockoutPolicy.AccountLockoutPolicyHelper;

namespace OpenDsc.Resource.Windows.AccountLockoutPolicy;

[DscResource("OpenDsc.Windows/AccountLockoutPolicy", "0.1.0", Description = "Manage Windows account lockout policy settings", Tags = ["windows", "security", "lockout", "policy"])]
[ExitCode(0, Description = "Success")]
[ExitCode(1, Exception = typeof(Exception), Description = "Error")]
[ExitCode(2, Exception = typeof(Win32Exception), Description = "Win32 API error")]
[ExitCode(3, Exception = typeof(UnauthorizedAccessException), Description = "Access denied - requires administrator privileges")]
[ExitCode(4, Exception = typeof(ArgumentException), Description = "Invalid parameter value")]
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
        IntPtr buffer = IntPtr.Zero;

        try
        {
            uint result = NetUserModalsGet(null, 3, out buffer);

            if (result == ERROR_ACCESS_DENIED)
            {
                throw new UnauthorizedAccessException("Access denied. Administrator privileges are required to read account lockout policy.");
            }

            if (result != NERR_Success)
            {
                throw new Win32Exception((int)result, $"Failed to get account lockout policy. Error code: {result}");
            }

            var info = Marshal.PtrToStructure<USER_MODALS_INFO_3>(buffer);

            return new Schema
            {
                LockoutThreshold = info.usrmod3_lockout_threshold,
                LockoutDurationMinutes = info.usrmod3_lockout_duration / 60,
                LockoutObservationWindowMinutes = info.usrmod3_lockout_observation_window / 60
            };
        }
        finally
        {
            if (buffer != IntPtr.Zero)
            {
                NetApiBufferFree(buffer);
            }
        }
    }

    public SetResult<Schema>? Set(Schema? instance)
    {
        if (instance is null)
        {
            throw new ArgumentNullException(nameof(instance));
        }

        var current = Get(instance);
        bool changed = false;

        uint lockoutDuration = instance.LockoutDurationMinutes ?? current.LockoutDurationMinutes!.Value;
        uint lockoutObservationWindow = instance.LockoutObservationWindowMinutes ?? current.LockoutObservationWindowMinutes!.Value;
        uint lockoutThreshold = instance.LockoutThreshold ?? current.LockoutThreshold!.Value;

        if (lockoutThreshold > 0 && lockoutDuration > 0 && lockoutObservationWindow > lockoutDuration)
        {
            throw new ArgumentException($"LockoutObservationWindowMinutes ({lockoutObservationWindow}) must be less than or equal to LockoutDurationMinutes ({lockoutDuration}) when LockoutThreshold is greater than 0.");
        }

        var info = new USER_MODALS_INFO_3
        {
            usrmod3_lockout_duration = lockoutDuration * 60,
            usrmod3_lockout_observation_window = lockoutObservationWindow * 60,
            usrmod3_lockout_threshold = lockoutThreshold
        };

        if (instance.LockoutThreshold.HasValue && instance.LockoutThreshold != current.LockoutThreshold)
        {
            changed = true;
        }

        if (instance.LockoutDurationMinutes.HasValue && instance.LockoutDurationMinutes != current.LockoutDurationMinutes)
        {
            changed = true;
        }

        if (instance.LockoutObservationWindowMinutes.HasValue && instance.LockoutObservationWindowMinutes != current.LockoutObservationWindowMinutes)
        {
            changed = true;
        }

        if (!changed)
        {
            return null;
        }

        uint result = NetUserModalsSet(null, 3, ref info, out _);

        if (result == ERROR_ACCESS_DENIED)
        {
            throw new UnauthorizedAccessException("Access denied. Administrator privileges are required to set account lockout policy.");
        }

        if (result == ERROR_INVALID_PARAMETER)
        {
            throw new ArgumentException("Invalid account lockout policy parameter.");
        }

        if (result != NERR_Success)
        {
            throw new Win32Exception((int)result, $"Failed to set account lockout policy. Error code: {result}");
        }

        return null;
    }
}
