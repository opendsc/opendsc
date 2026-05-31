// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using AwesomeAssertions;

using OpenDsc.Server.Services;

using Xunit;

namespace OpenDsc.Server.Tests.Services;

[Trait("Category", "Unit")]
public class ConfigDocumentSerializerTests
{
    [Fact]
    public void Serialize_WithNestedArrayProperty_DoesNotInsertBlankLinesBetweenArrayItems()
    {
        var serializer = new ConfigDocumentSerializer();
        var state = new ConfigDocumentState
        {
            Resources =
            [
                new ConfigResourceState
                {
                    Name = "Test",
                    Type = "Microsoft.Windows/Registry",
                    Properties =
                    {
                        ["valueData"] = ConfigPropertyValue.Literal(new Dictionary<string, object?>
                        {
                            ["Binary"] = new List<object?> { -4L, 3L, 6L }
                        })
                    }
                }
            ]
        };

        var yaml = serializer.Serialize(state);
        var normalizedYaml = yaml.Replace("\r\n", "\n");

        normalizedYaml.Should().Contain("        Binary:\n        - -4\n        - 3\n        - 6");
        yaml.Should().NotContain("\r\r\n");
    }
}
