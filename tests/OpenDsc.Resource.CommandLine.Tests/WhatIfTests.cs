// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Text.Json;

using Xunit;

namespace OpenDsc.Resource.CommandLine.Tests;

[Trait("Category", "Unit")]
public class WhatIfManifestTests
{
    private static readonly JsonSerializerOptions IgnoreNullOptions = new()
    {
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    [Fact]
    public void GenerateManifest_WhatIfResource_SetArgsContainWhatIfArg()
    {
        // Arrange
        var resource = new TestResourceWithWhatIf();

        // Act
        var manifest = ManifestBuilder.Build(resource);

        // Assert
        Assert.NotNull(manifest.Set);
        Assert.NotNull(manifest.Set.Args);
        var whatIfArg = Assert.Single(manifest.Set.Args.OfType<WhatIfArg>());
        Assert.Equal("--what-if", whatIfArg.Arg);
    }

    [Fact]
    public void GenerateManifest_WhatIfResource_SetHasWhatIfReturn()
    {
        // Arrange
        var resource = new TestResourceWithWhatIf();

        // Act
        var manifest = ManifestBuilder.Build(resource);

        // Assert
        Assert.NotNull(manifest.Set);
        Assert.Equal("stateAndDiff", manifest.Set.WhatIfReturn);
    }

    [Fact]
    public void GenerateManifest_WhatIfResource_WhatIfArgIsLastSetArg()
    {
        // Arrange
        var resource = new TestResourceWithWhatIf();

        // Act
        var manifest = ManifestBuilder.Build(resource);

        // Assert
        Assert.NotNull(manifest.Set);
        Assert.NotNull(manifest.Set.Args);
        Assert.IsType<WhatIfArg>(manifest.Set.Args[^1]);
    }

    [Fact]
    public void GenerateManifest_DeleteWhatIfResource_DeleteArgsContainWhatIfArg()
    {
        // Arrange
        var resource = new TestResourceWithWhatIf();

        // Act
        var manifest = ManifestBuilder.Build(resource);

        // Assert
        Assert.NotNull(manifest.Delete);
        Assert.NotNull(manifest.Delete.Args);
        var whatIfArg = Assert.Single(manifest.Delete.Args.OfType<WhatIfArg>());
        Assert.Equal("--what-if", whatIfArg.Arg);
    }

    [Fact]
    public void GenerateManifest_WhatIfReturnNone_OmitsWhatIfReturn()
    {
        // Arrange
        var resource = new TestResourceWhatIfNoReturn();

        // Act
        var manifest = ManifestBuilder.Build(resource);

        // Assert
        Assert.NotNull(manifest.Set);
        Assert.NotNull(manifest.Set.Args);
        Assert.Single(manifest.Set.Args.OfType<WhatIfArg>());
        Assert.Null(manifest.Set.WhatIfReturn);
    }

    [Fact]
    public void GenerateManifest_NonWhatIfResource_HasNoWhatIfArtifacts()
    {
        // Arrange
        var resource = new TestResourceAll();

        // Act
        var manifest = ManifestBuilder.Build(resource);

        // Assert
        Assert.NotNull(manifest.Set);
        Assert.NotNull(manifest.Set.Args);
        Assert.Empty(manifest.Set.Args.OfType<WhatIfArg>());
        Assert.Null(manifest.Set.WhatIfReturn);
        Assert.NotNull(manifest.Delete);
        Assert.NotNull(manifest.Delete.Args);
        Assert.Empty(manifest.Delete.Args.OfType<WhatIfArg>());
    }

    [Fact]
    public void GenerateManifest_WhatIfResource_SerializesWhatIfArgAndReturns()
    {
        // Arrange
        var resource = new TestResourceWithWhatIf();
        var manifest = ManifestBuilder.Build(resource);

        // Act
        var json = JsonSerializer.Serialize(manifest);

        // Assert
        Assert.Contains("\"whatIfArg\":\"--what-if\"", json);
        Assert.Contains("\"whatIfReturns\":\"stateAndDiff\"", json);
    }

    [Fact]
    public void GenerateManifest_NonWhatIfResource_SerializationOmitsWhatIf()
    {
        // Arrange
        var resource = new TestResourceAll();
        var manifest = ManifestBuilder.Build(resource);

        // Act
        // Match the production serialization contract, which omits null properties.
        var json = JsonSerializer.Serialize(manifest, IgnoreNullOptions);

        // Assert
        Assert.DoesNotContain("whatIfArg", json);
        Assert.DoesNotContain("whatIfReturns", json);
    }
}

[Trait("Category", "Unit")]
public class WhatIfCommandTests
{
    [Fact]
    public void SetCommand_HasWhatIfOption()
    {
        // Arrange
        var builder = new CommandBuilder();
        builder.AddResource<TestResourceWithWhatIf, TestSchema>(new TestResourceWithWhatIf());

        // Act
        var root = builder.Build();
        var setCommand = root.Subcommands.First(c => c.Name == "set");

        // Assert
        var whatIfOption = setCommand.Options.FirstOrDefault(o => o.Name == "--what-if");
        Assert.NotNull(whatIfOption);
        Assert.Contains("-w", whatIfOption.Aliases);
    }

