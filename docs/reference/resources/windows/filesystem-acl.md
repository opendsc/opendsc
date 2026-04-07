# OpenDsc.Windows.FileSystem/AccessControlList

## Synopsis

Manages Windows file and directory access control lists (ACLs), including owner,
group, and
access rules. This is a pure list-management resource that uses the `_purge`
pattern for access
rules instead of `_exist`.

## Type name

```plaintext
OpenDsc.Windows.FileSystem/AccessControlList
```

## Capabilities

| Capability | Supported |
| :--------- | :-------- |
| Get        | Yes       |
| Set        | Yes       |
| Delete     | No        |
| Export     | No        |

## Properties

| Property      | Type         | Required | Access     | Description                                                                                 |
| :------------ | :----------- | :------- | :--------- | :------------------------------------------------------------------------------------------ |
| `path`        | string       | Yes      | Read/Write | Full path to the file or directory.                                                         |
| `owner`       | string       | No       | Read/Write | Owner. Accepts username, `domain\user`, or SID.                                             |
| `group`       | string       | No       | Read/Write | Primary group. Accepts group name, `domain\group`, or SID.                                  |
| `accessRules` | AccessRule[] | No       | Read/Write | Access control entries to apply.                                                            |
| `_purge`      | bool         | No       | Write-Only | When `true`, removes access rules not in the list. When `false` (default), only adds rules. |

### AccessRule object

Each element in the `accessRules` array is an object with the following
properties:

| Property            | Type     | Required | Description                                                     |
| :------------------ | :------- | :------- | :-------------------------------------------------------------- |
| `identity`          | string   | Yes      | Identity (username, `domain\user`, or SID).                     |
| `rights`            | string[] | Yes      | File system rights to grant or deny. Values must be unique.     |
| `inheritanceFlags`  | string[] | No       | Inheritance flags: `ContainerInherit`, `ObjectInherit`, `None`. |
| `propagationFlags`  | string[] | No       | Propagation flags: `InheritOnly`, `NoPropagateInherit`, `None`. |
| `accessControlType` | string   | Yes      | `Allow` or `Deny`.                                              |

### File system rights

| Right                          | Description                             |
| :----------------------------- | :-------------------------------------- |
| `FullControl`                  | Full control over the file or directory |
| `Modify`                       | Read, write, execute, and delete        |
| `ReadAndExecute`               | Read and execute                        |
| `Read`                         | Read data, attributes, and permissions  |
| `Write`                        | Write data and attributes               |
| `ListDirectory`                | List directory contents                 |
| `CreateFiles`                  | Create files                            |
| `CreateDirectories`            | Create subdirectories                   |
| `Delete`                       | Delete the file or directory            |
| `DeleteSubdirectoriesAndFiles` | Delete subdirectories and files         |
| `ReadAttributes`               | Read file attributes                    |
| `WriteAttributes`              | Write file attributes                   |
| `ReadExtendedAttributes`       | Read extended attributes                |
| `WriteExtendedAttributes`      | Write extended attributes               |
| `Traverse`                     | Traverse directory                      |
| `ReadPermissions`              | Read access control entries             |
| `ChangePermissions`            | Change access control entries           |
| `TakeOwnership`                | Take ownership                          |
| `Synchronize`                  | Synchronize access                      |

## Examples

### Example 1 — Get the ACL for a file

```powershell
dsc resource get -r OpenDsc.Windows.FileSystem/AccessControlList --input '{"path":"C:\\Data\\config.xml"}'
```

### Example 2 — Add an access rule (additive)

```powershell
dsc resource set -r OpenDsc.Windows.FileSystem/AccessControlList --input '{
  "path": "C:\\Data",
  "accessRules": [
    {
      "identity": "BUILTIN\\Users",
      "rights": ["Read", "ReadAndExecute"],
      "inheritanceFlags": ["ContainerInherit", "ObjectInherit"],
      "propagationFlags": ["None"],
      "accessControlType": "Allow"
    }
  ]
}'
```

### Example 3 — Set exact ACL (purge mode)

```powershell
dsc resource set -r OpenDsc.Windows.FileSystem/AccessControlList --input '{
  "path": "C:\\Data",
  "owner": "BUILTIN\\Administrators",
  "accessRules": [
    {
      "identity": "BUILTIN\\Administrators",
      "rights": ["FullControl"],
      "inheritanceFlags": ["ContainerInherit", "ObjectInherit"],
      "propagationFlags": ["None"],
      "accessControlType": "Allow"
    },
    {
      "identity": "BUILTIN\\Users",
      "rights": ["Read", "ReadAndExecute"],
      "inheritanceFlags": ["ContainerInherit", "ObjectInherit"],
      "propagationFlags": ["None"],
      "accessControlType": "Allow"
    }
  ],
  "_purge": true
}'
```

### Example 4 — Configuration document

```yaml
$schema: https://aka.ms/dsc/schemas/v3/bundled/config/document.json
resources:
  - name: Secure data folder
    type: OpenDsc.Windows.FileSystem/AccessControlList
    properties:
      path: C:\Data
      owner: BUILTIN\Administrators
      accessRules:
        - identity: BUILTIN\Administrators
          rights:
            - FullControl
          inheritanceFlags:
            - ContainerInherit
            - ObjectInherit
          propagationFlags:
            - None
          accessControlType: Allow
        - identity: BUILTIN\Users
          rights:
            - Read
            - ReadAndExecute
          inheritanceFlags:
            - ContainerInherit
            - ObjectInherit
          propagationFlags:
            - None
          accessControlType: Allow
      _purge: true
```

## Exit codes

| Code | Description                 |
| :--- | :-------------------------- |
| 0    | Success                     |
| 1    | Error                       |
| 2    | Invalid JSON                |
| 3    | Access denied               |
| 4    | Invalid argument            |
| 5    | Unauthorized access         |
| 6    | File or directory not found |
| 7    | Directory not found         |
| 8    | Identity not found          |

## See also

- [OpenDsc resource reference](../overview.md)
