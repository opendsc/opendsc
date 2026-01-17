// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using Json.Schema.Generation;

namespace OpenDsc.Resource.SqlServer.Database;

[Description("Specifies the database user access mode.")]
public enum UserAccess
{
    [Description("Multiple users can access the database.")]
    Multiple,

    [Description("Only one user can access the database at a time.")]
    Single,

    [Description("Only database administrators can access the database.")]
    Restricted
}
