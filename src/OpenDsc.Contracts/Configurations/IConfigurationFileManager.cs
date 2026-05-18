// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

namespace OpenDsc.Contracts.Configurations;

/// <summary>
/// File management operations within configuration versions.
/// </summary>
public interface IConfigurationFileManager
{
    /// <summary>
    /// Adds or uploads files to a specific configuration version.
    /// </summary>
    /// <param name="name">The name of the configuration.</param>
    /// <param name="version">The version identifier.</param>
    /// <param name="files">The files to add to the version.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    Task AddFilesAsync(string name, string version, IReadOnlyList<FileUpload> files, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a file from a configuration version.
    /// </summary>
    /// <param name="name">The name of the configuration.</param>
    /// <param name="version">The version identifier.</param>
    /// <param name="filePath">The path of the file to delete (relative to the configuration root).</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    Task DeleteFileAsync(string name, string version, string filePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Downloads a file from a configuration version.
    /// </summary>
    /// <param name="name">The name of the configuration.</param>
    /// <param name="version">The version identifier.</param>
    /// <param name="filePath">The path of the file to download (relative to the configuration root).</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A stream containing the file content, or null if the file is not found.</returns>
    Task<Stream?> DownloadFileAsync(string name, string version, string filePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates or creates a file in a configuration version.
    /// </summary>
    /// <param name="name">The name of the configuration.</param>
    /// <param name="version">The version identifier.</param>
    /// <param name="filePath">The path where to save the file (relative to the configuration root).</param>
    /// <param name="content">The file content as a string.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    Task SaveFileAsync(string name, string version, string filePath, string content, CancellationToken cancellationToken = default);

    /// <summary>
    /// Changes the entry point for a configuration version.
    /// </summary>
    /// <param name="name">The name of the configuration.</param>
    /// <param name="version">The version identifier.</param>
    /// <param name="entryPoint">The new entry point file path (e.g., "main.dsc.yaml").</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    Task ChangeEntryPointAsync(string name, string version, string entryPoint, CancellationToken cancellationToken = default);
}
