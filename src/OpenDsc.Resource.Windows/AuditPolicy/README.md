# OpenDsc.Windows/AuditPolicy

Manage Windows audit policy for system security event auditing.

## Description

The AuditPolicy resource manages Windows Advanced Audit Policy
Configuration subcategories. Audit policies control what security events
are logged to the Windows Security event log, enabling monitoring and
compliance requirements for file access, logon events, privilege use,
and more.

This resource configures individual audit policy subcategories using
their unique GUIDs. Each subcategory can be set to audit Success events,
Failure events, both, or none.

## Requirements

- Windows operating system
- Administrative privileges (the resource automatically enables
  `SeSecurityPrivilege`)
- Elevated PowerShell session

<!-- markdownlint-disable MD052 -->
> [!NOTE]
> **Why SeSecurityPrivilege is required**
>
> The Windows API functions `AuditQuerySystemPolicy` and `AuditSetSystemPolicy`
> require the `SeSecurityPrivilege` (also known as "Manage auditing and security
> log") to access and modify system audit policy settings.
> This privilege controls access to the Security event log and audit
> configuration.
>
> The resource automatically enables this privilege before making API calls, but
> the process must have the privilege available (typically by running as
> Administrator). Without it, operations will fail with "Access denied" errors.
<!-- markdownlint-enable MD052 -->

## Properties

| Property      | Type   | Required | Description                                                                                              |
|---------------|--------|----------|----------------------------------------------------------------------------------------------------------|
| `subcategory` | string | Yes      | Name of the audit subcategory (e.g., 'File System', 'Logon', 'Security State Change'). Case-insensitive. |
| `setting`     | enum   | No       | Audit setting (default: `None`). Flags enum: `None`, `Success`, `Failure`, or `SuccessAndFailure`.       |

## Examples

### Enable success auditing for file system access

```yaml
resources:
  - type: OpenDsc.Windows/AuditPolicy
    properties:
      subcategory: File System
      setting: Success
```

### Enable both success and failure auditing for logon events

```yaml
resources:
  - type: OpenDsc.Windows/AuditPolicy
    properties:
      subcategory: Logon
      setting: SuccessAndFailure
```

### Disable auditing for a subcategory

```yaml
resources:
  - type: OpenDsc.Windows/AuditPolicy
    properties:
      subcategory: File System
      setting: None
```

## Audit Policy Subcategories

The following subcategory names are recognized by the resource. Use the exact
names (case-insensitive) shown in the **Subcategory** column. GUIDs are provided
for reference only and are not required in resource configurations.

### Account Logon

| Subcategory                        | GUID                                   |
|------------------------------------|----------------------------------------|
| Credential Validation              | `0cce923f-69ae-11d9-bed3-505054503030` |
| Kerberos Authentication Service    | `0cce9242-69ae-11d9-bed3-505054503030` |
| Kerberos Service Ticket Operations | `0cce9240-69ae-11d9-bed3-505054503030` |
| Other Account Logon Events         | `0cce9241-69ae-11d9-bed3-505054503030` |

### Account Management

| Subcategory                     | GUID                                   |
|---------------------------------|----------------------------------------|
| User Account Management         | `0cce9235-69ae-11d9-bed3-505054503030` |
| Computer Account Management     | `0cce9236-69ae-11d9-bed3-505054503030` |
| Security Group Management       | `0cce9237-69ae-11d9-bed3-505054503030` |
| Distribution Group Management   | `0cce9238-69ae-11d9-bed3-505054503030` |
| Application Group Management    | `0cce9239-69ae-11d9-bed3-505054503030` |
| Other Account Management Events | `0cce923a-69ae-11d9-bed3-505054503030` |

### Detailed Tracking

| Subcategory                 | GUID                                   |
|-----------------------------|----------------------------------------|
| Process Creation            | `0cce922b-69ae-11d9-bed3-505054503030` |
| Process Termination         | `0cce922c-69ae-11d9-bed3-505054503030` |
| DPAPI Activity              | `0cce922d-69ae-11d9-bed3-505054503030` |
| RPC Events                  | `0cce922e-69ae-11d9-bed3-505054503030` |
| Plug and Play Events        | `0cce9248-69ae-11d9-bed3-505054503030` |
| Token Right Adjusted Events | `0cce924a-69ae-11d9-bed3-505054503030` |

### DS Access (Directory Service)

| Subcategory                            | GUID                                   |
|----------------------------------------|----------------------------------------|
| Directory Service Access               | `0cce923b-69ae-11d9-bed3-505054503030` |
| Directory Service Changes              | `0cce923c-69ae-11d9-bed3-505054503030` |
| Directory Service Replication          | `0cce923d-69ae-11d9-bed3-505054503030` |
| Detailed Directory Service Replication | `0cce923e-69ae-11d9-bed3-505054503030` |

### Logon/Logoff

| Subcategory               | GUID                                   |
|---------------------------|----------------------------------------|
| Logon                     | `0cce9215-69ae-11d9-bed3-505054503030` |
| Logoff                    | `0cce9216-69ae-11d9-bed3-505054503030` |
| Account Lockout           | `0cce9217-69ae-11d9-bed3-505054503030` |
| IPsec Main Mode           | `0cce9218-69ae-11d9-bed3-505054503030` |
| IPsec Quick Mode          | `0cce9219-69ae-11d9-bed3-505054503030` |
| IPsec Extended Mode       | `0cce921a-69ae-11d9-bed3-505054503030` |
| Special Logon             | `0cce921b-69ae-11d9-bed3-505054503030` |
| Other Logon/Logoff Events | `0cce921c-69ae-11d9-bed3-505054503030` |
| Network Policy Server     | `0cce9243-69ae-11d9-bed3-505054503030` |
| User / Device Claims      | `0cce9247-69ae-11d9-bed3-505054503030` |
| Group Membership          | `0cce9249-69ae-11d9-bed3-505054503030` |

### Object Access

| Subcategory                    | GUID                                   |
|--------------------------------|----------------------------------------|
| File System                    | `0cce921d-69ae-11d9-bed3-505054503030` |
| Registry                       | `0cce921e-69ae-11d9-bed3-505054503030` |
| Kernel Object                  | `0cce921f-69ae-11d9-bed3-505054503030` |
| SAM                            | `0cce9220-69ae-11d9-bed3-505054503030` |
| Certification Services         | `0cce9221-69ae-11d9-bed3-505054503030` |
| Application Generated          | `0cce9222-69ae-11d9-bed3-505054503030` |
| Handle Manipulation            | `0cce9223-69ae-11d9-bed3-505054503030` |
| File Share                     | `0cce9224-69ae-11d9-bed3-505054503030` |
| Filtering Platform Packet Drop | `0cce9225-69ae-11d9-bed3-505054503030` |
| Filtering Platform Connection  | `0cce9226-69ae-11d9-bed3-505054503030` |
| Other Object Access Events     | `0cce9227-69ae-11d9-bed3-505054503030` |
| Detailed File Share            | `0cce9244-69ae-11d9-bed3-505054503030` |
| Removable Storage              | `0cce9245-69ae-11d9-bed3-505054503030` |
| Central Policy Staging         | `0cce9246-69ae-11d9-bed3-505054503030` |

### Policy Change

| Subcategory                      | GUID                                   |
|----------------------------------|----------------------------------------|
| Audit Policy Change              | `0cce922f-69ae-11d9-bed3-505054503030` |
| Authentication Policy Change     | `0cce9230-69ae-11d9-bed3-505054503030` |
| Authorization Policy Change      | `0cce9231-69ae-11d9-bed3-505054503030` |
| MPSSVC Rule-Level Policy Change  | `0cce9232-69ae-11d9-bed3-505054503030` |
| Filtering Platform Policy Change | `0cce9233-69ae-11d9-bed3-505054503030` |
| Other Policy Change Events       | `0cce9234-69ae-11d9-bed3-505054503030` |

### Privilege Use

| Subcategory                 | GUID                                   |
|-----------------------------|----------------------------------------|
| Sensitive Privilege Use     | `0cce9228-69ae-11d9-bed3-505054503030` |
| Non Sensitive Privilege Use | `0cce9229-69ae-11d9-bed3-505054503030` |
| Other Privilege Use Events  | `0cce922a-69ae-11d9-bed3-505054503030` |

### System

| Subcategory               | GUID                                   |
|---------------------------|----------------------------------------|
| Security State Change     | `0cce9210-69ae-11d9-bed3-505054503030` |
| Security System Extension | `0cce9211-69ae-11d9-bed3-505054503030` |
| System Integrity          | `0cce9212-69ae-11d9-bed3-505054503030` |
| IPsec Driver              | `0cce9213-69ae-11d9-bed3-505054503030` |
| Other System Events       | `0cce9214-69ae-11d9-bed3-505054503030` |

## GUI Access

You can view and manually configure audit policies through the Windows GUI:

### Local Security Policy

1. Run `secpol.msc`
2. Navigate to: **Security Settings** → **Advanced Audit Policy Configuration**
   → **System Audit Policies**
3. Select a category (e.g., **Object Access**)
4. Double-click a subcategory (e.g., **Audit File System**)
5. Check **Success** and/or **Failure** as needed

### Group Policy Editor

1. Run `gpedit.msc`
2. Navigate to: **Computer Configuration** → **Windows Settings** →
   **Security Settings** → **Advanced Audit Policy Configuration** →
   **System Audit Policies**
3. Configure subcategories as needed

### Command Line Verification

```powershell
# View all audit policies
auditpol /get /category:*

# View specific subcategory
auditpol /get /subcategory:"File System"

# View by GUID
auditpol /get /subcategory:{0cce921d-69ae-11d9-bed3-505054503030}
```

## Common Use Cases

### Compliance Monitoring

```yaml
# Track file access for sensitive directories
- type: OpenDsc.Windows/AuditPolicy
  properties:
    subcategoryGuid: 0cce921d-69ae-11d9-bed3-505054503030  # File System
    setting: SuccessAndFailure

# Track user logon/logoff
- type: OpenDsc.Windows/AuditPolicy
  properties:
    subcategoryGuid: 0cce9215-69ae-11d9-bed3-505054503030  # Logon
    setting: SuccessAndFailure
```

### Security Monitoring

```yaml
# Track privilege escalation attempts
- type: OpenDsc.Windows/AuditPolicy
  properties:
    subcategoryGuid: 0cce9228-69ae-11d9-bed3-505054503030  # Sensitive Privilege Use
    setting: Failure

# Track process creation for threat detection
- type: OpenDsc.Windows/AuditPolicy
  properties:
    subcategoryGuid: 0cce922b-69ae-11d9-bed3-505054503030  # Process Creation
    setting: Success
```

### Active Directory Monitoring

```yaml
# Track directory service changes
- type: OpenDsc.Windows/AuditPolicy
  properties:
    subcategoryGuid: 0cce923c-69ae-11d9-bed3-505054503030  # Directory Service Changes
    setting: SuccessAndFailure

# Track security group modifications
- type: OpenDsc.Windows/AuditPolicy
  properties:
    subcategoryGuid: 0cce9237-69ae-11d9-bed3-505054503030  # Security Group Management
    setting: Success
```

## Notes

- Changes take effect immediately
- Audit events are logged to the Windows Security event log
- Some subcategories may generate high volumes of events
  (e.g., File System, Process Creation)
- Consider disk space and performance impact when enabling verbose auditing
- The resource automatically enables `SeSecurityPrivilege` when setting audit
  policies
- Setting uses flags enum: `None`, `Success`, `Failure`, or `SuccessAndFailure`

## See Also

- [Advanced Security Audit Policy Settings][00]
- [Audit Policy Recommendations][01]
- [auditpol command reference][02]

<!-- -->
[00]: https://learn.microsoft.com/en-us/windows/security/threat-protection/auditing/advanced-security-audit-policy-settings
[01]: https://learn.microsoft.com/en-us/windows-server/identity/ad-ds/plan/security-best-practices/audit-policy-recommendations
[02]: https://learn.microsoft.com/en-us/windows-server/administration/windows-commands/auditpol
