// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Reflection;
using System.Text.Json;

using AwesomeAssertions;

using Xunit;

using OptionalFeatureResource = OpenDsc.Resource.Windows.OptionalFeature.Resource;
using OptionalFeatureSchema = OpenDsc.Resource.Windows.OptionalFeature.Schema;

namespace OpenDsc.Resource.Windows.Tests.OptionalFeature;

[Trait("Category", "Integration")]
public sealed class OptionalFeatureTests
{
    private readonly OptionalFeatureResource _resource = new(SourceGenerationContext.Default);

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
        var attr = typeof(OptionalFeatureResource).GetCustomAttribute<OpenDsc.Resource.DscResourceAttribute>();

        attr.Should().NotBeNull();
        attr!.Type.Should().Be("OpenDsc.Windows/OptionalFeature");
        attr.Version.ToString().Should().NotBeNullOrEmpty();
    }

    [RequiresDismFact]
    public void Get_NonExistentFeature_ReturnsExistFalse()
    {
        var schema = new OptionalFeatureSchema { Name = "NonExistentFeature_12345_XYZ" };

        var result = _resource.Get(schema);

        result.Exist.Should().BeFalse();
        result.Name.Should().Be("NonExistentFeature_12345_XYZ");
    }

    [RequiresDismFact]
    public void Get_ExistingFeature_ReturnsState()
    {
        const string featureName = "TelnetClient";

        var schema = new OptionalFeatureSchema { Name = featureName };

        var result = _resource.Get(schema);

        result.Name.Should().Be(featureName);
        result.Exist.Should().NotBeNull();
        result.DisplayName.Should().NotBeNull();
        result.Description.Should().NotBeNull();
    }

    [RequiresDismFact]
    public void Export_NoFilter_ReturnsFeatures()
    {
        var results = _resource.Export(null).ToList();

        results.Should().NotBeEmpty();
        results.Should().AllSatisfy(r => r.Name.Should().NotBeNull());
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

    [RequiresDismFact]
    public void Get_ExistingFeature_HasAllProperties()
    {
        const string featureName = "TelnetClient";
        var schema = new OptionalFeatureSchema { Name = featureName };

        var result = _resource.Get(schema);

        result.Name.Should().Be(featureName);
        result.Should().NotBeNull();
    }

    [Fact]
    public void GetSchema_JsonIsValid()
    {
        var schemaJson = _resource.GetSchema();
        var doc = JsonDocument.Parse(schemaJson);

        doc.RootElement.ValueKind.Should().Be(JsonValueKind.Object);
    }

    [RequiresDismFact]
    public void Export_IncludesMultipleFeatures()
    {
        var results = _resource.Export(null).ToList();

        results.Should().NotBeEmpty();
        results.Should().AllSatisfy(r => r.Name.Should().NotBeNullOrEmpty());
    }
}
