// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Net.Http.Headers;
using System.Net.Http.Json;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using OpenDsc.Schema;

namespace OpenDsc.Lcm;

/// <summary>
/// HTTP client for communicating with the DSC pull server.
/// </summary>
public partial class PullServerClient : IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly IOptionsMonitor<LcmConfig> _lcmMonitor;
    private readonly ILogger<PullServerClient> _logger;

    public PullServerClient(
        HttpClient httpClient,
        IOptionsMonitor<LcmConfig> lcmMonitor,
        ILogger<PullServerClient> logger)
    {
        _httpClient = httpClient;
        _lcmMonitor = lcmMonitor;
        _logger = logger;
    }

    /// <summary>
    /// Registers the node with the pull server.
    /// </summary>
    /// <returns>Registration result with node ID and API key.</returns>
    public async Task<RegisterNodeResult?> RegisterAsync(CancellationToken cancellationToken = default)
    {
        var config = _lcmMonitor.CurrentValue;
        var pullServer = config.PullServer;

        if (pullServer is null || string.IsNullOrWhiteSpace(pullServer.ServerUrl))
        {
            LogPullServerNotConfigured();
            return null;
        }

        if (string.IsNullOrWhiteSpace(pullServer.RegistrationKey))
        {
            LogRegistrationKeyNotConfigured();
            return null;
        }

        var fqdn = Environment.MachineName;
        try
        {
            fqdn = System.Net.Dns.GetHostEntry(Environment.MachineName).HostName;
        }
        catch (Exception ex)
        {
            LogFqdnResolutionFailed(ex, Environment.MachineName);
        }

        var request = new RegisterNodeRequest
        {
            RegistrationKey = pullServer.RegistrationKey,
            Fqdn = fqdn
        };

        try
        {
            using var response = await _httpClient.PostAsJsonAsync(
                $"{pullServer.ServerUrl}/api/v1/nodes/register",
                request,
                PullServerJsonContext.Default.RegisterNodeRequest,
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(cancellationToken);
                LogRegistrationFailed(response.StatusCode.ToString(), error);
                return null;
            }

            var result = await response.Content.ReadFromJsonAsync(
                PullServerJsonContext.Default.RegisterNodeResponse,
                cancellationToken);

            if (result is null)
            {
                LogRegistrationResponseInvalid();
                return null;
            }

            LogRegistrationSucceeded(result.NodeId);

            return new RegisterNodeResult
            {
                NodeId = result.NodeId,
                ApiKey = result.ApiKey,
                KeyRotationInterval = result.KeyRotationInterval
            };
        }
        catch (Exception ex)
        {
            LogRegistrationError(ex);
            return null;
        }
    }

    /// <summary>
    /// Checks if the configuration has changed by comparing checksums.
    /// </summary>
    /// <returns>True if configuration changed, false otherwise.</returns>
    public async Task<bool> HasConfigurationChangedAsync(CancellationToken cancellationToken = default)
    {
        var config = _lcmMonitor.CurrentValue;
        var pullServer = config.PullServer;

        if (pullServer is null || pullServer.NodeId is null || string.IsNullOrWhiteSpace(pullServer.ApiKey))
        {
            return false;
        }

        try
        {
            SetAuthorizationHeader(pullServer.ApiKey);

            using var response = await _httpClient.GetAsync(
                $"{pullServer.ServerUrl}/api/v1/nodes/{pullServer.NodeId}/configuration/checksum",
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                return false;
            }

            var result = await response.Content.ReadFromJsonAsync(
                PullServerJsonContext.Default.ConfigurationChecksumResponse,
                cancellationToken);

            if (result is null)
            {
                return false;
            }

            return result.Checksum != pullServer.ConfigurationChecksum;
        }
        catch (Exception ex)
        {
            LogChecksumCheckError(ex);
            return false;
        }
    }

    /// <summary>
    /// Downloads the configuration from the pull server.
    /// </summary>
    /// <returns>The configuration content, or null if download failed.</returns>
    public async Task<string?> GetConfigurationAsync(CancellationToken cancellationToken = default)
    {
        var config = _lcmMonitor.CurrentValue;
        var pullServer = config.PullServer;

        if (pullServer is null || pullServer.NodeId is null || string.IsNullOrWhiteSpace(pullServer.ApiKey))
        {
            LogPullServerNotConfigured();
            return null;
        }

        try
        {
            SetAuthorizationHeader(pullServer.ApiKey);

            using var response = await _httpClient.GetAsync(
                $"{pullServer.ServerUrl}/api/v1/nodes/{pullServer.NodeId}/configuration",
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                LogConfigurationDownloadFailed(response.StatusCode.ToString());
                return null;
            }

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            LogConfigurationDownloaded();
            return content;
        }
        catch (Exception ex)
        {
            LogConfigurationDownloadError(ex);
            return null;
        }
    }

    /// <summary>
    /// Gets the current configuration checksum from the server.
    /// </summary>
    public async Task<string?> GetConfigurationChecksumAsync(CancellationToken cancellationToken = default)
    {
        var config = _lcmMonitor.CurrentValue;
        var pullServer = config.PullServer;

        if (pullServer is null || pullServer.NodeId is null || string.IsNullOrWhiteSpace(pullServer.ApiKey))
        {
            return null;
        }

        try
        {
            SetAuthorizationHeader(pullServer.ApiKey);

            using var response = await _httpClient.GetAsync(
                $"{pullServer.ServerUrl}/api/v1/nodes/{pullServer.NodeId}/configuration/checksum",
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            var result = await response.Content.ReadFromJsonAsync(
                PullServerJsonContext.Default.ConfigurationChecksumResponse,
                cancellationToken);

            return result?.Checksum;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Submits a compliance report to the server.
    /// </summary>
    public async Task<bool> SubmitReportAsync(DscOperation operation, DscResult result, CancellationToken cancellationToken = default)
    {
        var config = _lcmMonitor.CurrentValue;
        var pullServer = config.PullServer;

        if (pullServer is null || pullServer.NodeId is null || string.IsNullOrWhiteSpace(pullServer.ApiKey))
        {
            return false;
        }

        if (!pullServer.ReportCompliance)
        {
            return true;
        }

        try
        {
            SetAuthorizationHeader(pullServer.ApiKey);

            var request = new SubmitReportRequest
            {
                Operation = operation,
                Result = result
            };

            using var response = await _httpClient.PostAsJsonAsync(
                $"{pullServer.ServerUrl}/api/v1/nodes/{pullServer.NodeId}/reports",
                request,
                PullServerJsonContext.Default.SubmitReportRequest,
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                LogReportSubmissionFailed(response.StatusCode.ToString());
                return false;
            }

            LogReportSubmitted();
            return true;
        }
        catch (Exception ex)
        {
            LogReportSubmissionError(ex);
            return false;
        }
    }

    /// <summary>
    /// Rotates the API key.
    /// </summary>
    /// <returns>The new API key, or null if rotation failed.</returns>
    public async Task<RotateKeyResult?> RotateApiKeyAsync(CancellationToken cancellationToken = default)
    {
        var config = _lcmMonitor.CurrentValue;
        var pullServer = config.PullServer;

        if (pullServer is null || pullServer.NodeId is null || string.IsNullOrWhiteSpace(pullServer.ApiKey))
        {
            return null;
        }

        try
        {
            SetAuthorizationHeader(pullServer.ApiKey);

            using var response = await _httpClient.PostAsync(
                $"{pullServer.ServerUrl}/api/v1/nodes/{pullServer.NodeId}/rotate-key",
                null,
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                LogKeyRotationFailed(response.StatusCode.ToString());
                return null;
            }

            var result = await response.Content.ReadFromJsonAsync(
                PullServerJsonContext.Default.RotateKeyResponse,
                cancellationToken);

            if (result is null)
            {
                return null;
            }

            LogKeyRotated();

            return new RotateKeyResult
            {
                ApiKey = result.ApiKey,
                KeyRotationInterval = result.KeyRotationInterval
            };
        }
        catch (Exception ex)
        {
            LogKeyRotationError(ex);
            return null;
        }
    }

    private void SetAuthorizationHeader(string apiKey)
    {
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
    }

    public void Dispose()
    {
        _httpClient.Dispose();
    }

    [LoggerMessage(EventId = 1001, Level = LogLevel.Error, Message = "Pull server not configured")]
    private partial void LogPullServerNotConfigured();

    [LoggerMessage(EventId = 1002, Level = LogLevel.Error, Message = "Registration key not configured")]
    private partial void LogRegistrationKeyNotConfigured();

    [LoggerMessage(EventId = 1003, Level = LogLevel.Error, Message = "Registration failed: {StatusCode} - {Error}")]
    private partial void LogRegistrationFailed(string statusCode, string error);

    [LoggerMessage(EventId = 1004, Level = LogLevel.Error, Message = "Registration response was invalid")]
    private partial void LogRegistrationResponseInvalid();

    [LoggerMessage(EventId = 1005, Level = LogLevel.Information, Message = "Node registered successfully: {NodeId}")]
    private partial void LogRegistrationSucceeded(Guid nodeId);

    [LoggerMessage(EventId = 1006, Level = LogLevel.Error, Message = "Registration error")]
    private partial void LogRegistrationError(Exception ex);

    [LoggerMessage(EventId = 1007, Level = LogLevel.Warning, Message = "Error checking configuration checksum")]
    private partial void LogChecksumCheckError(Exception ex);

    [LoggerMessage(EventId = 1008, Level = LogLevel.Error, Message = "Configuration download failed: {StatusCode}")]
    private partial void LogConfigurationDownloadFailed(string statusCode);

    [LoggerMessage(EventId = 1009, Level = LogLevel.Information, Message = "Configuration downloaded from server")]
    private partial void LogConfigurationDownloaded();

    [LoggerMessage(EventId = 1010, Level = LogLevel.Error, Message = "Configuration download error")]
    private partial void LogConfigurationDownloadError(Exception ex);

    [LoggerMessage(EventId = 1011, Level = LogLevel.Warning, Message = "Report submission failed: {StatusCode}")]
    private partial void LogReportSubmissionFailed(string statusCode);

    [LoggerMessage(EventId = 1012, Level = LogLevel.Debug, Message = "Compliance report submitted")]
    private partial void LogReportSubmitted();

    [LoggerMessage(EventId = 1013, Level = LogLevel.Warning, Message = "Report submission error")]
    private partial void LogReportSubmissionError(Exception ex);

    [LoggerMessage(EventId = 1014, Level = LogLevel.Warning, Message = "API key rotation failed: {StatusCode}")]
    private partial void LogKeyRotationFailed(string statusCode);

    [LoggerMessage(EventId = 1015, Level = LogLevel.Information, Message = "API key rotated successfully")]
    private partial void LogKeyRotated();

    [LoggerMessage(EventId = 1016, Level = LogLevel.Warning, Message = "API key rotation error")]
    private partial void LogKeyRotationError(Exception ex);

    [LoggerMessage(EventId = 1017, Level = LogLevel.Debug, Message = "Failed to resolve fully qualified domain name; using machine name '{MachineName}' instead.")]
    private partial void LogFqdnResolutionFailed(Exception ex, string machineName);
}

/// <summary>
/// Result of node registration.
/// </summary>
public sealed class RegisterNodeResult
{
    public Guid NodeId { get; set; }
    public string ApiKey { get; set; } = string.Empty;
    public TimeSpan KeyRotationInterval { get; set; }
}

/// <summary>
/// Result of key rotation.
/// </summary>
public sealed class RotateKeyResult
{
    public string ApiKey { get; set; } = string.Empty;
    public TimeSpan KeyRotationInterval { get; set; }
}

/// <summary>
/// Request to register a node with the server.
/// </summary>
public sealed class RegisterNodeRequest
{
    public string RegistrationKey { get; set; } = string.Empty;
    public string Fqdn { get; set; } = string.Empty;
}

/// <summary>
/// Response from node registration.
/// </summary>
public sealed class RegisterNodeResponse
{
    public Guid NodeId { get; set; }
    public string ApiKey { get; set; } = string.Empty;
    public TimeSpan KeyRotationInterval { get; set; }
}

/// <summary>
/// Response with configuration checksum.
/// </summary>
public sealed class ConfigurationChecksumResponse
{
    public string Checksum { get; set; } = string.Empty;
}

/// <summary>
/// Response from key rotation.
/// </summary>
public sealed class RotateKeyResponse
{
    public string ApiKey { get; set; } = string.Empty;
    public TimeSpan KeyRotationInterval { get; set; }
}

/// <summary>
/// Request to submit a compliance report.
/// </summary>
public sealed class SubmitReportRequest
{
    public DscOperation Operation { get; set; }
    public DscResult Result { get; set; } = null!;
}
