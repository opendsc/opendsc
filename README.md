# DSC Resources

This repository contains a C# library for generating Microsoft DSC v3 resources.
The library supports both .NET Standard 2.0 and .NET 8 and .NET 9. Ahead of Time
(AOT) compilation is supported.

The goals of the library is to facilitate C# devs to quickly create DSC
resources without having to manually create CLIs, JSON schema, and resource manifests.

| Library | Description |
| --- | --- |
| OpenDsc.Templates | DSC project templates |
| OpenDsc.Resource | Core DSC resource implementation |
| OpenDsc.Resource.CommandLine | CLI and resource manifest generator |
| OpenDsc.Resource.Windows.Service | Example native AOT resource |
