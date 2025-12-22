// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Security.AccessControl;

using Json.Schema.Generation;

namespace OpenDsc.Resource.Windows.FileSystem.Acl;

[Title("Access Rule")]
[Description("Represents a file system access control entry.")]
[AdditionalProperties(false)]
public sealed class AccessRule
{
    [Required]
    [Description("The identity to which the rule applies. Can be specified as: username, domain\\username, or SID (S-1-5-...).")]
    [Pattern(@"^(?:S-1-[0-59]-\d+(?:-\d+)*|[^\\]+\\[^\\]+|[^\\@]+)$")]
    public string Identity { get; set; } = string.Empty;

    [Required]
    [Description("The file system rights granted or denied.")]
    [UniqueItems(true)]
    public required FileSystemRights[] Rights { get; set; }

    [Description("The inheritance flags that determine how the rule is inherited by child objects.")]
    [UniqueItems(true)]
    [Nullable(false)]
    public InheritanceFlags[]? InheritanceFlags { get; set; }

    [Description("The propagation flags that determine how inheritance is propagated.")]
    [UniqueItems(true)]
    [Nullable(false)]
    public PropagationFlags[]? PropagationFlags { get; set; }

    [Required]
    [Description("Whether the rule allows or denies access.")]
    public AccessControlType AccessControlType { get; set; }
}