    [Fact]
    public void DeleteCommand_HasWhatIfOption()
    {
        // Arrange
        var builder = new CommandBuilder();
        builder.AddResource<TestResourceWithWhatIf, TestSchema>(new TestResourceWithWhatIf());

        // Act
        var root = builder.Build();
        var deleteCommand = root.Subcommands.First(c => c.Name == "delete");

        // Assert
        var whatIfOption = deleteCommand.Options.FirstOrDefault(o => o.Name == "--what-if");
        Assert.NotNull(whatIfOption);
        Assert.Contains("-w", whatIfOption.Aliases);
    }

    [Fact]
    public void SetCommand_ParsesWhatIfOption()
    {
        // Arrange
        var builder = new CommandBuilder();
        builder.AddResource<TestResourceWithWhatIf, TestSchema>(new TestResourceWithWhatIf());
        var root = builder.Build();

        // Act
        var parseResult = root.Parse(["set", "--input", """{"name":"test"}""", "--what-if"]);

        // Assert
        Assert.Empty(parseResult.Errors);
    }

    [Fact]
    public void SetCommand_ParsesShortWhatIfAlias()
    {
        // Arrange
        var builder = new CommandBuilder();
        builder.AddResource<TestResourceWithWhatIf, TestSchema>(new TestResourceWithWhatIf());
        var root = builder.Build();

        // Act
        var parseResult = root.Parse(["set", "-i", """{"name":"test"}""", "-w"]);

        // Assert
        Assert.Empty(parseResult.Errors);
    }

    [Fact]
    public void DeleteCommand_ParsesWhatIfOption_MultiResource()
    {
        // Arrange
        var builder = new CommandBuilder();
        builder.AddResource<TestResourceWithWhatIf, TestSchema>(new TestResourceWithWhatIf());
        builder.AddResource<TestResourceAll, TestSchema>(new TestResourceAll());
        var root = builder.Build();

        // Act
        var parseResult = root.Parse(["delete", "--resource", "TestResource/WhatIf", "--input", """{"name":"test"}""", "--what-if"]);

        // Assert
        Assert.Empty(parseResult.Errors);
    }

    [Fact]
    public void SetCommand_NonWhatIfResource_HasNoWhatIfOption()
    {
        // Arrange
        var builder = new CommandBuilder();
        builder.AddResource<TestResourceAll, TestSchema>(new TestResourceAll());

        // Act
        var root = builder.Build();
        var setCommand = root.Subcommands.First(c => c.Name == "set");

        // Assert
        Assert.DoesNotContain(setCommand.Options, o => o.Name == "--what-if");
    }

    [Fact]
    public void DeleteCommand_NonWhatIfResource_HasNoWhatIfOption()
    {
        // Arrange
        var builder = new CommandBuilder();
        builder.AddResource<TestResourceAll, TestSchema>(new TestResourceAll());

        // Act
        var root = builder.Build();
        var deleteCommand = root.Subcommands.First(c => c.Name == "delete");

        // Assert
        Assert.DoesNotContain(deleteCommand.Options, o => o.Name == "--what-if");
    }

    [Fact]
    public void SetCommand_NonWhatIfResource_WhatIfArgumentProducesParseError()
    {
        // Arrange
        var builder = new CommandBuilder();
        builder.AddResource<TestResourceAll, TestSchema>(new TestResourceAll());
        var root = builder.Build();

        // Act
        var parseResult = root.Parse(["set", "--input", """{"name":"test"}""", "--what-if"]);

        // Assert
        Assert.NotEmpty(parseResult.Errors);
    }

    [Fact]
    public void SetCommand_MixedResources_HasWhatIfOption()
    {
        // Arrange
        var builder = new CommandBuilder();
        builder.AddResource<TestResourceWithWhatIf, TestSchema>(new TestResourceWithWhatIf());
        builder.AddResource<TestResourceAll, TestSchema>(new TestResourceAll());

        // Act
        var root = builder.Build();
        var setCommand = root.Subcommands.First(c => c.Name == "set");

        // Assert
        Assert.Contains(setCommand.Options, o => o.Name == "--what-if");
    }
}
