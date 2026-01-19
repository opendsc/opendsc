// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using Json.Schema.Generation;

namespace OpenDsc.Resource.SqlServer;

[Description("Specifies the state of a permission.")]
public enum PermissionState
{
    [Description("Permission is granted.")]
    Grant,

    [Description("Permission is granted with the ability to grant to others.")]
    GrantWithGrant,

    [Description("Permission is denied.")]
    Deny
}
