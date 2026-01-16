// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using Json.Schema.Generation;

namespace OpenDsc.Resource.SqlServer.Database;

[Description("Specifies the database recovery model.")]
public enum RecoveryModel
{
    [Description("Full recovery model. All transactions are logged and the log is kept until a backup is performed.")]
    Full,

    [Description("Bulk-logged recovery model. Minimally logs bulk operations to reduce log space usage.")]
    BulkLogged,

    [Description("Simple recovery model. Log is truncated automatically when transactions are committed.")]
    Simple
}
