// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Text.Json;
using System.Text.Json.Serialization;

using Json.Schema;

namespace OpenDsc.Resource.Windows.FileSystem.Share;

[DscResource("OpenDsc.Windows.FileSystem/Share", "0.1.0", Description = "Manage Windows SMB shares", Tags = ["windows", "share", "smb"])]
[ExitCode(0, Description = "Success")]
[ExitCode(1, Exception = typeof(Exception), Description = "Error")]
[ExitCode(2, Exception = typeof(JsonException), Description = "Invalid JSON")]
[ExitCode(3, Exception = typeof(InvalidOperationException), Description = "Share operation failed")]
[ExitCode(4, Exception = typeof(ArgumentException), Description = "Invalid argument or missing required parameter")]
[ExitCode(5, Exception = typeof(UnauthorizedAccessException), Description = "Access denied")]
public sealed class Resource(JsonSerializerContext context) : DscResource<Schema>(context), IGettable<Schema>, ISettable<Schema>, IDeletable<Schema>, IExportable<Schema>
{
    public override string GetSchema()
    {
        var registry = new SchemaRegistry();
        var schema = registry.CreateBundle(GeneratedJsonSchemas.FileSystem_Share_Schema.BaseUri, Schema.BundleUri);
        return JsonSerializer.Serialize(schema, SourceGenerationContext.Default.JsonSchema);
    }

    public Schema Get(Schema? instance)
    {
        ArgumentNullException.ThrowIfNull(instance);

        var share = ShareHelper.GetShare(instance.Name);

        if (share is null)
        {
            return new Schema
            {
                Name = instance.Name,
                Path = instance.Path,
                Exist = false
            };
        }

        var permissions = ShareHelper.GetSharePermissions(instance.Name);

        return new Schema
        {
            Name = share.Name,
            Path = share.Path,
            Description = share.Description,
            Permissions = permissions.ToArray()
        };
    }

    public SetResult<Schema>? Set(Schema? instance)
    {
        ArgumentNullException.ThrowIfNull(instance);

        if (instance.Exist != false && string.IsNullOrEmpty(instance.Path))
        {
            throw new ArgumentException("Share path cannot be null or empty for create/update operations.", nameof(instance));
        }

        var current = Get(instance);

        if (instance.Exist == false)
        {
            HandleDelete(current, instance);
            return null;
        }

        HandleCreateOrUpdate(current, instance);
        HandlePermissions(instance);

        return null;
    }

    public void Delete(Schema? instance)
    {
        ArgumentNullException.ThrowIfNull(instance);

        ShareHelper.DeleteShare(instance.Name);
    }

    public IEnumerable<Schema> Export(Schema? filter)
    {
        foreach (var share in ShareHelper.EnumerateShares())
        {
            var permissions = ShareHelper.GetSharePermissions(share.Name);

            yield return new Schema
            {
                Name = share.Name,
                Path = share.Path,
                Description = share.Description,
                Permissions = permissions.ToArray()
            };
        }
    }

    private static void HandleDelete(Schema current, Schema desired)
    {
        if (current.Exist != false)
        {
            ShareHelper.DeleteShare(desired.Name);
        }
    }

    private static void HandleCreateOrUpdate(Schema current, Schema desired)
    {
        if (current.Exist == false)
        {
            if (string.IsNullOrEmpty(desired.Path))
            {
                throw new ArgumentException("Share path cannot be null or empty for create operations.", nameof(desired));
            }

            ShareHelper.CreateShare(desired.Name, desired.Path, desired.Description);
        }
        else
        {
            HandleDescriptionUpdate(current, desired);
        }
    }

    private static void HandleDescriptionUpdate(Schema current, Schema desired)
    {
        if (desired.Description is not null &&
            !string.Equals(current.Description, desired.Description, StringComparison.Ordinal))
        {
            ShareHelper.UpdateShareDescription(desired.Name, desired.Description);
        }
    }

    private static void HandlePermissions(Schema desired)
    {
        if (desired.Permissions is not null && desired.Permissions.Length > 0)
        {
            var purge = desired.Purge ?? false;
            ShareHelper.SetSharePermissions(desired.Name, desired.Permissions, purge);
        }
    }

}
