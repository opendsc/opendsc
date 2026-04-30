// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

namespace OpenDsc.Server.Authentication;

/// <summary>
/// Configuration for a single OpenID Connect provider.
/// </summary>
public sealed class OidcProviderOptions
{
    /// <summary>
    /// Unique scheme key for this provider, e.g. "EntraId".
    /// Used to construct scheme names "oidc_{Name}" and "jwt_{Name}".
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Display name shown on the login page button, e.g. "Sign in with Microsoft".
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// The OIDC authority (issuer) URL, e.g.
    /// "https://login.microsoftonline.com/{tenantId}/v2.0".
    /// </summary>
    public string Authority { get; set; } = string.Empty;

    /// <summary>
    /// The application (client) ID registered with the provider.
    /// </summary>
    public string ClientId { get; set; } = string.Empty;

    /// <summary>
    /// The client secret for confidential client flows.
    /// </summary>
    public string ClientSecret { get; set; } = string.Empty;

    /// <summary>
    /// Additional scopes to request beyond the defaults.
    /// Defaults to ["openid", "profile", "email"].
    /// </summary>
    public string[] Scopes { get; set; } = ["openid", "profile", "email"];

    /// <summary>
    /// The claim type in the provider's token that contains group identifiers.
    /// These are mapped to the standard "groups" claim for ExternalGroupMapping.
    /// Defaults to "groups".
    /// </summary>
    public string GroupClaimType { get; set; } = "groups";
}
