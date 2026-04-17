// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Reflection;
using System.Text.Json;

using AwesomeAssertions;

using OpenDsc.Schema;

using Xunit;

using EnvironmentResource = OpenDsc.Resource.Windows.Environment.Resource;
using EnvironmentSchema = OpenDsc.Resource.Windows.Environment.Schema;
using SysEnv = System.Environment;

namespace OpenDsc.Resource.Windows.Tests.Environment;

[Trait("Category", "Integration")]
public sealed class EnvironmentTests : WindowsTestBase
{
    private readonly EnvironmentResource _resource = new(SourceGenerationContext.Default);

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
        var attr = typeof(EnvironmentResource).GetCustomAttribute<OpenDsc.Resource.DscResourceAttribute>();

        attr.Should().NotBeNull();
        attr!.Type.Should().Be("OpenDsc.Windows/Environment");
        attr.Version.ToString().Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void Get_NonExistentVariable_ReturnsExistFalse()
    {
        var schema = new EnvironmentSchema { Name = "NonExistentVariable_12345_XYZ" };

        var result = _resource.Get(schema);

        result.Exist.Should().BeFalse();
        result.Name.Should().Be("NonExistentVariable_12345_XYZ");
    }

    [Fact]
    public void Get_ExistingUserVariable_ReturnsValue()
    {
        SysEnv.SetEnvironmentVariable("TestVar_GetExisting_XUnit", "TestValue_GetExisting", EnvironmentVariableTarget.User);
        try
        {
            var schema = new EnvironmentSchema { Name = "TestVar_GetExisting_XUnit" };

            var result = _resource.Get(schema);

            result.Name.Should().Be("TestVar_GetExisting_XUnit");
            result.Value.Should().Be("TestValue_GetExisting");
        }
        finally
        {
            SysEnv.SetEnvironmentVariable("TestVar_GetExisting_XUnit", null, EnvironmentVariableTarget.User);
        }
    }

    [Fact]
    public void Get_MachineScopedVariable_ReturnsValue()
    {
        var machineVars = SysEnv.GetEnvironmentVariables(EnvironmentVariableTarget.Machine);
        string? testVarName = null;
        foreach (string? key in machineVars.Keys)
        {
            testVarName = key;
            break;
        }

        if (testVarName is null)
        {
            return;
        }

        var schema = new EnvironmentSchema { Name = testVarName, Scope = DscScope.Machine };

        var result = _resource.Get(schema);

        result.Name.Should().Be(testVarName);
        result.Scope.Should().Be(DscScope.Machine);
    }

    [Fact]
    public void Set_NewUserVariable_CreatesVariable()
    {
        var schema = new EnvironmentSchema { Name = "TestVar_SetCreate_XUnit", Value = "TestValue123" };
        try
        {
            _resource.Set(schema);

            var result = _resource.Get(new EnvironmentSchema { Name = "TestVar_SetCreate_XUnit" });

            result.Value.Should().Be("TestValue123");
        }
        finally
        {
            SysEnv.SetEnvironmentVariable("TestVar_SetCreate_XUnit", null, EnvironmentVariableTarget.User);
        }
    }

    [Fact]
    public void Set_ExistingUserVariable_UpdatesValue()
    {
        SysEnv.SetEnvironmentVariable("TestVar_SetUpdate_XUnit", "OriginalValue", EnvironmentVariableTarget.User);
        try
        {
            _resource.Set(new EnvironmentSchema { Name = "TestVar_SetUpdate_XUnit", Value = "UpdatedValue" });

            var result = _resource.Get(new EnvironmentSchema { Name = "TestVar_SetUpdate_XUnit" });

            result.Value.Should().Be("UpdatedValue");
        }
        finally
        {
            SysEnv.SetEnvironmentVariable("TestVar_SetUpdate_XUnit", null, EnvironmentVariableTarget.User);
        }
    }

    [Fact]
    public void Set_VariableWithSpecialCharacters_SavesCorrectly()
    {
        const string value = @"C:\Path\With Spaces;D:\Another\Path";
        var schema = new EnvironmentSchema { Name = "TestVar_SetSpecial_XUnit", Value = value };
        try
        {
            _resource.Set(schema);

            var result = _resource.Get(new EnvironmentSchema { Name = "TestVar_SetSpecial_XUnit" });

            result.Value.Should().Be(value);
        }
        finally
        {
            SysEnv.SetEnvironmentVariable("TestVar_SetSpecial_XUnit", null, EnvironmentVariableTarget.User);
        }
    }

    [Fact]
    public void Set_NullValue_ThrowsArgumentException()
    {
        var schema = new EnvironmentSchema { Name = "TestVar_SetNull_XUnit", Value = null };

        var act = () => _resource.Set(schema);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Delete_ExistingVariable_RemovesVariable()
    {
        SysEnv.SetEnvironmentVariable("TestVar_Delete_XUnit", "ToBeDeleted", EnvironmentVariableTarget.User);
        try
        {
            _resource.Delete(new EnvironmentSchema { Name = "TestVar_Delete_XUnit" });

            var result = _resource.Get(new EnvironmentSchema { Name = "TestVar_Delete_XUnit" });

            result.Exist.Should().BeFalse();
        }
        finally
        {
            SysEnv.SetEnvironmentVariable("TestVar_Delete_XUnit", null, EnvironmentVariableTarget.User);
        }
    }

    [Fact]
    public void Delete_NonExistentVariable_DoesNotThrow()
    {
        var act = () => _resource.Delete(new EnvironmentSchema { Name = "NonExistentVar_Delete_XUnit" });

        act.Should().NotThrow();
    }

    [Fact]
    public void Export_ReturnsAllVariables()
    {
        var results = _resource.Export(null).ToList();

        results.Should().NotBeEmpty();
        results.Should().AllSatisfy(r => r.Name.Should().NotBeNull());
    }

    [Fact]
    public void Export_IncludesMachineScopedVariables()
    {
        var results = _resource.Export(null).ToList();

        results.Should().Contain(r => r.Scope == DscScope.Machine);
    }
}
