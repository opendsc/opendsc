# System Architecture & Data Flow

This document describes the architecture and data flows of the OpenDSC Pull
Server and Local Configuration Manager (LCM) integration, covering all major
subsystems: node registration, configuration delivery, parameter merging,
compliance reporting, and certificate lifecycle management.

## Table of Contents

- [System Overview](#system-overview)
- [Component Architecture](#component-architecture)
- [Node Registration & Authentication](#node-registration--authentication)
- [LCM Operational Loop](#lcm-operational-loop)
- [Configuration Bundle Delivery](#configuration-bundle-delivery)
- [Parameter Merging](#parameter-merging)
- [Composite Configurations](#composite-configurations)
- [Compliance Reporting](#compliance-reporting)
- [Certificate Lifecycle](#certificate-lifecycle)
- [Database Entity Model](#database-entity-model)

## System Overview

At the highest level, OpenDSC consists of three independent processes wired
together over HTTPS:

```mermaid
graph TB
    subgraph Admin["Admin Plane"]
        UI["Blazor Web UI\n(browser)"]
        APIKEY["API / Scripts\n(PAT auth)"]
    end

    subgraph Server["OpenDsc.Server (Pull Server)"]
        REST["REST API\n/api/v1/…"]
        BLAZOR["Blazor SSR\n/"]
        DB[("Database\nSQLite / PostgreSQL\n/ SQL Server")]
        FILES[("File Store\nconfigs/, parameters/")]
    end

    subgraph Node1["Managed Node A"]
        LCM1["OpenDsc.Lcm\n(background service)"]
        DSC1["dsc CLI\n(DSC engine)"]
        RESOURCES1["DSC Resources\n(OpenDsc.Resources.exe)"]
    end

    subgraph Node2["Managed Node B"]
        LCM2["OpenDsc.Lcm"]
        DSC2["dsc CLI"]
        RESOURCES2["DSC Resources"]
    end

    UI -->|"cookie / session"| BLAZOR
    APIKEY -->|"Bearer PAT"| REST
    BLAZOR <--> REST
    REST <--> DB
    REST <--> FILES

    LCM1 -->|"mTLS (client cert)"| REST
    DSC1 <-->|"subprocess + stdout/stderr"| LCM1
    DSC1 --> RESOURCES1

    LCM2 -->|"mTLS (client cert)"| REST
    DSC2 <-->|"subprocess"| LCM2
    DSC2 --> RESOURCES2
```

## Component Architecture

### Pull Server Internal Layers

```mermaid
graph TB
    subgraph API["API Layer (Minimal API Endpoints)"]
        NE["NodeEndpoints\n/api/v1/nodes"]
        CE["ConfigurationEndpoints\n/api/v1/configurations"]
        PE["ParameterEndpoints\n/api/v1/parameters"]
        CCE["CompositeConfigEndpoints\n/api/v1/composite-configurations"]
        RE["ReportEndpoints\n/api/v1/reports"]
        SE["SettingsEndpoints\n/api/v1/settings"]
        UE["UserEndpoints\n/api/v1/users"]
        GE["GroupEndpoints\n/api/v1/groups"]
        RKE["RegistrationKeyEndpoints\n/api/v1/registration-keys"]
    end

    subgraph AUTH["Authentication Middleware"]
        COOKIE["Cookie Auth\n(Blazor UI)"]
        PAT["PersonalAccessTokenHandler\n(API / scripts)"]
        MTLS["CertificateAuthHandler\n(LCM nodes)"]
    end

    subgraph SVC["Service Layer"]
        PMS["ParameterMergeService"]
        PM["ParameterMerger"]
        PSS["ParameterSchemaService"]
        PCS["ParameterCompatibilityService"]
        VRS["VersionRetentionService"]
        CS["ConfigurationService"]
        RAS["ResourceAuthorizationService"]
        GCT["GroupClaimsTransformation"]
    end

    subgraph DATA["Data Layer"]
        EF["EF Core DbContext"]
        DB[("Database")]
        FS[("File System")]
    end

    subgraph UI["Blazor SSR"]
        PAGES["Pages/"]
        LAYOUT["Layout/"]
        SHARED["Shared Components (MudBlazor)"]
    end

    AUTH --> API
    API --> SVC
    SVC --> DATA
    UI --> SVC
    UI --> API
```

### LCM Internal Structure

```mermaid
graph TB
    subgraph LCM["OpenDsc.Lcm (BackgroundService)"]
        W["LcmWorker\n(main loop)"]
        PSC["PullServerClient\n(HTTP + mTLS)"]
        EXEC["DscExecutor\n(subprocess wrapper)"]
        CM["CertificateManager\n(PFX rotation)"]
        CFG["LcmConfig\n(live reload via IOptionsMonitor)"]
    end

    W -->|"register / check-in\ncert rotation\ndownload bundle\nsubmit report"| PSC
    W -->|"config test\nconfig set"| EXEC
    W -->|"read / rotate cert"| CM
    CFG -->|"OnChange callback"| W
    EXEC -->|"spawn subprocess"| DSC["dsc CLI"]
```

## Node Registration & Authentication

### Initial Registration Sequence

```mermaid
sequenceDiagram
    participant LCM as LCM (Node)
    participant PSC as PullServerClient
    participant SERVER as Pull Server API
    participant DB as Database

    Note over LCM: No NodeId in config yet
    LCM->>PSC: RegisterAsync()
    PSC->>SERVER: GET /api/v1/settings/public (anonymous)
    SERVER-->>PSC: cert rotation interval, LCM defaults
    PSC->>SERVER: POST /api/v1/nodes/register\n{fqdn, registrationKey, mode, interval}
    Note over SERVER: Validate RegistrationKey\n(not revoked, not expired, under max uses)
    SERVER->>DB: Upsert Node (fqdn → new / update cert)
    DB-->>SERVER: NodeId (GUID)
    SERVER->>DB: Increment RegistrationKey.CurrentUses
    SERVER-->>PSC: 200 { nodeId }
    PSC->>LCM: Persist NodeId to appsettings
```

### mTLS Authentication (Subsequent Requests)

```mermaid
sequenceDiagram
    participant LCM as LCM (Node)
    participant TLS as TLS Handshake
    participant MTLS as CertificateAuthHandler
    participant DB as Database
    participant EP as Endpoint Handler

    LCM->>TLS: ClientHello + client certificate
    TLS-->>LCM: ServerHello (mutual TLS)
    Note over TLS: Kestrel accepts cert\n(AllowCertificate mode)
    LCM->>MTLS: Request with X.509 client cert
    MTLS->>DB: SELECT Node WHERE CertificateThumbprint = {sha256}
    DB-->>MTLS: Node { NodeId, Fqdn, ... }
    MTLS-->>LCM: ClaimsPrincipal { node_id, node_fqdn }
    LCM->>EP: Request proceeds with Node identity
```

## LCM Operational Loop

### Monitor Mode

```mermaid
flowchart TD
    START([Service start]) --> REGCHECK{NodeId\nconfigured?}
    REGCHECK -- No --> REGISTER[RegisterAsync\nget NodeId]
    REGISTER --> APPLYCONF[ApplyServerLcmConfigAsync\nserver-managed config overrides]
    REGCHECK -- Yes --> APPLYCONF
    APPLYCONF --> CERTCHECK{Cert\nexpiring soon?}
    CERTCHECK -- Yes --> ROTATE[RotateCertificateAsync\nnew PFX → server]
    CERTCHECK -- No --> CHECKSUMCHECK
    ROTATE --> CHECKSUMCHECK{Config\nchecksum\nchanged?}
    CHECKSUMCHECK -- No + local hash matches --> USECACHE[Use cached\nconfiguration path]
    CHECKSUMCHECK -- Yes --> DLSTATUS[UpdateLcmStatus\n→ Downloading]
    DLSTATUS --> DOWNLOAD[GetConfigurationBundleAsync\ndownload ZIP]
    DOWNLOAD --> EXTRACT[Extract ZIP\n(path-traversal safe)\nto pull cache dir]
    EXTRACT --> CHECKSUMSTORE[Persist checksum\nentryPoint\nparametersFile]
    CHECKSUMSTORE --> USECACHE
    USECACHE --> DSCTEST[DscExecutor.ExecuteTestAsync\ndsc config test --file entryPoint]
    DSCTEST --> REPORT[SubmitReportAsync\nPOST /api/v1/nodes/id/reports]
    REPORT --> STATUSUPDATE[UpdateLcmStatusAsync\n→ Compliant / NonCompliant]
    STATUSUPDATE --> WAIT[InterruptibleDelayAsync\n(ConfigurationModeInterval)]
    WAIT --> APPLYCONF
```

### Remediate Mode

```mermaid
flowchart TD
    GET[GetConfigurationPathAsync\n(same as Monitor up to download)] --> TEST[ExecuteTestAsync\ndsc config test]
    TEST --> DESIRED{All resources\nin desired state?}
    DESIRED -- Yes --> REPORT_PASS[SubmitReportAsync\noperation=Test, compliant=true]
    DESIRED -- No --> DSCSET[ExecuteSetAsync\ndsc config set]
    DSCSET --> REPORT_FAIL[SubmitReportAsync\noperation=Set, compliant=varies]
    REPORT_PASS --> STATUS[UpdateLcmStatusAsync]
    REPORT_FAIL --> STATUS
    STATUS --> WAIT[InterruptibleDelayAsync]
    WAIT --> GET
```

### Mode Switching (Live Reload)

```mermaid
sequenceDiagram
    participant FS as File System Watcher
    participant W as LcmWorker
    participant CTS as Mode CancellationTokenSource

    FS->>W: appsettings.json changed
    W->>W: OnConfigurationReloaded(newConfig)
    Note over W: Compare newMode vs _currentMode
    alt Mode changed
        W->>CTS: Cancel()
        Note over W: Current loop iteration exits\nwith OperationCanceledException
        W->>W: _currentMode = newMode
        W->>W: Create new CTS, restart loop
    else No mode change
        Note over W: Loop continues uninterrupted
    end
```

## Configuration Bundle Delivery

This diagram shows the full path from an admin uploading a configuration to a
node receiving and applying it.

```mermaid
sequenceDiagram
    participant ADMIN as Admin (UI / API)
    participant API as Pull Server API
    participant FS as File Store
    participant DB as Database
    participant LCM as LCM (Node)
    participant DSC as dsc CLI

    %% Upload phase
    ADMIN->>API: POST /api/v1/configurations\n(multipart: YAML files + entryPoint)
    API->>FS: Write files to configs/{name}/v{ver}/
    API->>API: Parse YAML → extract parameters block\n→ build JSON Schema
    API->>DB: ConfigurationVersion (Draft)\nConfigurationFile[]\nParameterSchema
    API-->>ADMIN: 201 Created

    ADMIN->>API: PUT /api/v1/configurations/{name}/versions/{ver}/publish
    API->>DB: Version.Status = Published → becomes Active
    API-->>ADMIN: 200 OK

    %% Assignment phase
    ADMIN->>API: PUT /api/v1/nodes/{nodeId}/configuration\n{configurationName}
    API->>DB: NodeConfiguration { nodeId, configId }
    API-->>ADMIN: 200 OK

    %% Pull phase
    LCM->>API: GET /api/v1/nodes/{id}/configuration/checksum (mTLS)
    API->>DB: Load active ConfigurationVersion checksum
    API-->>LCM: { checksum, entryPoint, parametersFile }

    alt Checksum changed
        LCM->>API: GET /api/v1/nodes/{id}/configuration/bundle (mTLS)
        API->>FS: Read all ConfigurationFiles
        API->>API: ParameterMergeService.MergeParametersAsync()\n→ single merged parameters.yaml
        API->>API: Build ZIP archive\n(files + parameters.yaml)
        API->>API: Compute SHA256 of ZIP
        API-->>LCM: 200 ZIP stream + X-Checksum header

        LCM->>LCM: Extract ZIP (path-traversal guard)\nto pull cache directory
        LCM->>LCM: Persist checksum + entryPoint + parametersFile
    end

    LCM->>DSC: dsc config test --file entryPoint\n--parameters-file parameters.yaml
    DSC-->>LCM: DscResult JSON (stdout) + trace JSONL (stderr)
```

## Parameter Merging

### Scope Hierarchy

```mermaid
graph TB
    subgraph SCOPES["Scope Hierarchy (low → high precedence)"]
        DEFAULT["Default Scope\n(global baseline)\nPrecedence: lowest"]
        REGION["Region Scope\ne.g. US-West, EU-Central"]
        ENV["Environment Scope\ne.g. Development, Production"]
        NODE["Node Scope\n(per-FQDN overrides)\nPrecedence: highest"]
    end

    subgraph NODE_TAGS["Node Tag Assignment"]
        N["Node\n(FQDN)"]
        NT1["NodeTag → ScopeValue 'US-West'\n(Region)"]
        NT2["NodeTag → ScopeValue 'Production'\n(Environment)"]
    end

    DEFAULT -->|"merged into"| MERGED["Merged\nparameters.yaml"]
    REGION -->|"overrides"| MERGED
    ENV -->|"overrides"| MERGED
    NODE -->|"overrides"| MERGED

    N --> NT1
    N --> NT2
    NT1 -->|"picks Region params"| REGION
    NT2 -->|"picks Environment params"| ENV
```

### Merge Flow (Server Side)

```mermaid
flowchart LR
    subgraph INPUTS["Parameter Files (per scope)"]
        D["Default/\nparameters.yaml\nv1.2.0"]
        R["Region/US-West/\nparameters.yaml\nv2.0.1"]
        E["Environment/Prod/\nparameters.yaml\nv1.5.3"]
        N["Node/server01.corp/\nparameters.yaml\nv1.0.0"]
    end

    subgraph MERGE["ParameterMergeService"]
        RES["VersionResolver\n(semver best-match)"]
        LOAD["Load active\nParameterFile per scope"]
        DEEP["IParameterMerger\ndeep-merge (narrow wins)"]
    end

    D --> RES
    R --> RES
    E --> RES
    N --> RES
    RES --> LOAD
    LOAD --> DEEP
    DEEP --> OUT["Merged\nparameters.yaml\n(bundled into ZIP)"]
```

## Composite Configurations

Composite configurations combine multiple versioned configurations into a single
deployment unit that is sent to a node as an ordered set of YAML files.

```mermaid
graph TB
    subgraph CC["CompositeConfiguration"]
        CCV["CompositeConfigurationVersion\n(semver, Active/Draft)"]
        CCI1["CompositeConfigurationItem\n(order=1) → Config A v2.1.0"]
        CCI2["CompositeConfigurationItem\n(order=2) → Config B v1.0.0"]
        CCI3["CompositeConfigurationItem\n(order=3) → Config C latest"]
    end

    subgraph BUNDLE["Bundle Assembly"]
        FA["Config A files"]
        FB["Config B files"]
        FC["Config C files"]
        PARAMS["Merged parameters.yaml\n(from all configs' schemas)"]
        ZIP["ZIP archive\n(delivered to node via /bundle)"]
    end

    CCV --> CCI1
    CCV --> CCI2
    CCV --> CCI3
    CCI1 --> FA
    CCI2 --> FB
    CCI3 --> FC
    FA --> ZIP
    FB --> ZIP
    FC --> ZIP
    PARAMS --> ZIP
```

## Compliance Reporting

```mermaid
sequenceDiagram
    participant DSC as dsc CLI
    participant LCM as LcmWorker
    participant PSC as PullServerClient
    participant API as Pull Server API
    participant DB as Database
    participant UI as Admin UI

    DSC-->>LCM: DscResult JSON\n(messages[], results[]\n{resourceType, name, inDesiredState})
    LCM->>LCM: Determine operation type\n(Test | Set) + overall compliance

    alt ReportCompliance = true
        LCM->>PSC: SubmitReportAsync(nodeId, operation, DscResult)
        PSC->>API: POST /api/v1/nodes/{id}/reports (mTLS)\n{operation, timestamp, resultJson}
        API->>API: Compute inDesiredState\n= all results are compliant
        API->>DB: Insert Report\nUpdate Node.Status + Node.LastCheckIn
        API-->>PSC: 200 OK
    end

    LCM->>PSC: UpdateLcmStatusAsync(Compliant|NonCompliant|Error)
    PSC->>API: PUT /api/v1/nodes/{id}/lcm-status (mTLS)
    API->>DB: Insert NodeStatusEvent\nUpdate Node.LcmStatus

    UI->>API: GET /api/v1/nodes/{id}/reports (PAT/Cookie)
    API->>DB: SELECT Reports WHERE NodeId = id
    API-->>UI: Report[] with resource-level detail
```

### Node Status Flow

```mermaid
stateDiagram-v2
    [*] --> Registered: POST /register
    Registered --> Downloading: config checksum changed
    Downloading --> Compliant: dsc test → all in desired state
    Downloading --> NonCompliant: dsc test → drift detected
    NonCompliant --> Compliant: dsc set succeeds (Remediate mode)
    NonCompliant --> Error: dsc set fails
    Compliant --> Downloading: next poll, checksum changed
    Compliant --> NonCompliant: next poll, drift detected
    Error --> Downloading: next poll cycle
    Registered --> Error: registration or download failure
```

## Certificate Lifecycle

```mermaid
sequenceDiagram
    participant LCM as LcmWorker
    participant CM as CertificateManager
    participant PSC as PullServerClient
    participant API as Pull Server API
    participant DB as Database

    Note over CM: CertificateSource = Managed
    LCM->>CM: LoadOrCreateCertificateAsync()
    alt PFX exists and not expiring
        CM-->>LCM: Existing X509Certificate2
    else PFX missing or expiring soon
        CM->>CM: Generate new RSA key pair\n+ self-signed X.509 cert
        CM->>CM: Save PFX to {ConfigDir}/certs/client.pfx
        CM-->>LCM: New X509Certificate2
        LCM->>PSC: RotateCertificateAsync(thumbprint, subject, notAfter)
        PSC->>API: POST /api/v1/nodes/{id}/rotate-certificate (old mTLS cert)
        API->>DB: Atomically update Node:\nCertificateThumbprint, Subject, NotAfter
        API-->>PSC: 200 OK
        Note over API: Old thumbprint invalidated immediately
    end
```

## Database Entity Model

```mermaid
erDiagram
    User ||--o{ PersonalAccessToken : "has"
    User }o--o{ Role : "assigned via RoleGroup"
    User }o--o{ Group : "member of"
    Group }o--o{ Role : "assigned via RoleGroup"

    RegistrationKey ||--o{ Node : "registers"

    Node ||--o{ Report : "submits"
    Node ||--o{ NodeStatusEvent : "logs"
    Node ||--o| NodeConfiguration : "has"
    Node ||--o{ NodeTag : "tagged with"
    NodeTag }o--|| ScopeValue : "references"
    ScopeValue }o--|| ScopeType : "instance of"

    NodeConfiguration }o--o| Configuration : "assigned"
    NodeConfiguration }o--o| CompositeConfiguration : "assigned"

    Configuration ||--o{ ConfigurationVersion : "versioned"
    ConfigurationVersion ||--o{ ConfigurationFile : "contains"
    ConfigurationVersion }o--|| ParameterSchema : "parameterized by"
    ParameterSchema ||--o{ ParameterFile : "scoped params"
    ParameterFile }o--|| ScopeType : "in scope"
    ParameterFile }o--o| ScopeValue : "scoped to value"

    CompositeConfiguration ||--o{ CompositeConfigurationVersion : "versioned"
    CompositeConfigurationVersion ||--o{ CompositeConfigurationItem : "ordered items"
    CompositeConfigurationItem }o--|| ConfigurationVersion : "pins version"

    ResourcePermission }o--o| User : "grants to"
    ResourcePermission }o--o| Group : "grants to"

    AuditLog }o--|| User : "recorded by"

    ServerSettings ||--|| ValidationSettings : "has"
    ServerSettings ||--o{ ConfigurationSettings : "per-config tuning"
```

## End-to-End: New Node Onboarding

The following sequence shows the complete lifecycle from an unmanaged machine to
a fully compliant managed node.

```mermaid
sequenceDiagram
    participant ADMIN as Admin
    participant SERVER as Pull Server
    participant LCM as LCM (New Node)
    participant DSC as dsc CLI

    %% Admin prepares the configuration
    ADMIN->>SERVER: POST /api/v1/configurations (upload YAML)
    ADMIN->>SERVER: PUT /configurations/{name}/versions/{ver}/publish
    ADMIN->>SERVER: POST /api/v1/registration-keys (create reg key)

    %% LCM installed on new node, first boot
    Note over LCM: appsettings.json:\nServerUrl + RegistrationKey
    LCM->>SERVER: GET /api/v1/settings/public (public cert rotation interval)
    LCM->>LCM: CertificateManager: generate client PFX
    LCM->>SERVER: POST /api/v1/nodes/register\n{fqdn, registrationKey, cert thumbprint}
    SERVER-->>LCM: { nodeId }
    LCM->>LCM: Persist NodeId to appsettings

    %% Admin assigns configuration
    ADMIN->>SERVER: PUT /api/v1/nodes/{nodeId}/configuration
    ADMIN->>SERVER: Tag node with scope values (region, env)

    %% First pull cycle
    LCM->>SERVER: GET /api/v1/nodes/{id}/configuration/checksum (mTLS)
    SERVER-->>LCM: checksum + entryPoint
    LCM->>SERVER: GET /api/v1/nodes/{id}/configuration/bundle (mTLS)
    SERVER->>SERVER: Merge parameters across scopes
    SERVER-->>LCM: ZIP (YAML files + merged parameters.yaml)
    LCM->>LCM: Extract ZIP to pull cache

    LCM->>DSC: dsc config test --file main.dsc.yaml
    DSC-->>LCM: DscResult (NonCompliant)

    alt Remediate Mode
        LCM->>DSC: dsc config set --file main.dsc.yaml
        DSC-->>LCM: DscResult (resources applied)
    end

    LCM->>SERVER: POST /api/v1/nodes/{id}/reports (mTLS)
    SERVER-->>ADMIN: Node visible as Compliant in UI
```
