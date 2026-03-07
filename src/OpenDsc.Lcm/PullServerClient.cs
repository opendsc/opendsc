// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Net.Http.Json;
using System.Security.Cryptography.X509Certificates;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using OpenDsc.Lcm.Contracts;
using OpenDsc.Schema;

namespace OpenDsc.Lcm;

/// <summary>
/// HTTP client for communicating with the DSC pull server using mTLS.
/// </summary>
public partial class PullServerClient : IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly IOptionsMonitor<LcmConfig> _lcmMonitor;
    private readonly ICertificateManager _certificateManager;
    private readonly ILogger<PullServerClient> _logger;

    public PullServerClient(
        HttpClient httpClient,
        IOptionsMonitor<LcmConfig> lcmMonitor,
        ICertificateManager certificateManager,
        ILogger<PullServerClient> logger)
    {
        _httpClient = httpClient;
        _lcmMonitor = lcmMonitor;
        _certificateManager = certificateManager;
        _logger = logger;
    }

    /// <summary>
    /// Registers the node with the pull server.
    /// </summary>
    /// <returns>Registration result with node ID.</returns>
    public async Task<RegisterNodeResponse?> RegisterAsync(CancellationToken cancellationToken = default)
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
            Fqdn = fqdn,
            ConfigurationSource = config.ConfigurationSource,
            ConfigurationMode = config.ConfigurationMode,
            ConfigurationModeInterval = config.ConfigurationModeInterval,
            ReportCompliance = pullServer.ReportCompliance
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

            pullServer.NodeId = result.NodeId;
            LogRegistrationSucceeded(result.NodeId);
            return result;
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

        if (pullServer is null || pullServer.NodeId is null)
        {
            return false;
        }

        try
        {
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

        if (pullServer is null || pullServer.NodeId is null)
        {
            LogPullServerNotConfigured();
            return null;
        }

        try
        {
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
    public async Task<ConfigurationChecksumResponse?> GetConfigurationChecksumAsync(CancellationToken cancellationToken = default)
    {
        var config = _lcmMonitor.CurrentValue;
        var pullServer = config.PullServer;

        if (pullServer is null || pullServer.NodeId is null)
        {
            return null;
        }

        try
        {
            using var response = await _httpClient.GetAsync(
                $"{pullServer.ServerUrl}/api/v1/nodes/{pullServer.NodeId}/configuration/checksum",
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            return await response.Content.ReadFromJsonAsync(
                PullServerJsonContext.Default.ConfigurationChecksumResponse,
                cancellationToken);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Downloads the configuration bundle as a ZIP file.
    /// </summary>
    /// <returns>Stream containing the ZIP bundle, or null if not available.</returns>
    public async Task<Stream?> GetConfigurationBundleAsync(CancellationToken cancellationToken = default)
    {
        var config = _lcmMonitor.CurrentValue;
        var pullServer = config.PullServer;

        if (pullServer is null || pullServer.NodeId is null)
        {
            return null;
        }

        try
        {
            using var response = await _httpClient.GetAsync(
                $"{pullServer.ServerUrl}/api/v1/nodes/{pullServer.NodeId}/configuration/bundle",
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                LogBundleDownloadFailed(response.StatusCode.ToString());
                return null;
            }

            var memoryStream = new MemoryStream();
            await response.Content.CopyToAsync(memoryStream, cancellationToken);
            memoryStream.Position = 0;

            LogBundleDownloadSucceeded(memoryStream.Length);
            return memoryStream;
        }
        catch (Exception ex)
        {
            LogBundleDownloadException(ex);
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

        if (pullServer is null || pullServer.NodeId is null)
        {
            return false;
        }

        if (!pullServer.ReportCompliance)
        {
            return true;
        }

        try
        {
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
                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                LogReportSubmissionFailed(response.StatusCode.ToString(), body);
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
    /// Rotates the certificate with the pull server.
    /// </summary>
    /// <returns>True if rotation succeeded, false otherwise.</returns>
    public async Task<bool> RotateCertificateAsync(X509Certificate2 newCertificate, CancellationToken cancellationToken = default)
    {
        var config = _lcmMonitor.CurrentValue;
        var pullServer = config.PullServer;

        if (pullServer is null || pullServer.NodeId is null)
        {
            return false;
        }

        try
        {
            var request = new RotateCertificateRequest
            {
                CertificateThumbprint = newCertificate.Thumbprint,
                CertificateSubject = newCertificate.Subject,
                CertificateNotAfter = newCertificate.NotAfter
            };

            using var response = await _httpClient.PostAsJsonAsync(
                $"{pullServer.ServerUrl}/api/v1/nodes/{pullServer.NodeId}/rotate-certificate",
                request,
                PullServerJsonContext.Default.RotateCertificateRequest,
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                LogCertificateRotationFailed(response.StatusCode.ToString());
                return false;
            }

            LogCertificateRotatedOnServer();
            return true;
        }
        catch (Exception ex)
        {
            LogCertificateRotationError(ex);
            return false;
        }
    }

    /// <summary>
    /// Updates the node's LCM operational status on the pull server.
    /// </summary>
    public async Task UpdateLcmStatusAsync(LcmStatus status, CancellationToken cancellationToken = default)
    {
        var config = _lcmMonitor.CurrentValue;
        var pullServer = config.PullServer;

        if (pullServer is null || pullServer.NodeId is null)
        {
            return;
        }

        try
        {
            var request = new UpdateLcmStatusRequest { LcmStatus = status };

            using var response = await _httpClient.PutAsJsonAsync(
                $"{pullServer.ServerUrl}/api/v1/nodes/{pullServer.NodeId}/lcm-status",
                request,
                PullServerJsonContext.Default.UpdateLcmStatusRequest,
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                LogLcmStatusUpdateFailed(response.StatusCode.ToString(), body);
            }
        }
        catch (Exception ex)
        {
            LogLcmStatusUpdateError(ex);
        }
    }

    /// <summary>
    /// Reports the node's current LCM configuration to the pull server.
    /// </summary>
    public async Task ReportLcmConfigAsync(CancellationToken cancellationToken = default)
    {
        var config = _lcmMonitor.CurrentValue;
        var pullServer = config.PullServer;

        if (pullServer is null || pullServer.NodeId is null)
        {
            return;
        }

        try
        {
            var request = new ReportNodeLcmConfigRequest
            {
                ConfigurationMode = config.ConfigurationMode,
                ConfigurationModeInterval = config.ConfigurationModeInterval,
                ReportCompliance = pullServer.ReportCompliance
            };

            using var response = await _httpClient.PutAsJsonAsync(
                $"{pullServer.ServerUrl}/api/v1/nodes/{pullServer.NodeId}/reported-config",
                request,
                PullServerJsonContext.Default.ReportNodeLcmConfigRequest,
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                LogReportLcmConfigFailed(response.StatusCode.ToString(), body);
            }
        }
        catch (Exception ex)
        {
            LogReportLcmConfigError(ex);
        }
    }

    public void Dispose()
    {
        _httpClient.Dispose();
    }

    /// <summary>
    /// Fetches the server-managed desired LCM configuration for this node.
    /// </summary>
    /// <returns>The desired LCM config, or null if unavailable.</returns>
    public async Task<NodeLcmConfigResponse?> GetLcmConfigAsync(CancellationToken cancellationToken = default)
    {
        var config = _lcmMonitor.CurrentValue;
        var pullServer = config.PullServer;

        if (pullServer is null || pullServer.NodeId is null)
        {
            return null;
        }

        try
        {
            using var response = await _httpClient.GetAsync(
                $"{pullServer.ServerUrl}/api/v1/nodes/{pullServer.NodeId}/lcm-config",
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            return await response.Content.ReadFromJsonAsync(
                PullServerJsonContext.Default.NodeLcmConfigResponse,
                cancellationToken);
        }
        catch (Exception ex)
        {
            LogLcmConfigFetchError(ex);
            return null;
        }
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

    [LoggerMessage(EventId = 1011, Level = LogLevel.Warning, Message = "Report submission failed: {StatusCode} - {Body}")]
    private partial void LogReportSubmissionFailed(string statusCode, string body);

    [LoggerMessage(EventId = 1012, Level = LogLevel.Debug, Message = "Compliance report submitted")]
    private partial void LogReportSubmitted();

    [LoggerMessage(EventId = 1013, Level = LogLevel.Warning, Message = "Report submission error")]
    private partial void LogReportSubmissionError(Exception ex);

    [LoggerMessage(EventId = 1014, Level = LogLevel.Warning, Message = "Certificate rotation failed: {StatusCode}")]
    private partial void LogCertificateRotationFailed(string statusCode);

    [LoggerMessage(EventId = 1015, Level = LogLevel.Information, Message = "Certificate rotated on server successfully")]
    private partial void LogCertificateRotatedOnServer();

    [LoggerMessage(EventId = 1016, Level = LogLevel.Warning, Message = "Certificate rotation error")]
    private partial void LogCertificateRotationError(Exception ex);

    [LoggerMessage(EventId = 1017, Level = LogLevel.Debug, Message = "Failed to resolve fully qualified domain name; using machine name '{MachineName}' instead.")]
    private partial void LogFqdnResolutionFailed(Exception ex, string machineName);

    [LoggerMessage(EventId = 1018, Level = LogLevel.Error, Message = "Bundle download failed: {StatusCode}")]
    private partial void LogBundleDownloadFailed(string statusCode);

    [LoggerMessage(EventId = 1019, Level = LogLevel.Information, Message = "Bundle downloaded successfully: {Bytes} bytes")]
    private partial void LogBundleDownloadSucceeded(long bytes);

    [LoggerMessage(EventId = 1020, Level = LogLevel.Error, Message = "Bundle download error")]
    private partial void LogBundleDownloadException(Exception ex);

    [LoggerMessage(EventId = EventIds.LcmStatusUpdateFailed, Level = LogLevel.Warning, Message = "LCM status update failed: {StatusCode} - {Body}")]
    private partial void LogLcmStatusUpdateFailed(string statusCode, string body);

    [LoggerMessage(EventId = EventIds.LcmStatusUpdateError, Level = LogLevel.Warning, Message = "LCM status update error")]
    private partial void LogLcmStatusUpdateError(Exception ex);

    [LoggerMessage(EventId = 1021, Level = LogLevel.Warning, Message = "LCM config fetch error")]
    private partial void LogLcmConfigFetchError(Exception ex);

    [LoggerMessage(EventId = 1022, Level = LogLevel.Warning, Message = "Failed to report LCM config to server: {StatusCode} - {Body}")]
    private partial void LogReportLcmConfigFailed(string statusCode, string body);

    [LoggerMessage(EventId = 1023, Level = LogLevel.Warning, Message = "Error reporting LCM config to server")]
    private partial void LogReportLcmConfigError(Exception ex);
}
