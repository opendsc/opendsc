// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Text.Json.Serialization;
using System.DirectoryServices.AccountManagement;
using System.Text.Json;

namespace OpenDsc.Resource.Windows.User;

[DscResource("OpenDsc.Windows/User", Description = "Manage users.", Tags = ["windows", "user"])]
[ExitCode(0, Description = "Success")]
[ExitCode(1, Description = "Invalid parameter")]
[ExitCode(2, Exception = typeof(Exception), Description = "Generic error")]
[ExitCode(3, Exception = typeof(JsonException), Description = "Invalid JSON")]
[ExitCode(4, Exception = typeof(InvalidOperationException), Description = "An error occurred while processing the user")]
[ExitCode(5, Exception = typeof(UnauthorizedAccessException), Description = "Access denied")]

public sealed class Resource : DscResource<Schema>, IGettable<Schema>, ISettable<Schema>, IDeletable<Schema>, IExportable<Schema>
{
    public Schema Get(Schema instance)
    {
        return Utils.GetUser(instance.Username);
    }

    public SetResult<Schema> Set(Schema instance)
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

    public void Delete(Schema instance)
    {
        if (Utils.UserExists(instance.Username))
        {
            Logger.WriteTrace($"Deleting user '{instance.Username}'");
            Utils.DeleteUser(instance.Username);
        }
    }

    public IEnumerable<Schema> Export()
    {
        return Utils.GetAllUsers();
    }
}
