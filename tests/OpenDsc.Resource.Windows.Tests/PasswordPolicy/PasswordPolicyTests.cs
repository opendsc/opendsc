// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Reflection;
using System.Text.Json;

using AwesomeAssertions;

using Xunit;

using PasswordPolicyResource = OpenDsc.Resource.Windows.PasswordPolicy.Resource;
using PasswordPolicySchema = OpenDsc.Resource.Windows.PasswordPolicy.Schema;

namespace OpenDsc.Resource.Windows.Tests.PasswordPolicy;

[Trait("Category", "Integration")]
public sealed class PasswordPolicyTests : WindowsTestBase
{
    private readonly PasswordPolicyResource _resource = new(OpenDsc.Resource.Windows.SourceGenerationContext.Default);

    [Fact]
    public void GetSchema_ReturnsValidJson()
    {
        var schemaJson = _resource.GetSchema();
        var doc = JsonDocument.Parse(schemaJson);

        doc.RootElement.GetProperty("$schema").GetString()
            .Should().Be("https://json-schema.org/draft/2020-12/schema");
    }

    [Fact]
    public void DscResourceAttribute_HasCorrectTypeAndVersion()
    {
        var attr = typeof(PasswordPolicyResource).GetCustomAttribute<OpenDsc.Resource.DscResourceAttribute>();

        attr.Should().NotBeNull();
        attr!.Type.Should().Be("OpenDsc.Windows/PasswordPolicy");
        attr.Version.ToString().Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void Get_CurrentPolicy_ReturnsValidPolicy()
    {
        var result = _resource.Get(new PasswordPolicySchema());

        result.Should().NotBeNull();

        var minimumPasswordLength = result.MinimumPasswordLength ?? throw new InvalidOperationException("MinimumPasswordLength must not be null.");
        var maximumPasswordAgeDays = result.MaximumPasswordAgeDays ?? throw new InvalidOperationException("MaximumPasswordAgeDays must not be null.");
        var minimumPasswordAgeDays = result.MinimumPasswordAgeDays ?? throw new InvalidOperationException("MinimumPasswordAgeDays must not be null.");
        var passwordHistoryLength = result.PasswordHistoryLength ?? throw new InvalidOperationException("PasswordHistoryLength must not be null.");

        minimumPasswordLength.Should().BeGreaterThanOrEqualTo(0);
        minimumPasswordLength.Should().BeLessThanOrEqualTo(14);
        maximumPasswordAgeDays.Should().BeGreaterThanOrEqualTo(0);
        maximumPasswordAgeDays.Should().BeLessThanOrEqualTo(999);
        minimumPasswordAgeDays.Should().BeGreaterThanOrEqualTo(0);
        minimumPasswordAgeDays.Should().BeLessThanOrEqualTo(998);
        passwordHistoryLength.Should().BeGreaterThanOrEqualTo(0);
        passwordHistoryLength.Should().BeLessThanOrEqualTo(24);
    }

    [RequiresAdminFact]
    public void Set_MinPasswordLength_UpdatesPolicy()
    {
        var original = _resource.Get(new PasswordPolicySchema());
        const uint targetLength = 8;

        try
        {
            _resource.Set(new PasswordPolicySchema { MinimumPasswordLength = targetLength });

            var updated = _resource.Get(new PasswordPolicySchema());
            updated.MinimumPasswordLength.Should().Be(targetLength);
        }
        finally
        {
            _resource.Set(new PasswordPolicySchema
            {
                MinimumPasswordLength = original.MinimumPasswordLength,
                MaximumPasswordAgeDays = original.MaximumPasswordAgeDays,
                MinimumPasswordAgeDays = original.MinimumPasswordAgeDays,
                PasswordHistoryLength = original.PasswordHistoryLength
            });
        }
    }

    [RequiresAdminFact]
    public void Set_MaxPasswordAge_UpdatesPolicy()
    {
        var original = _resource.Get(new PasswordPolicySchema());
        const uint targetAge = 90;

        try
        {
            _resource.Set(new PasswordPolicySchema { MaximumPasswordAgeDays = targetAge });

            var updated = _resource.Get(new PasswordPolicySchema());
            updated.MaximumPasswordAgeDays.Should().Be(targetAge);
        }
        finally
        {
            _resource.Set(new PasswordPolicySchema
            {
                MinimumPasswordLength = original.MinimumPasswordLength,
                MaximumPasswordAgeDays = original.MaximumPasswordAgeDays,
                MinimumPasswordAgeDays = original.MinimumPasswordAgeDays,
                PasswordHistoryLength = original.PasswordHistoryLength
            });
        }
    }

    [RequiresAdminFact]
    public void Set_MinPasswordAgeDays_UpdatesPolicy()
    {
        var original = _resource.Get(new PasswordPolicySchema());
        const uint targetMinAge = 1;

        try
        {
            _resource.Set(new PasswordPolicySchema { MinimumPasswordAgeDays = targetMinAge });

            var updated = _resource.Get(new PasswordPolicySchema());
            updated.MinimumPasswordAgeDays.Should().Be(targetMinAge);
        }
        finally
        {
            _resource.Set(new PasswordPolicySchema
            {
                MinimumPasswordLength = original.MinimumPasswordLength,
                MaximumPasswordAgeDays = original.MaximumPasswordAgeDays,
                MinimumPasswordAgeDays = original.MinimumPasswordAgeDays,
                PasswordHistoryLength = original.PasswordHistoryLength
            });
        }
    }

    [RequiresAdminFact]
    public void Set_PasswordHistoryLength_UpdatesPolicy()
    {
        var original = _resource.Get(new PasswordPolicySchema());
        const uint targetHistory = 5;

        try
        {
            _resource.Set(new PasswordPolicySchema { PasswordHistoryLength = targetHistory });

            var updated = _resource.Get(new PasswordPolicySchema());
            updated.PasswordHistoryLength.Should().Be(targetHistory);
        }
        finally
        {
            _resource.Set(new PasswordPolicySchema
            {
                MinimumPasswordLength = original.MinimumPasswordLength,
                MaximumPasswordAgeDays = original.MaximumPasswordAgeDays,
                MinimumPasswordAgeDays = original.MinimumPasswordAgeDays,
                PasswordHistoryLength = original.PasswordHistoryLength
            });
        }
    }

    [RequiresAdminFact]
    public void Set_MaxPasswordAgeZero_MeansNeverExpire()
    {
        var original = _resource.Get(new PasswordPolicySchema());
        const uint neverExpire = 0;

        try
        {
            _resource.Set(new PasswordPolicySchema { MaximumPasswordAgeDays = neverExpire });

            var updated = _resource.Get(new PasswordPolicySchema());
            updated.MaximumPasswordAgeDays.Should().Be(neverExpire);
        }
        finally
        {
            _resource.Set(new PasswordPolicySchema
            {
                MinimumPasswordLength = original.MinimumPasswordLength,
                MaximumPasswordAgeDays = original.MaximumPasswordAgeDays,
                MinimumPasswordAgeDays = original.MinimumPasswordAgeDays,
                PasswordHistoryLength = original.PasswordHistoryLength
            });
        }
    }
}
