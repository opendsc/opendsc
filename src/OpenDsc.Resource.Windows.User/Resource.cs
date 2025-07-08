// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Text.Json.Serialization;
using System.DirectoryServices.AccountManagement;
using OpenDsc.Resource;

namespace OpenDsc.Resource.Windows.User;

public sealed class Resource : AotDscResource<Schema>, IGettable<Schema>, ISettable<Schema>, IDeletable<Schema>, IExportable<Schema>
{
    public Resource(JsonSerializerContext context) : base("DSCUniversalResources.Windows/User", context)
    {
        Description = "Manage users in computer management.";
        Tags = ["Windows"];
        ExitCodes.Add(4, new() { Exception = typeof(MultipleMatchesException), Description = "The user could not be found" });
        ExitCodes.Add(5, new() { Exception = typeof(InvalidOperationException), Description = "An error occurred while processing the user" });
    }

    public Schema Get(Schema instance)
    {
        return Utils.GetUser(instance.Username);
    }

    public SetResult<Schema> Set(Schema instance)
    {
        try
        {
            bool userExists = Utils.UserExists(instance.Username);

            if (!userExists)
            {
                Logger.WriteTrace($"Creating user '{instance.Username}'");
                Utils.CreateUser(instance);
            }
            else
            {
                Logger.WriteTrace($"Updating user '{instance.Username}'");
                Utils.UpdateUser(instance);
            }

            // TODO: Mask the password
            return new SetResult<Schema>(instance);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to set user '{instance.Username}': {ex.Message}", ex);
        }
    }

    public void Delete(Schema instance)
    {
        try
        {
            if (Utils.UserExists(instance.Username))
            {
                Logger.WriteTrace($"Deleting user '{instance.Username}'");
                Utils.DeleteUser(instance.Username);
            }
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to delete user '{instance.Username}': {ex.Message}", ex);
        }
    }

    public IEnumerable<Schema> Export()
    {
        return Utils.GetAllUsers();
    }
}
