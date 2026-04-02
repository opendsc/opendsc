// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Reflection;
using System.Text.Json;

using AwesomeAssertions;

using Xunit;

using AuditPolicyResource = OpenDsc.Resource.Windows.AuditPolicy.Resource;
using AuditPolicySchema = OpenDsc.Resource.Windows.AuditPolicy.Schema;
using AuditSetting = OpenDsc.Resource.Windows.AuditPolicy.AuditSetting;

namespace OpenDsc.Resource.Windows.Tests.AuditPolicy;

[Trait("Category", "Integration")]
public sealed class AuditPolicyTests : WindowsTestBase
{
    private readonly AuditPolicyResource _resource = new(OpenDsc.Resource.Windows.SourceGenerationContext.Default);

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
        var attr = typeof(AuditPolicyResource).GetCustomAttribute<OpenDsc.Resource.DscResourceAttribute>();

        attr.Should().NotBeNull();
        attr!.Type.Should().Be("OpenDsc.Windows/AuditPolicy");
        attr.Version.ToString().Should().NotBeNullOrEmpty();
    }

    [RequiresAdminFact]
    public void Get_FileSystem_ReturnsSetting()
    {
        var result = _resource.Get(new AuditPolicySchema { Subcategory = "File System" });

        result.Should().NotBeNull();
        result.Subcategory.Should().Be("File System");

        // Setting may be null, empty, or contain values depending on system state.
        result.Setting.Should().NotBeNull();
        result.Setting!.All(v => v == AuditSetting.Success || v == AuditSetting.Failure).Should().BeTrue();
    }

    [RequiresAdminFact]
    public void Set_Success_UpdatesPolicy()
    {
        var original = _resource.Get(new AuditPolicySchema { Subcategory = "File System" });

        try
        {
            _resource.Set(new AuditPolicySchema
            {
                Subcategory = "File System",
                Setting = new[] { AuditSetting.Success }
            });

            var updated = _resource.Get(new AuditPolicySchema { Subcategory = "File System" });
            updated.Setting.Should().Contain(AuditSetting.Success);
            updated.Setting.Should().NotContain(AuditSetting.Failure);
        }
        finally
        {
            _resource.Set(new AuditPolicySchema
            {
                Subcategory = "File System",
                Setting = original.Setting
            });
        }
    }

    [RequiresAdminFact]
    public void Set_Failure_UpdatesPolicy()
    {
        var original = _resource.Get(new AuditPolicySchema { Subcategory = "File System" });

        try
        {
            _resource.Set(new AuditPolicySchema
            {
                Subcategory = "File System",
                Setting = new[] { AuditSetting.Failure }
            });

            var updated = _resource.Get(new AuditPolicySchema { Subcategory = "File System" });
            updated.Setting.Should().Contain(AuditSetting.Failure);
            updated.Setting.Should().NotContain(AuditSetting.Success);
        }
        finally
        {
            _resource.Set(new AuditPolicySchema
            {
                Subcategory = "File System",
                Setting = original.Setting
            });
        }
    }

    [RequiresAdminFact]
    public void Set_SuccessAndFailure_UpdatesPolicy()
    {
        var original = _resource.Get(new AuditPolicySchema { Subcategory = "File System" });

        try
        {
            _resource.Set(new AuditPolicySchema
            {
                Subcategory = "File System",
                Setting = new[] { AuditSetting.Success, AuditSetting.Failure }
            });

            var updated = _resource.Get(new AuditPolicySchema { Subcategory = "File System" });
            updated.Setting.Should().Contain(AuditSetting.Success);
            updated.Setting.Should().Contain(AuditSetting.Failure);
        }
        finally
        {
            _resource.Set(new AuditPolicySchema
            {
                Subcategory = "File System",
                Setting = original.Setting
            });
        }
    }

    [RequiresAdminFact]
    public void Set_None_DisablesPolicy()
    {
        var original = _resource.Get(new AuditPolicySchema { Subcategory = "File System" });

        try
        {
            _resource.Set(new AuditPolicySchema
            {
                Subcategory = "File System",
                Setting = Array.Empty<AuditSetting>()
            });

            var updated = _resource.Get(new AuditPolicySchema { Subcategory = "File System" });
            updated.Setting.Should().BeNullOrEmpty();
        }
        finally
        {
            _resource.Set(new AuditPolicySchema
            {
                Subcategory = "File System",
                Setting = original.Setting
            });
        }
    }

    [Fact]
    public void Set_InvalidSubcategory_ThrowsUnknownSubcategoryException()
    {
        var act = () => _resource.Set(new AuditPolicySchema
        {
            Subcategory = "Invalid Subcategory Name",
            Setting = new[] { AuditSetting.Success }
        });

        act.Should().Throw<OpenDsc.Resource.Windows.AuditPolicy.UnknownSubcategoryException>();
    }
}
