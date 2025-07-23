// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.DirectoryServices.AccountManagement;
using OpenDsc.Resource;

namespace OpenDsc.Resource.Windows.User;

internal static class Utils
{
    public static Schema GetUser(string Username)
    {
        using var context = new PrincipalContext(ContextType.Machine);
        using var user = UserPrincipal.FindByIdentity(context, Username);

        return user == null
            ? new Schema { Username = Username, Exist = false }
            : new Schema
            {
                Username = user.SamAccountName,
                FullName = user.DisplayName,
                Description = user.Description,
                Disabled = !user.Enabled,
                PasswordNeverExpires = user.PasswordNeverExpires,
                PasswordChangeNotAllowed = user.UserCannotChangePassword,
                PasswordChangeRequired = IsPasswordChangeRequired(user)
            };
    }

    public static bool UserExists(string Username)
    {
        return UserExistsInternal(Username);
    }

    private static bool UserExistsInternal(string Username)
    {
        using var context = new PrincipalContext(ContextType.Machine);
        using var user = UserPrincipal.FindByIdentity(context, Username);
        return user != null;
    }

    public static void CreateUser(Schema Schema)
    {
        using var context = new PrincipalContext(ContextType.Machine);
        using var user = new UserPrincipal(context);

        user.SamAccountName = Schema.Username;
        user.Name = Schema.Username;

        if (!string.IsNullOrEmpty(Schema.FullName))
            user.DisplayName = Schema.FullName;

        if (!string.IsNullOrEmpty(Schema.Description))
            user.Description = Schema.Description;

        if (!string.IsNullOrEmpty(Schema.Password))
            user.SetPassword(Schema.Password);

        if (Schema.Disabled.HasValue)
            user.Enabled = !Schema.Disabled.Value;

        if (Schema.PasswordNeverExpires.HasValue)
            user.PasswordNeverExpires = Schema.PasswordNeverExpires.Value;

        if (Schema.PasswordChangeNotAllowed.HasValue)
            user.UserCannotChangePassword = Schema.PasswordChangeNotAllowed.Value;

        user.Save();

        // Handle password change required after creation
        if (Schema.PasswordChangeRequired == true)
        {
            user.ExpirePasswordNow();
        }
    }

    public static void UpdateUser(Schema Schema)
    {
        using var context = new PrincipalContext(ContextType.Machine);
        using var user = UserPrincipal.FindByIdentity(context, Schema.Username);

        // Update properties only if they're specified
        if (!string.IsNullOrEmpty(Schema.FullName))
            user.DisplayName = Schema.FullName;

        if (!string.IsNullOrEmpty(Schema.Description))
            user.Description = Schema.Description;

        if (!string.IsNullOrEmpty(Schema.Password))
            user.SetPassword(Schema.Password);

        if (Schema.Disabled.HasValue)
            user.Enabled = !Schema.Disabled.Value;

        if (Schema.PasswordNeverExpires.HasValue)
            user.PasswordNeverExpires = Schema.PasswordNeverExpires.Value;

        if (Schema.PasswordChangeNotAllowed.HasValue)
            user.UserCannotChangePassword = Schema.PasswordChangeNotAllowed.Value;

        user.Save();

        // Handle password change required
        if (Schema.PasswordChangeRequired == true)
        {
            user.ExpirePasswordNow();
        }
    }

    public static void DeleteUser(string Username)
    {
        using var context = new PrincipalContext(ContextType.Machine);
        using var user = UserPrincipal.FindByIdentity(context, Username);

        if (user == null)
        {
            Logger.WriteError($"User '{Username}' not found");
            return;
        }

        user.Delete();
    }

    private static bool IsPasswordChangeRequired(UserPrincipal user)
    {
        try
        {
            // Check if password has expired or must be changed at next logon
            return user.LastPasswordSet == null || user.LastPasswordSet == DateTime.MinValue;
        }
        catch
        {
            return false;
        }
    }

    public static List<Schema> GetAllUsers()
    {
        var users = new List<Schema>();

        using var context = new PrincipalContext(ContextType.Machine);
        using var searcher = new PrincipalSearcher(new UserPrincipal(context));

        foreach (var result in searcher.FindAll())
        {
            if (result is UserPrincipal user)
            {
                users.Add(new Schema
                {
                    Username = user.SamAccountName,
                    FullName = user.DisplayName,
                    Description = user.Description,
                    Disabled = !user.Enabled,
                    PasswordNeverExpires = user.PasswordNeverExpires,
                    PasswordChangeNotAllowed = user.UserCannotChangePassword,
                    PasswordChangeRequired = IsPasswordChangeRequired(user)
                });

                user.Dispose();
            }
        }

        return users;
    }
}
