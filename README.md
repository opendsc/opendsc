# OpenDSC

OpenDsc is DSC's missing solution layer.
This project is to recreate the Local Configuration Manager (LCM), pull server, and reporting capabilities for [DSCv3](https://github.com/PowerShell/DSC).

Microsoft's intention with DSCv3 not to compete with other solutions
like Chef, Ansible and cloud based solutions like Azure Machine Configuration
but, rather be the platform layer.
This allows the other solutions to call DSC resources without competing directly.

While task-scheduler with PowerShell script can accomplish the needs for a basic and rudimentary LCM replacement, a full-featured LCM replacement can reduce the administrators technical debt maintaining and deploying configuration with reporting capabilities.

## LCM - Local Configuration Manager

The LCM is the agent on the device to start dsc configuration and report back on the success or failure.

The LCM can be configured in a push or pull scenario.
A push would be landing the configuration on the device.
A pull would have the LCM call the pull server to retrieve new configuration.

Partial configurations are not in scope.
The intent is to have a single configuration that is compiled elsewhere and letting the `dsc` executable perform the key validation.

### Push Model

In this diagram the author creates the configuration on their device.
Pushes the configuration to the remote device using whatever method they like SSH/RDP/PSRemoting.
Then lands the configuration in the directory the LCM is looking for the configuration.

```mermaid
sequenceDiagram
    Author->>LCM: Deploy configuration
    LCM->>DSC: Apply configuration
    DSC->>LCM: Retrieves status
    LCM-->>Reporting: Send status
```

## Pull Server

In DSCv1 pull server was used for the LCM to retrieve configuration and resources.

With DSCv3 having supporting multiple languages and different delivery mechanisms to deploy resources, is not in scope to carry over that feature at this time.

Implementation should be a REST API and not have a user interface.
LCM will have a token to authenticate to the pull server.

Open considerations:

* How to handle token rotation?

### Pull Model

In this diagram the author creates the configuration.
Publishes it to the pull server.
Then the LCM is configured to in pull mode.
On a reoccurring basis the LCM requests if there is a new configuration.
If there is a new configuration is pulled to the device to consume.

```mermaid
sequenceDiagram
    Author->>Pull: Publish configuration
    LCM->>Pull: Request configuration
    Pull->>LCM: Deploy configuration
    LCM->>DSC: Apply configuration
    DSC->>LCM: Retrieves status
    LCM-->>Reporting: Send status
```

## Reporting Server

The reporting server is used for the LCM to send status updates.
Implementation should be a REST API and not have a user interface.
The storage medium should allow for different database providers using the entity framework.
LCM will have a token to authenticate to the reporting server.

Open considerations:

* How to handle token rotation?

```mermaid
sequenceDiagram
    LCM->>DSC: Apply configuration
    DSC->>LCM: Retrieves status
    LCM->>Reporting: Sends status
    Reporting->>DB: Stores status
    User->>Reporting: Requests status
    Reporting->>DB: Requests stored status
    DB->>Reporting: Retrieves stored status
    Reporting->>User: Sends status
```

## Configuration Server

This does not have an equivalent in DSCv1, this would be an API and front-end website/application wih the capability to create, update, and deploy configuration to the pull server or pushing to the device.

The configuration server should have the ability to create configuration based on role, location, environment, etc.
Then the configuration would be merged at deployment time to the pull server or pushed to the device.

## Agent-less Deployment Server

There is an interesting capability that could be entertained.
Ansible uses an agent-less deployment strategy.
OpenDSC could have a model where a deployment server handles the LCM responsibilities remotely.
A con of this approach is scaling the deployment servers.

Implementation should be a REST API and not have a user interface.

## Securing configuration

### Push mode

The following diagram is how the LCM would securely store the configuration file at rest.
There is also the possibility of replacing GPG with CA certificates.

Open considerations:

* Allow unencrypted configuration?

```mermaid
flowchart TD
    A[LCM Starts] --> B{GPG key exists?}
    B -->|Yes| C{Configuration encrypted?}
    B -->|No| D[Create GPG encryption key]
    D --> C
    C -->|Yes| E[Decrypt configuration]
    C -->|No| F[Encrypt configuration file]
    F --> E
    E --> G[Call DSC to apply configuration]
```

### Pull mode

In a pull server situation the following diagram illustrates bootstrapping and delivery process.

Open considerations:

* How to handle key rotation?

```mermaid
sequenceDiagram
    Author->>LCM: Configures LCM in pull mode
    LCM->>LCM: Generates GPG key
    Pull->>Pull: Generates GPG key
    LCM->>Pull: Sends LCM public key
    Pull->>LCM: Sends pull public key
    Author->>Pull: Generates configuration
    Pull->>Pull: Encrypts configuration with LCM public key
    Pull->>Pull: Signs encrypted configuration with pull private key
    LCM->>Pull: Requests configuration
    Pull->>LCM: Sends configuration
    LCM->>LCM: Verify configuration signature is from pull public key
    LCM->>DSC: Apply configuration
    DSC->>LCM: Retrieves status
    LCM-->>Reporting: Send status
```
