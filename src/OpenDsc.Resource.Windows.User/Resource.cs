// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Text.Json.Serialization;
using OpenDsc.Resource;

namespace OpenDsc.Resource.Windows.User;

public sealed class Resource : AotDscResource<Schema>, IGettable<Schema>, ISettable<Schema>, IDeletable<Schema>, IExportable<Schema>
{
    public Resource(JsonSerializerContext context) : base("DSCUniversalResources.Windows/User", context)
    {
        Description = "Manage users in computer management.";
        Tags = ["Windows"];
        ExitCodes.Add(4, new() { Exception = typeof(FileNotFoundException), Description = "Failed to get user information" });
        ExitCodes.Add(5, new() { Exception = typeof(InvalidOperationException), Description = "Failed to create or update user" });
        ExitCodes.Add(6, new() { Exception = typeof(InvalidOperationException), Description = "Failed to delete user" });
        ExitCodes.Add(7, new() { Exception = typeof(InvalidOperationException), Description = "Failed to export users" });
    }

    public Schema Get(Schema instance)
    {
        return Utils.GetUser(instance.userName);
    }

    public SetResult<Schema> Set(Schema instance)
    {
        try
        {
            bool userExists = Utils.UserExists(instance.userName);

            if (!userExists)
            {
                Logger.WriteTrace($"Creating user '{instance.userName}'");
                Utils.CreateUser(instance);
            }
            else
            {
                Logger.WriteTrace($"Updating user '{instance.userName}'");
                Utils.UpdateUser(instance);
            }

            // TODO: Mask the password
            return new SetResult<Schema>(instance);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to set user '{instance.userName}': {ex.Message}", ex);
        }
    }

    public void Delete(Schema instance)
    {
        try
        {
            if (Utils.UserExists(instance.userName))
            {
                Logger.WriteTrace($"Deleting user '{instance.userName}'");
                Utils.DeleteUser(instance.userName);
            }
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to delete user '{instance.userName}': {ex.Message}", ex);
        }
    }

    public IEnumerable<Schema> Export()
    {
        return Utils.GetAllUsers();
    }
}
