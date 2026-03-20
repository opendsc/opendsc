# OpenDsc.Windows/PasswordPolicy

Manages Windows password policy settings at the system level.

## Description

This resource configures domain or local password policy settings that
apply to all users on the system. Password policies control password
complexity, age, history, and minimum length requirements. This is a
system-wide singleton resource - there is only one password policy per
system.

**Note:** Changes require administrator privileges and take effect
immediately.

## Requirements

- Windows operating system
- Administrator privileges (for Set operations)
- Local system or domain controller

## Properties

| Property                  | Type    | Description                |
|---------------------------|---------|----------------------------|
| `minimumPasswordLength`   | integer | Minimum password length    |
|                           |         | (0-14). Default is 0 (no   |
|                           |         | minimum).                  |
| `maximumPasswordAgeDays`  | integer | Maximum password age in    |
|                           |         | days (0-999). Use 0 for    |
|                           |         | unlimited (never expires). |
|                           |         | Default is 42 days.        |
| `minimumPasswordAgeDays`  | integer | Minimum password age in    |
|                           |         | days (0-998). Default is   |
|                           |         | 0 days (can change         |
|                           |         | immediately).              |
| `passwordHistoryLength`   | integer | Number of unique passwords |
|                           |         | before reuse allowed       |
|                           |         | (0-24). Default is 0 (no   |
|                           |         | history).                  |

## Examples

### Get Current Password Policy

```yaml
# config.dsc.yaml
$schema: https://aka.ms/dsc/schemas/v3/config.yaml
resources:
  - name: Check password policy
    type: OpenDsc.Windows/PasswordPolicy
    properties: {}
```

### Set Strong Password Policy

```yaml
# Strong password requirements
resources:
  - name: Enforce strong passwords
    type: OpenDsc.Windows/PasswordPolicy
    properties:
      minimumPasswordLength: 12
      maximumPasswordAgeDays: 90
      minimumPasswordAgeDays: 1
      passwordHistoryLength: 24
```

### Set Basic Password Policy

```yaml
# Basic password requirements
resources:
  - name: Basic password policy
    type: OpenDsc.Windows/PasswordPolicy
    properties:
      minimumPasswordLength: 8
      maximumPasswordAgeDays: 60
      passwordHistoryLength: 5
```

### Disable Password Expiration

```yaml
# Passwords never expire
resources:
  - name: No password expiration
    type: OpenDsc.Windows/PasswordPolicy
    properties:
      maximumPasswordAgeDays: 0
```

## Command Line Usage

### Get Current Settings

```powershell
dsc resource get -r OpenDsc.Windows/PasswordPolicy --input '{}'
```

### Set Password Policy

```powershell
$policy = @{
    minimumPasswordLength = 10
    maximumPasswordAgeDays = 90
    passwordHistoryLength = 12
} | ConvertTo-Json -Compress

dsc resource set -r OpenDsc.Windows/PasswordPolicy --input $policy
```

### Verify Settings with net accounts

```powershell
# View current password policy
net accounts

# Output shows:
# - Minimum password length
# - Maximum password age
# - Minimum password age
# - Length of password history
```

## Common Use Cases

### Corporate Environment

```yaml
# Balanced security for business
resources:
  - name: Corporate password policy
    type: OpenDsc.Windows/PasswordPolicy
    properties:
      minimumPasswordLength: 12
      maximumPasswordAgeDays: 90
      minimumPasswordAgeDays: 1
      passwordHistoryLength: 12
```

### High Security Environment

```yaml
# Maximum security settings
resources:
  - name: High security passwords
    type: OpenDsc.Windows/PasswordPolicy
    properties:
      minimumPasswordLength: 14
      maximumPasswordAgeDays: 60
      minimumPasswordAgeDays: 2
      passwordHistoryLength: 24
```

### Development/Test Environment

```yaml
# Relaxed for development (not recommended for production)
resources:
  - name: Dev environment passwords
    type: OpenDsc.Windows/PasswordPolicy
    properties:
      minimumPasswordLength: 8
      maximumPasswordAgeDays: 0
      passwordHistoryLength: 0
```

## Important Notes

- **Singleton Resource:** Only one password policy exists per system
- **No _exist Property:** Cannot be deleted, only configured
- **Immediate Effect:** Changes apply immediately to the system
- **Domain vs Local:** On domain-joined machines, domain policy may
  override local settings
- **Fine-Grained Passwords:** Domain controllers support multiple
  password policies via Fine-Grained Password Policies (not managed by
  this resource)
- **Complexity Requirements:** This resource does not manage password
  complexity rules (uppercase, lowercase, digits, symbols) - those are
  managed separately via Group Policy or Local Security Policy

## Group Policy Equivalent

Local Computer Policy path:

```text
Computer Configuration
└── Windows Settings
    └── Security Settings
        └── Account Policies
            └── Password Policy
```

Settings:

- Minimum password length
- Maximum password age
- Minimum password age
- Enforce password history

## See Also

- [Password Policy Settings][00]
- [NetUserModalsGet][01]
- [NetUserModalsSet][02]
- [NIST SP 800-63B][03]

<!-- Link reference definitions -->
[00]: https://learn.microsoft.com/en-us/windows/security/threat-protection/security-policy-settings/password-policy
[01]: https://learn.microsoft.com/en-us/windows/win32/api/lmaccess/nf-lmaccess-netusermodalsget
[02]: https://learn.microsoft.com/en-us/windows/win32/api/lmaccess/nf-lmaccess-netusermodalsset
[03]: https://pages.nist.gov/800-63-3/sp800-63b.html
