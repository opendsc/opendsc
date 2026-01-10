// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

namespace OpenDsc.Schema;

/// <summary>
/// Exit codes returned by the DSC CLI.
/// </summary>
public enum DscExitCode
{
    /// <summary>
    /// The command executed successfully without any errors.
    /// </summary>
    Success = 0,

    /// <summary>
    /// The command failed because it received invalid arguments.
    /// </summary>
    InvalidArgument = 1,

    /// <summary>
    /// The command failed because a resource raised an error.
    /// </summary>
    ResourceError = 2,

    /// <summary>
    /// The command failed because a value couldn't be serialized to or deserialized from JSON.
    /// </summary>
    JsonSerializationError = 3,

    /// <summary>
    /// The command failed because input for the command wasn't valid YAML or JSON.
    /// </summary>
    InvalidInput = 4,

    /// <summary>
    /// The command failed because a resource definition or instance value was invalid against its JSON schema.
    /// </summary>
    SchemaValidationError = 5,

    /// <summary>
    /// The command was cancelled by a Ctrl+C interruption.
    /// </summary>
    Cancelled = 6
}
