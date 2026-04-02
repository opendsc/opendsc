// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Reflection;
using System.Text.Json;

using AwesomeAssertions;

using OpenDsc.Resource;

using Xunit;

using MultiContext = TestResource.Multi.SourceGenerationContext;

namespace TestResource.Multi.Tests;

[Trait("Category", "Integration")]
public sealed class ServiceResourceTests
{
    private readonly ServiceResource _resource = new(MultiContext.Default);

    [Fact]
    public void GetSchema_ReturnsValidJson()
    {
        var schemaJson = _resource.GetSchema();
        var doc = JsonDocument.Parse(schemaJson);

        doc.RootElement.ValueKind.Should().Be(JsonValueKind.Object);
    }

    [Fact]
    public void GetSchema_ContainsNameAndStateProperties()
    {
        var schemaJson = _resource.GetSchema();
        var doc = JsonDocument.Parse(schemaJson);

        var props = doc.RootElement.GetProperty("properties");
        props.TryGetProperty("name", out _).Should().BeTrue();
        props.TryGetProperty("state", out _).Should().BeTrue();
    }

    [Fact]
    public void DscResourceAttribute_HasCorrectType()
    {
        var attr = typeof(ServiceResource).GetCustomAttribute<DscResourceAttribute>();

        attr.Should().NotBeNull();
        attr!.Type.Should().Be("TestResource.Multi/Service");
    }

    [Fact]
    public void DscResourceAttribute_HasDescription()
    {
        var attr = typeof(ServiceResource).GetCustomAttribute<DscResourceAttribute>();

        attr!.Description.Should().Be("Manages service state");
    }

    [Fact]
    public void Get_RunningService_ReturnsRunningState()
    {
        var result = _resource.Get(new ServiceSchema { Name = "TestService1" });

        result.Exist.Should().BeNull();
        result.Name.Should().Be("TestService1");
        result.State.Should().Be(ServiceState.Running);
    }

    [Fact]
    public void Get_StoppedService_ReturnsStoppedState()
    {
        var result = _resource.Get(new ServiceSchema { Name = "TestService2" });

        result.Exist.Should().BeNull();
        result.Name.Should().Be("TestService2");
        result.State.Should().Be(ServiceState.Stopped);
    }

    [Fact]
    public void Get_NonExistentService_ReturnsExistFalse()
    {
        var result = _resource.Get(new ServiceSchema { Name = $"NonExistentService_{Guid.NewGuid():N}" });

        result.Exist.Should().BeFalse();
        result.State.Should().BeNull();
    }

    [Fact]
    public void Test_ServiceInDesiredState_ReturnsNoDifferingProperties()
    {
        var result = _resource.Test(new ServiceSchema { Name = "TestService1", State = ServiceState.Running });

        result.DifferingProperties.Should().BeNullOrEmpty();
    }

    [Fact]
    public void Test_ServiceNotInDesiredState_ReturnsDifferingProperty()
    {
        var result = _resource.Test(new ServiceSchema { Name = "TestService1", State = ServiceState.Stopped });

        result.DifferingProperties.Should().Contain(nameof(ServiceSchema.State));
    }

    [Fact]
    public void Test_NonExistentService_ReturnsDifferingProperty()
    {
        var result = _resource.Test(new ServiceSchema { Name = $"NonExistentService_{Guid.NewGuid():N}" });

        result.DifferingProperties.Should().Contain(nameof(ServiceSchema.Exist));
    }
}
