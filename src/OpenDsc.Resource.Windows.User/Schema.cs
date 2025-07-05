// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Text.Json.Serialization;

namespace OpenDsc.Resource.Windows.User;

public sealed class Schema
{
    [JsonRequired]
    public string userName { get; set; } = string.Empty;

    public string? fullName { get; set; }

    public string? description { get; set; }

    public string? password { get; set; }

    public bool? disabled { get; set; }

    public bool? passwordNeverExpires { get; set; }

    public bool? passwordChangeRequired { get; set; }

    public bool? passwordChangeNotAllowed { get; set; }

    [JsonPropertyName("_exist")]
    public bool? exist { get; set; }
}
