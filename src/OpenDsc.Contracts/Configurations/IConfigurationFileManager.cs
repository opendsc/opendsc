// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

namespace OpenDsc.Contracts.Configurations;

/// <summary>
/// File management operations within configuration versions.
/// </summary>
public interface IConfigurationFileManager
{
    Task AddFilesAsync(string name, string version, IReadOnlyList<FileUpload> files, CancellationToken cancellationToken = default);
    Task DeleteFileAsync(string name, string version, string filePath, CancellationToken cancellationToken = default);
    Task<Stream?> DownloadFileAsync(string name, string version, string filePath, CancellationToken cancellationToken = default);
    Task SaveFileAsync(string name, string version, string filePath, string content, CancellationToken cancellationToken = default);
    Task ChangeEntryPointAsync(string name, string version, string entryPoint, CancellationToken cancellationToken = default);
}
