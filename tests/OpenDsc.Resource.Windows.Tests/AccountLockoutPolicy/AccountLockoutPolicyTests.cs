// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Reflection;
using System.Text.Json;

using AwesomeAssertions;

using Xunit;

using AccountLockoutPolicyResource = OpenDsc.Resource.Windows.AccountLockoutPolicy.Resource;
using AccountLockoutPolicySchema = OpenDsc.Resource.Windows.AccountLockoutPolicy.Schema;

namespace OpenDsc.Resource.Windows.Tests.AccountLockoutPolicy;

[Trait("Category", "Integration")]
public sealed class AccountLockoutPolicyTests : WindowsTestBase
{
    private readonly AccountLockoutPolicyResource _resource = new(OpenDsc.Resource.Windows.SourceGenerationContext.Default);

    [Fact]
    public void GetSchema_ReturnsValidJson()
    {
        var schemaJson = _resource.GetSchema();
        var doc = JsonDocument.Parse(schemaJson);

        doc.RootElement.ValueKind.Should().Be(JsonValueKind.Object);
    }

    [Fact]
    public void DscResourceAttribute_HasCorrectTypeAndVersion()
    {
        var attr = typeof(AccountLockoutPolicyResource).GetCustomAttribute<OpenDsc.Resource.DscResourceAttribute>();

        attr.Should().NotBeNull();
        attr!.Type.Should().Be("OpenDsc.Windows/AccountLockoutPolicy");
        attr.Version.ToString().Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void Get_CurrentPolicy_ReturnsValidPolicy()
    {
        var result = _resource.Get(new AccountLockoutPolicySchema());

        result.Should().NotBeNull();

        var threshold = result.LockoutThreshold ?? throw new InvalidOperationException("LockoutThreshold must not be null.");
        var duration = result.LockoutDurationMinutes ?? throw new InvalidOperationException("LockoutDurationMinutes must not be null.");
        var observation = result.LockoutObservationWindowMinutes ?? throw new InvalidOperationException("LockoutObservationWindowMinutes must not be null.");

        threshold.Should().BeGreaterThanOrEqualTo(0);
        threshold.Should().BeLessThanOrEqualTo(999);

        duration.Should().BeGreaterThanOrEqualTo(0);
        duration.Should().BeLessThanOrEqualTo(99999);

        observation.Should().BeGreaterThanOrEqualTo(0);
        observation.Should().BeLessThanOrEqualTo(99999);

        if (threshold > 0)
        {
            observation.Should().BeLessThanOrEqualTo(duration);
        }
    }

    [RequiresAdminFact]
    public void Set_LockoutThreshold_UpdatesPolicy()
    {
        var original = _resource.Get(new AccountLockoutPolicySchema());
        const uint targetThreshold = 5;

        try
        {
            _resource.Set(new AccountLockoutPolicySchema
            {
                LockoutThreshold = targetThreshold,
                LockoutDurationMinutes = original.LockoutDurationMinutes,
                LockoutObservationWindowMinutes = original.LockoutObservationWindowMinutes
            });

            var updated = _resource.Get(new AccountLockoutPolicySchema());
            updated.LockoutThreshold.Should().Be(targetThreshold);
        }
        finally
        {
            _resource.Set(new AccountLockoutPolicySchema
            {
                LockoutThreshold = original.LockoutThreshold,
                LockoutDurationMinutes = original.LockoutDurationMinutes,
                LockoutObservationWindowMinutes = original.LockoutObservationWindowMinutes
            });
        }
    }

    [RequiresAdminFact]
    public void Set_LockoutDuration_UpdatesPolicy()
    {
        var original = _resource.Get(new AccountLockoutPolicySchema());
        const uint targetDuration = 30;
        const uint targetObservation = 30;

        try
        {
            _resource.Set(new AccountLockoutPolicySchema
            {
                LockoutThreshold = original.LockoutThreshold ?? 0,
                LockoutDurationMinutes = targetDuration,
                LockoutObservationWindowMinutes = targetObservation
            });

            var updated = _resource.Get(new AccountLockoutPolicySchema());
            updated.LockoutDurationMinutes.Should().Be(targetDuration);
            updated.LockoutObservationWindowMinutes.Should().Be(targetObservation);
        }
        finally
        {
            _resource.Set(new AccountLockoutPolicySchema
            {
                LockoutThreshold = original.LockoutThreshold,
                LockoutDurationMinutes = original.LockoutDurationMinutes,
                LockoutObservationWindowMinutes = original.LockoutObservationWindowMinutes
            });
        }
    }

    [RequiresAdminFact]
    public void Set_ObservationWindowExceedsDuration_ThrowsArgumentException()
    {
        var act = () => _resource.Set(new AccountLockoutPolicySchema
        {
            LockoutThreshold = 5,
            LockoutDurationMinutes = 10,
            LockoutObservationWindowMinutes = 20
        });

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Set_NullInstance_ThrowsArgumentNullException()
    {
        var act = () => _resource.Set(null);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Get
        var original = _resource.Get(new AccountLockoutPolicySchema());

        try
        {
            _resource.Set(new AccountLockoutPolicySchema
            {
        LockoutThreshold = 0,
                LockoutDurationMinutes = original.LockoutDurationMinutes,
                LockoutObservationWindowMinutes = original.LockoutObservationWindowMinutes
    });

            var updated = _resource.Get(new AccountLockoutPolicySchema());
    updated.LockoutThreshold.Should().Be(0);
}
        finally
        {
            _resource.Set(new AccountLockoutPolicySchema
            {
    LockoutThreshold = original.LockoutThreshold,
                LockoutDurationMinutes = original.LockoutDurationMinutes,
                LockoutObservationWindowMinutes = original.LockoutObservationWindowMinutes
});
        }
    }
}
