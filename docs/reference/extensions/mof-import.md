# Import Extension

The MOF import extension converts legacy DSC v1 MOF configuration files into a
format the DSC CLI can execute.

## Capabilities

- Import

## Usage

Use the DSC CLI command syntax the same way as any other YAML/JSON configuration
document.

```powershell
dsc config test --file C:\<path>\localhost.mof
```
