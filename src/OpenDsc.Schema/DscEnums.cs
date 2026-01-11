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
