# Configure OpenID Connect (OIDC)

The Pull Server supports external identity providers via OpenID Connect,
allowing users to sign in with their organization's existing credentials.
Each provider also issues JWT bearer tokens that API clients can use directly.

## When to use this guide

Use this guide when you want to:

- Allow users to sign in through Microsoft Entra ID, Okta, Auth0, or any
  standards-compliant OIDC provider.
- Avoid managing local passwords for team members who already have an
  organizational identity.
- Enable CI/CD pipelines or scripts to authenticate with short-lived JWT bearer
  tokens from a trusted provider.

## How it works

When OIDC is configured, the Pull Server registers a pair of authentication
schemes for each provider:

- **Cookie scheme** — used by the web UI; the browser is redirected to the
  provider, signs in, and a session cookie is issued.
- **JWT bearer scheme** — used by API clients; the client presents an access
  token in the `Authorization: Bearer` header and the server validates it
  against the provider's authority.

On first sign-in, the server automatically provisions a local user account and
links it to the provider identity. Subsequent sign-ins look up the existing
account.

!!! note
    OIDC users cannot set or change a local password. Their identity is managed
    entirely by the external provider.

## Prerequisites

- An application registered with your identity provider (client ID and secret).
- The provider's authority (issuer) URL.
- The Pull Server must be reachable at the redirect URI configured in your app
  registration.

## Register an application with your provider

### Microsoft Entra ID

