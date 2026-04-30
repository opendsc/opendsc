// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using AwesomeAssertions;

using Xunit;

namespace OpenDsc.Resource.Tests;

[Trait("Category", "Unit")]
public class DscResourceAttributeTests
{
    [Fact]
    public void Constructor_WithValidType_SetsTypeProperty()
    {
        var attr = new DscResourceAttribute("OpenDsc.Test/Resource");

        attr.Type.Should().Be("OpenDsc.Test/Resource");
    }

    [Fact]
    public void Constructor_WithValidTypeWithGroup_SetsTypeProperty()
    {
        var attr = new DscResourceAttribute("OpenDsc.Test.Group/Resource");

        attr.Type.Should().Be("OpenDsc.Test.Group/Resource");
    }

    [Fact]
    public void DefaultValues_SetReturnIsNone()
    {
        var attr = new DscResourceAttribute("OpenDsc.Test/Resource");

        attr.SetReturn.Should().Be(SetReturn.None);
    }

    [Fact]
    public void DefaultValues_TestReturnIsState()
    {
        var attr = new DscResourceAttribute("OpenDsc.Test/Resource");

        attr.TestReturn.Should().Be(TestReturn.State);
    }

    [Fact]
    public void DefaultValues_DescriptionIsEmpty()
    {
        var attr = new DscResourceAttribute("OpenDsc.Test/Resource");

        attr.Description.Should().BeEmpty();
    }

    [Fact]
    public void DefaultValues_TagsIsEmpty()
    {
        var attr = new DscResourceAttribute("OpenDsc.Test/Resource");

        attr.Tags.Should().BeEmpty();
    }

    [Fact]
    public void SetReturn_CanBeSetToState()
    {
        var attr = new DscResourceAttribute("OpenDsc.Test/Resource")
        {
            SetReturn = SetReturn.State
        };

        attr.SetReturn.Should().Be(SetReturn.State);
    }

    [Fact]
    public void SetReturn_CanBeSetToStateAndDiff()
    {
        var attr = new DscResourceAttribute("OpenDsc.Test/Resource")
        {
            SetReturn = SetReturn.StateAndDiff
        };

        attr.SetReturn.Should().Be(SetReturn.StateAndDiff);
    }

    [Fact]
    public void TestReturn_CanBeSetToStateAndDiff()
    {
        var attr = new DscResourceAttribute("OpenDsc.Test/Resource")
        {
            TestReturn = TestReturn.StateAndDiff
        };

        attr.TestReturn.Should().Be(TestReturn.StateAndDiff);
    }

    [Fact]
    public void Description_CanBeSet()
    {
        var attr = new DscResourceAttribute("OpenDsc.Test/Resource")
        {
            Description = "Test description"
        };

        attr.Description.Should().Be("Test description");
    }

    [Fact]
    public void Tags_CanBeSet()
    {
        var tags = new[] { "tag1", "tag2" };
        var attr = new DscResourceAttribute("OpenDsc.Test/Resource")
        {
            Tags = tags
        };

        attr.Tags.Should().Equal(tags);
    }

    [Fact]
    public void AttributeAppliedToClass_CanBeRetrieved()
    {
        var attr = typeof(TestResource).GetCustomAttributes(typeof(DscResourceAttribute), false)
            .FirstOrDefault() as DscResourceAttribute;

        attr.Should().NotBeNull();
        attr!.Type.Should().Be("OpenDsc.Test/TestResource");
    }
}
