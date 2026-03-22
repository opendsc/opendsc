// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Reflection;
using System.Text.Json;

using AwesomeAssertions;

using OpenDsc.Resource.Windows.OptionalFeature;

using Xunit;

using OptionalFeatureResource = OpenDsc.Resource.Windows.OptionalFeature.Resource;
using OptionalFeatureSchema = OpenDsc.Resource.Windows.OptionalFeature.Schema;

namespace OpenDsc.Resource.Windows.Tests.OptionalFeature;

[Trait("Category", "Integration")]
public sealed class OptionalFeatureTests
{
    private readonly OptionalFeatureResource _resource = new(SourceGenerationContext.Default);

    private bool IsDismAvailable()
    {
        try
        {
            _resource.Get(new OptionalFeatureSchema { Name = "NonExistentFeature_12345_XYZ" });
            return true;
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("Failed to initialize DISM API", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }
    }

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
        var attr = typeof(OptionalFeatureResource).GetCustomAttribute<OpenDsc.Resource.DscResourceAttribute>();

        attr.Should().NotBeNull();
        attr!.Type.Should().Be("OpenDsc.Windows/OptionalFeature");
        attr.Version.ToString().Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void Get_NonExistentFeature_ReturnsExistFalse()
    {
        if (!IsDismAvailable())
        {
            return;
        }

        var schema = new OptionalFeatureSchema { Name = "NonExistentFeature_12345_XYZ" };

        var result = _resource.Get(schema);

        result.Exist.Should().BeFalse();
        result.Name.Should().Be("NonExistentFeature_12345_XYZ");
    }

    [Fact]
    public void Get_ExistingFeature_ReturnsState()
    {
        if (!IsDismAvailable())
        {
            return;
        }

        const string featureName = "TelnetClient";

        var schema = new OptionalFeatureSchema { Name = featureName };

        var result = _resource.Get(schema);

        result.Name.Should().Be(featureName);
        result.Exist.Should().NotBeNull();
        result.DisplayName.Should().NotBeNull();
        result.Description.Should().NotBeNull();
    }

    [Fact]
    public void Export_NoFilter_ReturnsFeatures()
    {
        if (!IsDismAvailable())
        {
            return;
        }

        var results = _resource.Export(null).ToList();

        results.Should().NotBeEmpty();
        results.Should().AllSatisfy(r => r.Name.Should().NotBeNull());
    }

    [Fact(Skip = "Enabling/disabling Windows optional features is destructive for CI environments; run manually when needed.")]
    public void Set_FeatureState_Skipped()
    {
    }
}
