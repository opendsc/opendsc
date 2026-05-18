// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

namespace OpenDsc.Contracts.Parameters;

/// <summary>
/// Schema and parameter file operations.
/// </summary>
public interface IParameterSchemaManager
{
    /// <summary>
    /// Gets all parameter schemas available on the server.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A read-only list of parameter schema details.</returns>
    Task<IReadOnlyList<ParameterSchemaDetails>> GetSchemasAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a specific parameter schema for a configuration.
    /// </summary>
    /// <param name="configurationId">The unique identifier of the configuration.</param>
    /// <param name="majorVersion">Optional major version number to retrieve a specific schema version (or null for latest).</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>Parameter schema details, or null if not found.</returns>
    Task<ParameterSchemaDetails?> GetSchemaAsync(
        Guid configurationId,
        int? majorVersion = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all parameter files belonging to a schema.
    /// </summary>
    /// <param name="schemaId">The unique identifier of the schema.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A read-only list of parameter file details for the schema.</returns>
    Task<IReadOnlyList<ParameterFileDetails>> GetSchemaFilesAsync(
        Guid schemaId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Uploads a new parameter schema for a configuration version.
    /// </summary>
    /// <param name="configurationId">The unique identifier of the configuration.</param>
    /// <param name="version">The configuration version identifier.</param>
    /// <param name="content">The JSON schema content as a stream.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>Information about the upload operation, including any validation results.</returns>
    Task<PublishResult> UploadSchemaAsync(
        Guid configurationId,
        string version,
        Stream content,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates parameter content against a specific schema version.
    /// </summary>
    /// <param name="configurationId">The unique identifier of the configuration.</param>
    /// <param name="version">The configuration version identifier.</param>
    /// <param name="parameterContent">The parameter content to validate (typically JSON).</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>Validation results including any errors or warnings.</returns>
    Task<ValidationResult> ValidateAsync(
        Guid configurationId,
        string version,
        string parameterContent,
        CancellationToken cancellationToken = default);
}
