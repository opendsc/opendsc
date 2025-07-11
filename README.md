# DSC Resources

This repository contains a C# library for generating Microsoft DSC v3 resources.
The library supports both .NET Standard 2.0 and .NET 8 and .NET 9. Ahead of Time
(AOT) compilation is supported.

The goals of the library is to facilitate C# devs to quickly create DSC
resources without having to manually create CLIs, JSON schema, and resource
manifests.

| Library                        | Description                         |
| ------------------------------ | ----------------------------------- |
| [OpenDsc.Templates]            | DSC project templates               |
| [OpenDsc.Resource]             | Core DSC resource implementation    |
| [OpenDsc.Resource.CommandLine] | CLI and resource manifest generator |

[OpenDsc.Templates]: https://www.nuget.org/packages/OpenDsc.Templates
[OpenDsc.Resource]: https://www.nuget.org/packages/OpenDsc.Templates
[OpenDsc.Resource.CommandLine]: https://www.nuget.org/packages/OpenDsc.Templates

## Getting Started

Navigate to the [OpenDsc.Templates] NuGet page and follow the instructions on
installing the templates and creating a DSC .NET project.

## DSC Resources

This repository includes example Microsoft DSC v3 resources built using the
OpenDSC library. These resources are co-located in the `src` directory and
serve as both working implementations and reference examples for building
your own DSC resources.

The following table represent the available DSC resources:

| Resource                         | Description                         |
| -------------------------------- | ----------------------------------- |
| OpenDsc.Resource.Windows.Service | Manage Windows services             |
| OpenDsc.Resource.Windows.User    | Manage users in computer management |

To build, publish, and test these resources, follow the steps below.

### Prerequisites

- .NET SDK 9.0 or later (preferable)
- PowerShell 7.0 or later
- Microsoft DSC v3.1 or later

### Building a resource

You can build a DSC resource using the following snippet. It uses the
`OpenDsc.Resource.Windows.User` resource as example:

```powershell
$projectName = 'OpenDsc.Resource.Windows.User'
# Default debug
.\build.ps1 -ProjectName $projectName

# Use 'Release' as configuration
.\build.ps1 -ProjectName $projectName -Configuration Release
```

### Publishing a resource

To publish the resource as executable, you can use the `-Publish` switch
parameter. The following code snippet illustrates how the executable is
published and stored in the `output` directory in the root of the project:

```powershell
.\build.ps1 -ProjectName $projectName -Publish -Configuration Release
```

### Running resource tests

The OpenDsc repository executes tests dynamically based on testData. If a
resource contains a directory `data` with a `testData.psd1` file, it
leverages this data to determine what operations will be executed against
`dsc.exe`.

For example, the `OpenDsc.Resource.Windows.User` DSC resource contains the
following truncated testcases:

```powershell
@{
    # This is a PowerShell data file that contains test data for the OpenDsc.Resource.Windows.User utility
    testCases = @(
        @{
            operation = 'get'
            testData = @(
                @{
                    username = 'Administrator'    
                }
            )
            requiresElevation = $false
        },
        # truncated
    )  
}
```

When you execute the build automation script using the `-Test` switch parameter,
the tests are loaded in the test context, and execute the `get` operation with
the `testData` send as JSON to `dsc.exe`.

The following code snippet illustrates
how you can test the `OpenDsc.Resource.Windows.User` DSC resource **after** you
have published the executable:

```powershell
.\build.ps1 -ProjectName $projectName -Publish -Configuration Release -Test
```