1. Open the [Azure portal](https://portal.azure.com) and navigate to
   **Microsoft Entra ID → App registrations**.
2. Click **New registration**.
3. Set the redirect URI to:

   ```text
   https://<your-server>/signin-oidc-<ProviderName>
   ```

   For example: `https://dsc.example.com/signin-oidc-EntraId`
4. Note the **Application (client) ID** and **Directory (tenant) ID**.
5. Under **Certificates & secrets**, create a new client secret and note its
   value.

### Okta

1. In your Okta admin console, go to **Applications → Create App Integration**.
2. Choose **OIDC - OpenID Connect** and **Web Application**.
3. Set the sign-in redirect URI to:

   ```text
   https://<your-server>/signin-oidc-<ProviderName>
   ```

4. Note the **Client ID** and **Client Secret**.
5. Your authority URL is `https://<your-okta-domain>`.

## Configure the Pull Server

Add one or more providers under `Authentication:OidcProviders` in
`appsettings.json`:

<!-- markdownlint-disable MD046 -->

=== "Microsoft Entra ID"

    ```json
    {
        "Authentication": {
        "OidcProviders": [
            {
            "Name": "EntraId",
            "DisplayName": "Sign in with Microsoft",
            "Authority": "https://login.microsoftonline.com/<tenant-id>/v2.0",
            "ClientId": "<application-client-id>",
            "ClientSecret": "<client-secret>",
            "Scopes": ["openid", "profile", "email"]
            }
        ]
        }
    }
    ```

=== "Okta"

    ```json
    {
      "Authentication": {
        "OidcProviders": [
          {
            "Name": "Okta",
            "DisplayName": "Sign in with Okta",
            "Authority": "https://<your-okta-domain>",
            "ClientId": "<client-id>",
            "ClientSecret": "<client-secret>",
            "Scopes": ["openid", "profile", "email"]
          }
        ]
      }
    }
    ```

=== "Auth0"

    ```json
    {
      "Authentication": {
        "OidcProviders": [
          {
            "Name": "Auth0",
            "DisplayName": "Sign in with Auth0",
            "Authority": "https://<your-auth0-domain>",
            "ClientId": "<client-id>",
            "ClientSecret": "<client-secret>",
            "Scopes": ["openid", "profile", "email"]
          }
        ]
      }
    }
    ```

!!! warning
    Store `ClientSecret` in a secrets manager or environment variable rather
    than in `appsettings.json` directly. Use
    [ASP.NET Core Secret Manager][app-secrets]
    during development, or environment variable overrides in production:

    ```text
    Authentication__OidcProviders__0__ClientSecret=<secret>
    ```

<!-- markdownlint-enable MD046 -->

Restart the Pull Server after changing the configuration.

## Configuration reference

| Property | Required | Description |
| :--- | :--- | :--- |
| `Name` | Yes | Unique key for the provider. Used to build the redirect path `/signin-oidc-{Name}` and the challenge endpoint `/api/v1/auth/oidc/{Name}/challenge`. |
| `DisplayName` | Yes | Text shown on the login page button. |
| `Authority` | Yes | The OIDC issuer URL. The server appends `/.well-known/openid-configuration` to discover endpoints. |
| `ClientId` | Yes | The application (client) ID registered with the provider. |
| `ClientSecret` | Yes | The client secret for the authorization code flow. |
| `Scopes` | No | Additional scopes to request. Defaults to `["openid", "profile", "email"]`. |
| `GroupClaimType` | No | The token claim that contains group identifiers. Defaults to `groups`. |

## Multiple providers

You can configure more than one provider simultaneously. The login page will
show a button for each.

<!-- markdownlint-disable MD040 -->

```json title="appsettings.json"
{
  "Authentication": {
    "OidcProviders": [
      {
        "Name": "EntraId",
        "DisplayName": "Sign in with Microsoft",
        "Authority": "https://login.microsoftonline.com/<tenant-id>/v2.0",
        "ClientId": "<client-id-1>",
        "ClientSecret": "<secret-1>"
      },
      {
        "Name": "Okta",
        "DisplayName": "Sign in with Okta",
        "Authority": "https://<your-okta-domain>",
        "ClientId": "<client-id-2>",
        "ClientSecret": "<secret-2>"
      }
    ]
  }
}
```

<!-- markdownlint-enable MD040 -->

## Map provider groups to Pull Server roles

When a provider includes group identifiers in the token, you can map those
groups to Pull Server roles for automatic role assignment.

1. Set `GroupClaimType` to the claim name your provider uses (if it isn't
   `groups`):

    ```json
    {
      "GroupClaimType": "roles"
    }
    ```

2. In the web UI, navigate to **Admin → Roles** and create or edit a role.
3. Add the external group ID to the role's group mappings.

Users whose token contains the mapped group identifier will automatically be
granted that role on sign-in.

## Authenticate API clients with JWT bearer tokens

Once OIDC is configured, API clients can use access tokens issued by the
provider directly.

<!-- markdownlint-disable MD046 -->

=== "PowerShell"

    ```powershell
    # Acquire a token from your provider (example uses client credentials)
    $body = @{
        grant_type    = 'client_credentials'
        client_id     = '<client-id>'
        client_secret = '<client-secret>'
        scope         = 'openid'
    }
    $token = (Invoke-RestMethod -Uri 'https://login.microsoftonline.com/<tenant>/oauth2/v2.0/token' `
        -Method Post -Body $body).access_token

    # Use the token in API calls
    $headers = @{ Authorization = "Bearer $token" }
    Invoke-RestMethod -Uri 'https://dsc.example.com/api/v1/configurations' `
        -Headers $headers
    ```

=== "Shell"

    ```bash
    # Acquire a token (example uses curl with client credentials)
    TOKEN=$(curl -s -X POST \
      "https://login.microsoftonline.com/<tenant>/oauth2/v2.0/token" \
      -d "grant_type=client_credentials&client_id=<id>&client_secret=<secret>&scope=openid" \
      | jq -r '.access_token')

    # Use the token in API calls
    curl -H "Authorization: Bearer $TOKEN" \
      https://dsc.example.com/api/v1/configurations
    ```

<!-- markdownlint-enable MD046 -->

!!! note
    The server determines which JWT bearer scheme to use by reading the `iss`
    (issuer) claim from the token and matching it against the configured
    provider authorities. No extra configuration is needed.

## Verify the configuration

1. Open `https://<your-server>/login` in a browser.
2. A button labelled with your `DisplayName` should appear alongside the
   username/password form.
3. Click the button — you should be redirected to your provider's sign-in page.
4. After authenticating, you should be redirected back to the Pull Server and
   signed in.

!!! tip
    If the button does not appear, check the Pull Server logs for configuration
    errors at startup. Common issues are an unreachable authority URL or a
    mismatched redirect URI in the provider's app registration.

[app-secrets]: https://learn.microsoft.com/aspnet/core/security/app-secrets
