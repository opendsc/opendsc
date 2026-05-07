// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using Microsoft.AspNetCore.Components.Forms;

namespace OpenDsc.Server.Infrastructure;

/// <summary>
/// Adapts an ASP.NET Core <see cref="IFormFile"/> to the Blazor <see cref="IBrowserFile"/> interface
/// so REST endpoints can reuse service methods designed for Blazor file uploads.
/// </summary>
internal sealed class FormFileBrowserFileAdapter : IBrowserFile
{
    private readonly IFormFile _formFile;

    public FormFileBrowserFileAdapter(IFormFile formFile)
    {
        _formFile = formFile;
    }

    public string Name => _formFile.FileName;
    public DateTimeOffset LastModified => DateTimeOffset.UtcNow;
    public long Size => _formFile.Length;
    public string ContentType => _formFile.ContentType;

    public Stream OpenReadStream(long maxAllowedSize = 512000, CancellationToken cancellationToken = default)
        => _formFile.OpenReadStream();
}
