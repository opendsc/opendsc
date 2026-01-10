# OpenDsc.Schema

Data contracts for Microsoft DSC v3 output schemas.

## Overview

This library provides strongly-typed .NET models for deserializing output from
DSC (Desired State Configuration) v3 operations. It supports all DSC
configuration and resource operation outputs with full Native AOT
compatibility.

Supports .NET Standard 2.0, .NET 8, .NET 9, and .NET 10.

## Features

- **Spec-compliant** - Implements Microsoft DSC v3.x output schemas
- **AOT-compatible** - Source-generated JSON serialization for Native AOT
  deployments
- **Strongly-typed metadata** - Type-safe access to DSC operation metadata
- **Operation-specific results** - Dedicated types for Get, Test, and Set
  operations
- **Cross-platform** - Works on any platform supporting .NET Standard 2.0+

## Usage

### Deserializing DSC Configuration Results

```csharp
using System.Text.Json;
using OpenDsc.Schema;

// Parse DSC test output
var json = /* output from: dsc config test --output-format json */;
var result = JsonSerializer.Deserialize(json, SourceGenerationContext.Default.DscResult);

// Check if all resources are in desired state
var allInDesiredState = result.Results?.All(r =>
{
    var testResult = r.Result?.Deserialize<DscTestOperationResult>(SourceGenerationContext.Default.DscTestOperationResult);
    return testResult?.InDesiredState == true;
}) ?? true;

// Access metadata
var operation = result.Metadata?.MicrosoftDsc?.Operation;
var duration = result.Metadata?.MicrosoftDsc?.Duration;
```

### Working with Resource-Specific Schemas

Since resource states vary by type, use `JsonElement` deserialization to
strongly-type them:

```csharp
// Define your resource schema
public class RegistrySchema
{
    public string? KeyPath { get; set; }
    public string? ValueName { get; set; }
    public object? ValueData { get; set; }
}

// Deserialize resource actual state
var resourceResult = result.Results?[0];
var testResult = resourceResult?.Result?.Deserialize<DscTestOperationResult>(SourceGenerationContext.Default.DscTestOperationResult);
var actualState = testResult?.ActualState?.Deserialize<RegistrySchema>();
```

### Processing Messages

```csharp
if (result.Messages != null)
{
    foreach (var message in result.Messages)
    {
        Console.WriteLine($"[{message.Level}] {message.Type}/{message.Name}: {message.Message}");
    }
}
```

## LCM Integration

The Local Configuration Manager (LCM) service uses this schema library to
deserialize DSC operation results for continuous monitoring and remediation.
The LCM parses `dsc config test` and `dsc config set` outputs to determine
resource drift and apply corrections.

### Example: LCM Drift Detection

```csharp
using System.Text.Json;
using OpenDsc.Schema;

// Deserialize test result from DSC CLI
var json = /* output from: dsc config test --output-format json */;
var result = JsonSerializer.Deserialize(json, SourceGenerationContext.Default.DscResult);

// Check for resources not in desired state
var needsCorrection = result.Results?.Any(r =>
{
    var testOp = r.Result?.Deserialize<DscTestOperationResult>(SourceGenerationContext.Default.DscTestOperationResult);
    return testOp?.InDesiredState == false;
}) ?? false;

if (needsCorrection)
{
    // Apply corrections using dsc config set
    var setJson = /* output from: dsc config set --output-format json */;
    var setResult = JsonSerializer.Deserialize(setJson, SourceGenerationContext.Default.DscResult);

    // Check for restart requirements
    var restartRequired = setResult.Metadata?.MicrosoftDsc?.RestartRequired;
    if (restartRequired?.Count > 0)
    {
        // Handle system/service/process restarts
    }
}
```

## Types

### Configuration Results

- `DscResult` - Result from config get/test/set operations
- `DscMetadata` - Metadata including operation context
- `MicrosoftDscMetadata` - DSC-specific metadata (version, duration, etc.)

### Resource Results

- `DscResourceResult` - Wrapper for individual resource results
- `DscGetOperationResult` - Result from resource get operation
- `DscTestOperationResult` - Result from resource test operation
- `DscSetOperationResult` - Result from resource set operation

### Messages & Metadata

- `DscMessage` - Structured message from resources
- `DscTraceMessage` - Trace message from DSC stderr output
- `DscRestartRequirement` - Restart requirement information
- `DscExitCode` - Exit codes returned by the DSC CLI

### Enums

- `DscOperation` - Operation types (Get, Set, Test, Export)
- `DscExecutionKind` - Execution types (Actual, WhatIf)
- `DscSecurityContext` - Security contexts (Current, Elevated, Restricted)
- `DscMessageLevel` - Message levels (Error, Warning, Information)
- `DscTraceLevel` - Trace levels (Error, Warn, Info, Debug, Trace)
