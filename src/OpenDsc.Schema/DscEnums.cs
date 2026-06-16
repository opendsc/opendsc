// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Text.Json.Serialization;

namespace OpenDsc.Schema;

/// <summary>
/// DSC operation type.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<DscOperation>))]
public enum DscOperation
{
    /// <summary>
    /// Get operation - retrieve current state.
    /// </summary>
    Get,

    /// <summary>
    /// Set operation - apply desired state.
    /// </summary>
    Set,

    /// <summary>
    /// Test operation - check if in desired state.
    /// </summary>
    Test,

    /// <summary>
    /// Export operation - enumerate all instances.
    /// </summary>
    Export
}

/// <summary>
/// DSC execution kind.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<DscExecutionKind>))]
public enum DscExecutionKind
{
    /// <summary>
    /// Actual execution - changes are applied.
    /// </summary>
    Actual,

    /// <summary>
    /// WhatIf execution - preview changes without applying.
    /// </summary>
    WhatIf
}

/// <summary>
/// DSC security context.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<DscSecurityContext>))]
public enum DscSecurityContext
{
    /// <summary>
    /// Current security context.
    /// </summary>
    Current,

    /// <summary>
    /// Elevated security context (admin/root).
    /// </summary>
    Elevated,

    /// <summary>
    /// Restricted security context.
    /// </summary>
    Restricted
}

/// <summary>
/// DSC message level.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<DscMessageLevel>))]
public enum DscMessageLevel
{
    /// <summary>
    /// Error message level.
    /// </summary>
    Error,

    /// <summary>
    /// Warning message level.
    /// </summary>
    Warning,

    /// <summary>
    /// Information message level.
    /// </summary>
    Information
}

/// <summary>
/// DSC trace message level.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<DscTraceLevel>))]
public enum DscTraceLevel
{
    /// <summary>
    /// Error trace level.
    /// </summary>
    Error,

    /// <summary>
    /// Warning trace level.
    /// </summary>
    Warn,

    /// <summary>
    /// Info trace level.
    /// </summary>
    Info,

    /// <summary>
    /// Debug trace level.
    /// </summary>
    Debug,

    /// <summary>
    /// Trace level.
    /// </summary>
    Trace
}

/// <summary>
/// DSC scope for operations that can target user or machine level.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<DscScope>))]
public enum DscScope
{
    /// <summary>
    /// User scope - affects current user.
    /// </summary>
    User,

    /// <summary>
    /// Machine scope - affects entire machine.
    /// </summary>
    Machine
}

/// <summary>
/// DSC resource kind from the ResourceManifest schema.
/// Indicates the type or role of the DSC resource.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<DscResourceKind>))]
public enum DscResourceKind
{
    /// <summary>
    /// Standard DSC resource.
    /// </summary>
    Resource,

    /// <summary>
    /// DSC adapter resource for adapting other platforms.
    /// </summary>
    Adapter,

    /// <summary>
    /// DSC exporter resource for exporting configuration.
    /// </summary>
    Exporter,

    /// <summary>
    /// DSC importer resource for importing configuration.
    /// </summary>
    Importer,

    /// <summary>
    /// DSC group resource for grouping other resources.
    /// </summary>
    Group
}

/// <summary>
/// DSC resource capability indicating which operations the resource supports.
/// Maps to values from the DSC ResourceManifest Capability schema type.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<DscCapability>))]
public enum DscCapability
{
    /// <summary>
    /// The resource supports retrieving configuration (Get).
    /// </summary>
    Get,

    /// <summary>
    /// The resource supports applying configuration (Set).
    /// </summary>
    Set,

    /// <summary>
    /// The resource supports the _exist property directly.
    /// </summary>
    SetHandlesExist,

    /// <summary>
    /// The resource supports simulating configuration (WhatIf).
    /// </summary>
    WhatIf,

    /// <summary>
    /// The resource supports validating configuration (Test).
    /// </summary>
    Test,

    /// <summary>
    /// The resource supports deleting configuration.
    /// </summary>
    Delete,

    /// <summary>
    /// The resource supports exporting configuration (Export).
    /// </summary>
    Export,

    /// <summary>
    /// The resource supports resolving imported configuration.
    /// </summary>
    Resolve
}
