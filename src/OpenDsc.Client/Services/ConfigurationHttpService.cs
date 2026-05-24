// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Net.Http.Headers;
using System.Net.Http.Json;

using OpenDsc.Client.Http;
using OpenDsc.Contracts.Configurations;
using OpenDsc.Contracts.Permissions;

namespace OpenDsc.Client.Services;

/// <summary>
/// HTTP implementation of configuration operations.
/// </summary>
public sealed class ConfigurationHttpService(HttpClient client)
    : HttpServiceBase(client),
      IConfigurationPermissions,
      IConfigurationSettings,
      IConfigurationReader,
      IConfigurationManager,
      IConfigurationFileManager
{
    private static readonly ClientSerializerContext Ctx = ClientSerializerContext.Default;

    /// <inheritdoc />
    public async Task<IReadOnlyList<PermissionEntry>?> GetPermissionsAsync(
        string name,
        CancellationToken cancellationToken = default)
        => await GetOrNullAsync($"api/v1/configurations/{Uri.EscapeDataString(name)}/permissions", Ctx.PermissionEntryList, cancellationToken).ConfigureAwait(false);

    /// <inheritdoc />
    public Task GrantPermissionAsync(
        string name,
        GrantPermissionRequest request,
        CancellationToken cancellationToken = default)
        => PutAsync(
            $"api/v1/configurations/{Uri.EscapeDataString(name)}/permissions",
            request,
            Ctx.GrantPermissionRequest,
            cancellationToken);

    /// <inheritdoc />
    public Task RevokePermissionAsync(
        string name,
        RevokePermissionRequest request,
        CancellationToken cancellationToken = default)
        => base.DeleteAsync(
            $"api/v1/configurations/{Uri.EscapeDataString(name)}/permissions/{request.PrincipalType}/{request.PrincipalId}",
            cancellationToken);

    /// <inheritdoc />
    public Task<ConfigurationSettingsSummary?> GetSettingsAsync(
        string name,
        CancellationToken cancellationToken = default)
        => GetOrNullAsync(
            $"api/v1/configurations/{Uri.EscapeDataString(name)}/settings",
            Ctx.ConfigurationSettingsSummary,
            cancellationToken);

    /// <inheritdoc />
    public Task<ConfigurationSettingsSummary> UpdateSettingsAsync(
        string name,
        UpdateConfigurationSettingsRequest request,
        CancellationToken cancellationToken = default)
        => PutAsync(
            $"api/v1/configurations/{Uri.EscapeDataString(name)}/settings",
            request,
            Ctx.UpdateConfigurationSettingsRequest,
            Ctx.ConfigurationSettingsSummary,
            cancellationToken);

    /// <inheritdoc />
    public Task DeleteSettingsAsync(
        string name,
        CancellationToken cancellationToken = default)
        => base.DeleteAsync(
            $"api/v1/configurations/{Uri.EscapeDataString(name)}/settings",
            cancellationToken);

    /// <inheritdoc />
    public Task<ConfigurationRetentionSummary?> GetRetentionSettingsAsync(
        string name,
        CancellationToken cancellationToken = default)
        => GetOrNullAsync(
            $"api/v1/configurations/{Uri.EscapeDataString(name)}/settings/retention",
            Ctx.ConfigurationRetentionSummary,
            cancellationToken);

    /// <inheritdoc />
    public Task SaveRetentionSettingsAsync(
        string name,
        SaveRetentionSettingsRequest request,
        CancellationToken cancellationToken = default)
        => PutAsync(
            $"api/v1/configurations/{Uri.EscapeDataString(name)}/settings/retention",
            request,
            Ctx.SaveRetentionSettingsRequest,
            cancellationToken);

    /// <inheritdoc />
    public Task ResetRetentionSettingsAsync(
        string name,
        CancellationToken cancellationToken = default)
        => base.DeleteAsync(
            $"api/v1/configurations/{Uri.EscapeDataString(name)}/settings/retention",
            cancellationToken);

    /// <inheritdoc />
    public async Task<IReadOnlyList<ConfigurationSummary>> GetConfigurationsAsync(CancellationToken cancellationToken = default)
        => await GetAsync("api/v1/configurations", Ctx.ConfigurationSummaryList, cancellationToken).ConfigureAwait(false);

    /// <inheritdoc />
    public Task<ConfigurationDetails?> GetConfigurationAsync(string name, CancellationToken cancellationToken = default)
        => GetOrNullAsync($"api/v1/configurations/{Uri.EscapeDataString(name)}", Ctx.ConfigurationDetails, cancellationToken);

    /// <inheritdoc />
    public async Task<IReadOnlyList<ConfigurationVersionDetails>?> GetVersionsAsync(string name, CancellationToken cancellationToken = default)
        => await GetOrNullAsync($"api/v1/configurations/{Uri.EscapeDataString(name)}/versions", Ctx.ConfigurationVersionDetailsList, cancellationToken).ConfigureAwait(false);

    /// <inheritdoc />
    public async Task<IReadOnlyList<string>> GetConfigurationVersionListAsync(string name, CancellationToken cancellationToken = default)
        => await GetAsync($"api/v1/configurations/{Uri.EscapeDataString(name)}/versions/list", Ctx.StringList, cancellationToken).ConfigureAwait(false);

    /// <inheritdoc />
    public Task<bool> IsConfigurationAssignedAsync(string name, CancellationToken cancellationToken = default)
        => GetAsync($"api/v1/configurations/{Uri.EscapeDataString(name)}/assignment", Ctx.Boolean, cancellationToken);

    /// <inheritdoc />
    public Task<VersionUsageInfo> IsVersionInUseAsync(string name, string version, CancellationToken cancellationToken = default)
        => GetAsync($"api/v1/configurations/{Uri.EscapeDataString(name)}/versions/{Uri.EscapeDataString(version)}/usage", Ctx.VersionUsageInfo, cancellationToken);

    /// <inheritdoc />
    public Task<Guid?> GetParameterSchemaIdAsync(string name, CancellationToken cancellationToken = default)
        => GetAsync("api/v1/configurations/" + Uri.EscapeDataString(name) + "/parameter-schema-id", Ctx.NullableGuid, cancellationToken);

    /// <inheritdoc />
    public async Task<ConfigurationDetails> CreateAsync(CreateConfigurationAdminRequest request, CancellationToken cancellationToken = default)
    {
        using var form = new MultipartFormDataContent();
        form.Add(new StringContent(request.Name), "name");
        if (request.Description is not null)
            form.Add(new StringContent(request.Description), "description");
        form.Add(new StringContent(request.EntryPoint), "entryPoint");
        form.Add(new StringContent(request.Version), "version");
        form.Add(new StringContent(request.UseServerManagedParameters.ToString().ToLowerInvariant()), "useServerManagedParameters");
        foreach (var file in request.Files)
        {
            var fileContent = new StreamContent(file.Content);
            if (!string.IsNullOrEmpty(file.ContentType))
                fileContent.Headers.ContentType = new MediaTypeHeaderValue(file.ContentType);
            form.Add(fileContent, "files", file.FileName);
        }
        var response = await Client.PostAsync("api/v1/configurations", form, cancellationToken).ConfigureAwait(false);
        await EnsureSuccessAsync(response, cancellationToken).ConfigureAwait(false);
        return (await response.Content.ReadFromJsonAsync(Ctx.ConfigurationDetails, cancellationToken).ConfigureAwait(false))!;
    }

    /// <inheritdoc />
    public Task<ConfigurationDetails> UpdateAsync(string name, UpdateConfigurationAdminRequest request, CancellationToken cancellationToken = default)
        => PatchAsync($"api/v1/configurations/{Uri.EscapeDataString(name)}", request, Ctx.UpdateConfigurationAdminRequest, Ctx.ConfigurationDetails, cancellationToken);

    /// <inheritdoc />
    public new Task DeleteAsync(string name, CancellationToken cancellationToken = default)
        => base.DeleteAsync($"api/v1/configurations/{Uri.EscapeDataString(name)}", cancellationToken);

    /// <inheritdoc />
    public async Task<ConfigurationVersionDetails> CreateVersionAsync(string name, CreateConfigurationVersionRequest request, CancellationToken cancellationToken = default)
    {
        using var form = new MultipartFormDataContent();
        form.Add(new StringContent(request.Version), "version");
        if (request.EntryPoint is not null)
            form.Add(new StringContent(request.EntryPoint), "entryPoint");
        foreach (var file in request.Files)
        {
            var fileContent = new StreamContent(file.Content);
            if (!string.IsNullOrEmpty(file.ContentType))
                fileContent.Headers.ContentType = new MediaTypeHeaderValue(file.ContentType);
            form.Add(fileContent, "files", file.FileName);
        }
        var response = await Client.PostAsync($"api/v1/configurations/{Uri.EscapeDataString(name)}/versions", form, cancellationToken).ConfigureAwait(false);
        await EnsureSuccessAsync(response, cancellationToken).ConfigureAwait(false);
        return (await response.Content.ReadFromJsonAsync(Ctx.ConfigurationVersionDetails, cancellationToken).ConfigureAwait(false))!;
    }

    /// <inheritdoc />
    public Task<ConfigurationVersionDetails> CreateVersionFromExistingAsync(string name, CreateVersionFromExistingRequest request, CancellationToken cancellationToken = default)
        => PostAsync($"api/v1/configurations/{Uri.EscapeDataString(name)}/versions/from-existing", request, Ctx.CreateVersionFromExistingRequest, Ctx.ConfigurationVersionDetails, cancellationToken);

    /// <inheritdoc />
    public Task<PublishResult> PublishVersionAsync(string name, string version, CancellationToken cancellationToken = default)
        => PutAsync($"api/v1/configurations/{Uri.EscapeDataString(name)}/versions/{Uri.EscapeDataString(version)}/publish", Ctx.PublishResult, cancellationToken);

    /// <inheritdoc />
    public Task DeleteVersionAsync(string name, string version, CancellationToken cancellationToken = default)
        => base.DeleteAsync($"api/v1/configurations/{Uri.EscapeDataString(name)}/versions/{Uri.EscapeDataString(version)}", cancellationToken);

    /// <inheritdoc />
    public async Task AddFilesAsync(string name, string version, IReadOnlyList<FileUpload> files, CancellationToken cancellationToken = default)
    {
        using var form = new MultipartFormDataContent();
        foreach (var file in files)
        {
            var fileContent = new StreamContent(file.Content);
            if (!string.IsNullOrEmpty(file.ContentType))
                fileContent.Headers.ContentType = new MediaTypeHeaderValue(file.ContentType);
            form.Add(fileContent, "files", file.FileName);
        }

        var response = await Client.PostAsync($"api/v1/configurations/{Uri.EscapeDataString(name)}/versions/{Uri.EscapeDataString(version)}/files", form, cancellationToken).ConfigureAwait(false);
        await EnsureSuccessAsync(response, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public Task DeleteFileAsync(string name, string version, string filePath, CancellationToken cancellationToken = default)
        => base.DeleteAsync($"api/v1/configurations/{Uri.EscapeDataString(name)}/versions/{Uri.EscapeDataString(version)}/files/{Uri.EscapeDataString(filePath)}", cancellationToken);

    /// <inheritdoc />
    public async Task<Stream?> DownloadFileAsync(string name, string version, string filePath, CancellationToken cancellationToken = default)
    {
        var response = await Client.GetAsync(
            $"api/v1/configurations/{Uri.EscapeDataString(name)}/versions/{Uri.EscapeDataString(version)}/files/{Uri.EscapeDataString(filePath)}",
            cancellationToken).ConfigureAwait(false);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound) return null;
        await EnsureSuccessAsync(response, cancellationToken).ConfigureAwait(false);
        return await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public Task SaveFileAsync(string name, string version, string filePath, string content, CancellationToken cancellationToken = default)
        => PutAsync($"api/v1/configurations/{Uri.EscapeDataString(name)}/versions/{Uri.EscapeDataString(version)}/files/{Uri.EscapeDataString(filePath)}", content, Ctx.String, cancellationToken);

    /// <inheritdoc />
    public Task ChangeEntryPointAsync(string name, string version, string entryPoint, CancellationToken cancellationToken = default)
        => PutAsync($"api/v1/configurations/{Uri.EscapeDataString(name)}/versions/{Uri.EscapeDataString(version)}/entry-point", cancellationToken);
}
