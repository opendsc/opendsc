// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.IO;
using System.Linq;
using System.Reflection;
using System.ServiceProcess;
using System.Text.Json;

using AwesomeAssertions;

using OpenDsc.Schema;

using Xunit;

using ServiceResource = OpenDsc.Resource.Windows.Service.Resource;
using ServiceSchema = OpenDsc.Resource.Windows.Service.Schema;

namespace OpenDsc.Resource.Windows.Tests.Service;

[Trait("Category", "Integration")]
public sealed class ServiceTests
{
    private readonly ServiceResource _resource = new(SourceGenerationContext.Default);

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
        var attr = typeof(ServiceResource).GetCustomAttribute<OpenDsc.Resource.DscResourceAttribute>();

        attr.Should().NotBeNull();
        attr!.Type.Should().Be("OpenDsc.Windows/Service");
        attr.Version.ToString().Should().NotBeNullOrEmpty();
    }

    [WindowsOnlyFact]
    public void Get_NonExistentService_ReturnsExistFalse()
    {
        var schema = new ServiceSchema { Name = "NonExistentService_12345" };

        var result = _resource.Get(schema);

        result.Exist.Should().BeFalse();
        result.Name.Should().Be("NonExistentService_12345");
    }

    [WindowsOnlyFact]
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

    [WindowsOnlyFact]
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

        if (!File.Exists(testServicePath))
        {
            // If test artifact is missing, skip gracefully.
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
}
