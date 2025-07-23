using System.Text.Json.Serialization;

namespace Temp;

public sealed class Schema
{
    [JsonRequired]
    public string Name { get; set; } = string.Empty;

    public string? DisplayName { get; set; }

    public int MyProperty { get; set; }

    public bool IsEnabled { get; set; }

    [JsonPropertyName("_exist")]
    public bool? Exist { get; set; }

    [JsonPropertyName("_inDesiredState")]
    public bool? InDesiredState { get; set; }
}
