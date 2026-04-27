// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Reflection;
using System.ServiceProcess;
using System.Text.Json;

using AwesomeAssertions;

using Xunit;

using ServiceResource = OpenDsc.Resource.Windows.Service.Resource;
using ServiceSchema = OpenDsc.Resource.Windows.Service.Schema;

namespace OpenDsc.Resource.Windows.Tests.Service;

[Trait("Category", "Integration")]
public sealed class ServiceTests : WindowsTestBase
{
    private readonly ServiceResource _resource = new(SourceGenerationContext.Default);

    [Fact]
    public void GetSchema_ReturnsValidJson()
    {
        var schemaJson = _resource.GetSchema();
        var doc = JsonDocument.Parse(schemaJson);

        doc.RootElement.ValueKind.Should().Be(JsonValueKind.Object);
        var hasProperties = doc.RootElement.TryGetProperty("properties", out _);
        var hasDefs = doc.RootElement.TryGetProperty("$defs", out _);
        var hasSchema = doc.RootElement.TryGetProperty("$schema", out _);
        (hasProperties || hasDefs || hasSchema).Should().BeTrue();
    }

    [Fact]
    public void DscResourceAttribute_HasCorrectTypeAndVersion()
    {
        var attr = typeof(ServiceResource).GetCustomAttribute<OpenDsc.Resource.DscResourceAttribute>();

        attr.Should().NotBeNull();
        attr!.Type.Should().Be("OpenDsc.Windows/Service");
        attr.Version.ToString().Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void Get_NonExistentService_ReturnsExistFalse()
    {
        var schema = new ServiceSchema { Name = "NonExistentService_12345" };

        var result = _resource.Get(schema);

        result.Exist.Should().BeFalse();
        result.Name.Should().Be("NonExistentService_12345");
    }

    [Fact]
    public void Get_ExistingBuiltinService_ReturnsState()
    {
        var schema = new ServiceSchema { Name = "Spooler" };

        var result = _resource.Get(schema);

        result.Exist.Should().NotBe(false);
        result.Name.Should().Be("Spooler");
        result.DisplayName.Should().NotBeNullOrEmpty();
        result.Status.Should().NotBeNull();
        result.StartType.Should().NotBeNull();
    }

    [Fact]
    public void Export_NoFilter_ReturnsServices()
    {
        var results = _resource.Export(null).ToList();

        results.Should().NotBeEmpty();
        results.Should().AllSatisfy(r => r.Name.Should().NotBeNullOrEmpty());
    }

    [RequiresAdminFact]
    public void Set_NewService_CreatesService()
    {
        var testServicePath = Path.GetFullPath(Path.Combine("..", "..", "..", "..", "artifacts", "TestService", "TestService.exe"));

        // Skip if test artifact is missing
        if (!File.Exists(testServicePath))
        {
            return;
        }

        var serviceName = $"DscTestService_{Guid.NewGuid():N}";
        var createSchema = new ServiceSchema
        {
            Name = serviceName,
            Path = testServicePath,
            StartType = ServiceStartMode.Manual
        };

        try
        {
            _resource.Set(createSchema);

            var actual = _resource.Get(new ServiceSchema { Name = serviceName });

            actual.Exist.Should().NotBe(false);
            actual.Name.Should().Be(serviceName);
            actual.StartType.Should().Be(ServiceStartMode.Manual);
        }
        finally
        {
            try
            {
                _resource.Delete(new ServiceSchema { Name = serviceName });
            }
            catch
            {
                // best-effort cleanup
            }
        }
    }

    [RequiresAdminFact]
    public void Set_ExistingService_UpdatesStartType()
    {
        var testServicePath = Path.GetFullPath(Path.Combine("..", "..", "..", "..", "artifacts", "TestService", "TestService.exe"));

        // Skip if test artifact is missing
        if (!File.Exists(testServicePath))
        {
            return;
        }

        var serviceName = $"DscTestService_{Guid.NewGuid():N}";

        try
        {
            _resource.Set(new ServiceSchema
            {
                Name = serviceName,
                Path = testServicePath,
                StartType = ServiceStartMode.Manual
            });

            _resource.Set(new ServiceSchema
            {
                Name = serviceName,
                StartType = ServiceStartMode.Automatic
            });

            var actual = _resource.Get(new ServiceSchema { Name = serviceName });

            actual.StartType.Should().Be(ServiceStartMode.Automatic);
        }
        finally
        {
            try
            {
                _resource.Delete(new ServiceSchema { Name = serviceName });
            }
            catch
            {
                // best-effort cleanup
            }
        }
    }

    [RequiresAdminFact]
    public void Delete_ExistingService_RemovesService()
    {
        var testServicePath = Path.GetFullPath(Path.Combine("..", "..", "..", "..", "artifacts", "TestService", "TestService.exe"));

        // Skip if test artifact is missing
        if (!File.Exists(testServicePath))
        {
            return;
        }

        var serviceName = $"DscTestService_{Guid.NewGuid():N}";

        _resource.Set(new ServiceSchema
        {
            Name = serviceName,
            Path = testServicePath,
            StartType = ServiceStartMode.Manual
        });

        _resource.Delete(new ServiceSchema { Name = serviceName });

        var result = _resource.Get(new ServiceSchema { Name = serviceName });

        result.Exist.Should().BeFalse();
    }

    [Fact]
    public void Set_InvalidStatus_ThrowsArgumentException()
    {
        var schema = new ServiceSchema { Name = "AnyService", Status = ServiceControllerStatus.StartPending };

        var act = () => _resource.Set(schema);

        act.Should().Throw<ArgumentException>().WithMessage("*Invalid service status*");
    }

    [Fact]
    public void Set_InvalidStartType_ThrowsArgumentException()
    {
        var schema = new ServiceSchema { Name = "AnyService", StartType = ServiceStartMode.Boot };

        var act = () => _resource.Set(schema);

        act.Should().Throw<ArgumentException>().WithMessage("*Invalid service start type*");
    }

    [Fact]
    public void Set_NewService_WithoutPath_ThrowsArgumentException()
    {
        var serviceName = $"DscTestService_{Guid.NewGuid():N}";

        var act = () => _resource.Set(new ServiceSchema
        {
            Name = serviceName,
            StartType = ServiceStartMode.Manual
        });

        act.Should().Throw<ArgumentException>().WithMessage("*Path*");
    }

    [Fact]
    public void Set_NewService_WithoutStartType_ThrowsArgumentException()
    {
        var serviceName = $"DscTestService_{Guid.NewGuid():N}";

        var act = () => _resource.Set(new ServiceSchema
        {
            Name = serviceName,
            Path = @"C:\Windows\System32\cmd.exe"
        });

        act.Should().Throw<ArgumentException>().WithMessage("*StartType*");
    }

    [Fact]
    public void Get_NullInstance_ThrowsArgumentNullException()
    {
        var act = () => _resource.Get(null);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Set_NullInstance_ThrowsArgumentNullException()
    {
        var act = () => _resource.Set(null);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Delete_NullInstance_ThrowsArgumentNullException()
    {
        var act = () => _resource.Delete(null);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Set_InvalidStatusContinuousPending_ThrowsArgumentException()
    {
        var act = () => _resource.Set(new ServiceSchema { Name = "AnyService", Status = ServiceControllerStatus.ContinuePending });

        act.Should().Throw<ArgumentException>().WithMessage("*Invalid service status*");
    }

    [Fact]
    public void Set_InvalidStartTypeSystem
    {
        r.Name.Should().NotBeNullOrEmpty();
    r.DisplayName.Should().NotBeNullOrEmpty();
});
    }

    [Fact]
public void Get_NonExistentService_HasExistFalse()
{
    var schema = new ServiceSchema { Name = "NonExistentService_" + Guid.NewGuid().ToString("N") };

    var result = _resource.Get(schema);

    result.Exist.Should().BeFalse();
    result.Name.Should().Be(schema.Name);
}

[RequiresAdminFact]
public void Set_ExistingService_UpdatesDisplayName()
{
    var testServicePath = Path.GetFullPath(Path.Combine("..", "..", "..", "..", "artifacts", "TestService", "TestService.exe"));

    // Skip if test artifact is missing
    if (!File.Exists(testServicePath))
    {
        return;
    }

    var serviceName = $"DscTestService_{Guid.NewGuid():N}";

    try
    {
        _resource.Set(new ServiceSchema
        {
            Name = serviceName,
            Path = testServicePath,
            StartType = ServiceStartMode.Manual
        });

        _resource.Set(new ServiceSchema
        {
            Name = serviceName,
            DisplayName = "Updated Display Name"
        });

        var actual = _resource.Get(new ServiceSchema { Name = serviceName });

        actual.DisplayName.Should().Be("Updated Display Name");
    }
    finally
    {
        try { _resource.Delete(new ServiceSchema { Name = serviceName }); } catch { }
    }
}

[RequiresAdminFact]
public void Set_ExistingService_UpdatesDescription()
{
    var testServicePath = Path.GetFullPath(Path.Combine("..", "..", "..", "..", "artifacts", "TestService", "TestService.exe"));

    // Skip if test artifact is missing
    if (!File.Exists(testServicePath))
    {
        return;
    }

    var serviceName = $"DscTestService_{Guid.NewGuid():N}";

    try
    {
        _resource.Set(new ServiceSchema
        {
            Name = serviceName,
            Path = testServicePath,
            StartType = ServiceStartMode.Manual
        });

        _resource.Set(new ServiceSchema
        {
            Name = serviceName,
            Description = "Updated service description"
        });

        var actual = _resource.Get(new ServiceSchema { Name = serviceName });

        actual.Description.Should().Be("Updated service description");
    }
    finally
    {
        try { _resource.Delete(new ServiceSchema { Name = serviceName }); } catch { }
    }
}
}
