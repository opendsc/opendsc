// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

namespace OpenDsc.Contracts.Parameters;

/// <summary>
/// Schema and parameter file operations.
/// </summary>
public interface IParameterSchemaManager
{
    Task<IReadOnlyList<ParameterSchemaDetails>> GetSchemasAsync(
        CancellationToken cancellationToken = default);

    Task<ParameterSchemaDetails?> GetSchemaAsync(
        Guid configurationId,
        int? majorVersion = null,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ParameterFileDetails>> GetSchemaFilesAsync(
        Guid schemaId,
        CancellationToken cancellationToken = default);

    Task<PublishResult> UploadSchemaAsync(
        Guid configurationId,
        string version,
        Stream content,
        CancellationToken cancellationToken = default);

    Task<ValidationResult> ValidateAsync(
        Guid configurationId,
        string version,
        string parameterContent,
        CancellationToken cancellationToken = default);
}
