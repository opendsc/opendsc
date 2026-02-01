// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Net.Http.Json;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

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
    private X509Certificate2? _clientCertificate;

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
    /// Ensures the HttpClient has the client certificate configured.
    /// </summary>
    private void EnsureClientCertificate()
    {
        if (_clientCertificate is not null)
        {
            return;
        }

        _clientCertificate = _certificateManager.GetClientCertificate();
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

        EnsureClientCertificate();

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

        EnsureClientCertificate();

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

        EnsureClientCertificate();

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
    public async Task<string?> GetConfigurationChecksumAsync(CancellationToken cancellationToken = default)
    {
        var config = _lcmMonitor.CurrentValue;
        var pullServer = config.PullServer;

        if (pullServer is null || pullServer.NodeId is null)
        {
            return null;
        }

        EnsureClientCertificate();

        try
        {
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

        if (pullServer is null || pullServer.NodeId is null)
        {
            return false;
        }

        if (!pullServer.ReportCompliance)
        {
            return true;
        }

        EnsureClientCertificate();

        try
        {
            var request = new SubmitReportRequest
            {
                Operation = operation.ToString(),
                Success = !result.HadErrors,
                Result = JsonSerializer.Serialize(result, PullServerJsonContext.Default.DscResult),
                Timestamp = DateTimeOffset.UtcNow
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
            _clientCertificate?.Dispose();
            _clientCertificate = newCertificate;
            return true;
        }
        catch (Exception ex)
        {
            LogCertificateRotationError(ex);
            return false;
        }
    }

    public void Dispose()
    {
        _clientCertificate?.Dispose();
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

    [LoggerMessage(EventId = 1014, Level = LogLevel.Warning, Message = "Certificate rotation failed: {StatusCode}")]
    private partial void LogCertificateRotationFailed(string statusCode);

    [LoggerMessage(EventId = 1015, Level = LogLevel.Information, Message = "Certificate rotated on server successfully")]
    private partial void LogCertificateRotatedOnServer();

    [LoggerMessage(EventId = 1016, Level = LogLevel.Warning, Message = "Certificate rotation error")]
    private partial void LogCertificateRotationError(Exception ex);

    [LoggerMessage(EventId = 1017, Level = LogLevel.Debug, Message = "Failed to resolve fully qualified domain name; using machine name '{MachineName}' instead.")]
    private partial void LogFqdnResolutionFailed(Exception ex, string machineName);
}
