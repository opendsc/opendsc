// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Security.Cryptography.X509Certificates;

namespace OpenDsc.Lcm;

/// <summary>
/// Interface for managing certificates for mTLS authentication with the pull server.
/// </summary>
public interface ICertificateManager
{
    /// <summary>
    /// Gets the client certificate based on configuration.
    /// </summary>
    X509Certificate2? GetClientCertificate();

    /// <summary>
    /// Checks if the current certificate needs rotation.
    /// </summary>
    bool ShouldRotateCertificate(X509Certificate2? cert, PullServerSettings pullServer);

    /// <summary>
    /// Rotates the managed certificate.
    /// </summary>
    X509Certificate2? RotateCertificate(PullServerSettings pullServer);
}
