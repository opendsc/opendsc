# OpenDsc.Windows/AccountLockoutPolicy

Manages Windows account lockout policy settings at the system level.

## Description

This resource configures account lockout policy that applies to all user
accounts on the system. Account lockout policies help protect against
brute-force password attacks by locking accounts after a specified
number of failed logon attempts. This is a system-wide singleton
resource - there is only one account lockout policy per system.

**Note:** Changes require administrator privileges and take effect
immediately.

## Requirements

- Windows operating system
- Administrator privileges (for Set operations)
- Local system or domain controller

## Properties

- **`lockoutThreshold`** (integer) - Failed logon attempts before lockout
  (0-999). Use 0 to never lock out (not recommended).
- **`lockoutDurationMinutes`** (integer) - Minutes account remains locked
  (0-99999). Use 0 to require admin unlock.
- **`lockoutObservationWindowMinutes`** (integer) - Minutes after failed logon
  before counter resets (0-99999). Must be ≤ duration.

## Examples

### Get Current Lockout Policy

```yaml
# config.dsc.yaml
$schema: https://aka.ms/dsc/schemas/v3/config.yaml
resources:
  - name: Check lockout policy
    type: OpenDsc.Windows/AccountLockoutPolicy
    properties: {}
```

### Set Recommended Lockout Policy

```yaml
# Microsoft Security Baseline
resources:
  - name: Standard lockout policy
    type: OpenDsc.Windows/AccountLockoutPolicy
    properties:
      lockoutThreshold: 10
      lockoutDurationMinutes: 10
      lockoutObservationWindowMinutes: 10
```

### Set Strict Lockout Policy

```yaml
# High security environment
resources:
  - name: Strict lockout policy
    type: OpenDsc.Windows/AccountLockoutPolicy
    properties:
      lockoutThreshold: 5
      lockoutDurationMinutes: 30
      lockoutObservationWindowMinutes: 30
```

### Disable Account Lockout

```yaml
# No lockout (prevents DoS but less secure)
resources:
  - name: Disable lockout
    type: OpenDsc.Windows/AccountLockoutPolicy
    properties:
      lockoutThreshold: 0
```

### Require Administrator Unlock

```yaml
# Locked accounts stay locked until admin unlocks
resources:
  - name: Permanent lockout
    type: OpenDsc.Windows/AccountLockoutPolicy
    properties:
      lockoutThreshold: 5
      lockoutDurationMinutes: 0
      lockoutObservationWindowMinutes: 30
```

## Command Line Usage

### Get Current Settings

```powershell
dsc resource get -r OpenDsc.Windows/AccountLockoutPolicy --input '{}'
```

### Set Lockout Policy

```powershell
$policy = @{
    lockoutThreshold = 10
    lockoutDurationMinutes = 15
    lockoutObservationWindowMinutes = 15
} | ConvertTo-Json -Compress

dsc resource set -r OpenDsc.Windows/AccountLockoutPolicy --input $policy
```

### Verify Settings with net accounts

```powershell
# View current lockout policy
net accounts

# Output shows:
# - Lockout threshold
# - Lockout duration (min)
# - Lockout observation window (min)
```

### Unlock a Locked Account

```powershell
# Unlock a specific user account
net user USERNAME /active:yes

# Or using PowerShell
Unlock-ADAccount -Identity USERNAME
```

## Common Use Cases

### Balanced Security Policy

```yaml
# Good for most organizations
resources:
  - name: Balanced lockout
    type: OpenDsc.Windows/AccountLockoutPolicy
    properties:
      lockoutThreshold: 10
      lockoutDurationMinutes: 15
      lockoutObservationWindowMinutes: 15
```

### High Security Environment

```yaml
# Maximum protection
resources:
  - name: High security lockout
    type: OpenDsc.Windows/AccountLockoutPolicy
    properties:
      lockoutThreshold: 3
      lockoutDurationMinutes: 60
      lockoutObservationWindowMinutes: 60
```

### Prevent Denial of Service

```yaml
# Disable lockout to prevent DoS (use MFA instead)
resources:
  - name: No lockout policy
    type: OpenDsc.Windows/AccountLockoutPolicy
    properties:
      lockoutThreshold: 0
```

### Financial/Healthcare Compliance

```yaml
# PCI-DSS / HIPAA compliant
resources:
  - name: Compliance lockout
    type: OpenDsc.Windows/AccountLockoutPolicy
    properties:
      lockoutThreshold: 5
      lockoutDurationMinutes: 30
      lockoutObservationWindowMinutes: 30
```

## Important Notes

- **Singleton Resource:** Only one lockout policy exists per system
- **No _exist Property:** Cannot be deleted, only configured
- **Immediate Effect:** Changes apply immediately to the system
- **Domain vs Local:** On domain-joined machines, domain policy may
  override local settings
- **Help Desk Impact:** Lower thresholds = more unlock requests
- **DoS Risk:** Very low thresholds can enable denial-of-service attacks
  by intentionally locking out accounts
- **Administrator Accounts:** Built-in Administrator account is never
  locked out via policy (security feature)
- **Observation Window:** Must be less than or equal to lockout duration
  when threshold > 0

## Validation Rules

The resource enforces the following validation:

```text
If lockoutThreshold > 0 AND lockoutDurationMinutes > 0:
    lockoutObservationWindowMinutes ≤ lockoutDurationMinutes
```

**Note:** When `lockoutDurationMinutes` is 0 (administrator unlock
required), the observation window can be any value since accounts remain
locked until manually unlocked.

If you violate this rule, the Set operation will fail with an error.

## Group Policy Equivalent

Local Computer Policy path:

```text
Computer Configuration
└── Windows Settings
    └── Security Settings
        └── Account Policies
            └── Account Lockout Policy
```

Settings:

- Account lockout threshold
- Account lockout duration
- Reset account lockout counter after

## Troubleshooting

### Check Locked Out Users

```powershell
# View locked out accounts (requires AD)
Search-ADAccount -LockedOut

# Check specific user
Get-ADUser USERNAME -Properties LockedOut | Select-Object Name, LockedOut
```

### Monitor Failed Logon Attempts

```powershell
# Check Security event log for failed logons (Event ID 4625)
Get-WinEvent -FilterHashtable @{
    LogName='Security'
    ID=4625
    StartTime=(Get-Date).AddHours(-1)
} | Select-Object TimeCreated, Message -First 10
```

## Best Practices

1. **Enable MFA:** Use multi-factor authentication as primary defense
2. **Monitor Events:** Track failed logon attempts (Event ID 4625)
3. **User Training:** Educate users about password managers
4. **Self-Service:** Implement self-service password reset tools
5. **Exception Accounts:** Use separate non-locking accounts for
   services and automated tasks
6. **Regular Review:** Periodically review lockout events and adjust
   policy as needed
7. **Alert on Patterns:** Alert on suspicious lockout patterns
   (potential attacks)

## See Also

- [Account Lockout Policy Settings][00]
- [NetUserModalsGet][01]
- [NetUserModalsSet][02]
- [Configuring Account Lockout][03]

<!-- Link reference definitions -->
[00]: https://learn.microsoft.com/en-us/windows/security/threat-protection/security-policy-settings/account-lockout-policy
[01]: https://learn.microsoft.com/en-us/windows/win32/api/lmaccess/nf-lmaccess-netusermodalsget
[02]: https://learn.microsoft.com/en-us/windows/win32/api/lmaccess/nf-lmaccess-netusermodalsset
[03]: https://techcommunity.microsoft.com/t5/microsoft-security-baselines/configuring-account-lockout/ba-p/701040
