// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using Json.Schema.Generation;

namespace OpenDsc.Resource.SqlServer.Database;

[Description("Specifies the database containment type.")]
public enum ContainmentType
{
    [Description("Database is not contained.")]
    None,

    [Description("Database is partially contained.")]
    Partial
}
