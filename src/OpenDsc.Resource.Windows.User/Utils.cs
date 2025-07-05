// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.DirectoryServices.AccountManagement;
using OpenDsc.Resource;

namespace OpenDsc.Resource.Windows.User;

internal static class Utils
{
    public static Schema GetUser(string userName)
    {
        try
        {
            using var context = new PrincipalContext(ContextType.Machine);
            using var user = UserPrincipal.FindByIdentity(context, userName);

            return user == null
                ? new Schema { userName = userName, exist = false }
                : new Schema
                {
                    userName = userName,
                    exist = true,
                    fullName = user.DisplayName,
                    description = user.Description,
                    disabled = !user.Enabled,
                    passwordNeverExpires = user.PasswordNeverExpires,
                    passwordChangeNotAllowed = user.UserCannotChangePassword,
                    passwordChangeRequired = IsPasswordChangeRequired(user)
                };
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to retrieve user '{userName}': {ex.Message}", ex);
        }
    }

    public static bool UserExists(string userName)
    {
        try
        {
            using var context = new PrincipalContext(ContextType.Machine);
            using var user = UserPrincipal.FindByIdentity(context, userName);
            return user != null;
        }
        catch
        {
            return false;
        }
    }

    public static void CreateUser(Schema Schema)
    {
        try
        {
            using var context = new PrincipalContext(ContextType.Machine);
            using var user = new UserPrincipal(context);

            user.SamAccountName = Schema.userName;
            user.Name = Schema.userName;

            if (!string.IsNullOrEmpty(Schema.fullName))
                user.DisplayName = Schema.fullName;

            if (!string.IsNullOrEmpty(Schema.description))
                user.Description = Schema.description;

            if (!string.IsNullOrEmpty(Schema.password))
                user.SetPassword(Schema.password);

            if (Schema.disabled.HasValue)
                user.Enabled = !Schema.disabled.Value;

            if (Schema.passwordNeverExpires.HasValue)
                user.PasswordNeverExpires = Schema.passwordNeverExpires.Value;

            if (Schema.passwordChangeNotAllowed.HasValue)
                user.UserCannotChangePassword = Schema.passwordChangeNotAllowed.Value;

            user.Save();

            // Handle password change required after creation
            if (Schema.passwordChangeRequired == true)
            {
                user.ExpirePasswordNow();
            }
        }
        catch (Exception ex)
        {
            Logger.WriteError($"Failed to create user '{Schema.userName}': {ex.Message}");
            Environment.Exit(5);
        }
    }

    public static void UpdateUser(Schema Schema)
    {
        try
        {
            using var context = new PrincipalContext(ContextType.Machine);
            using var user = UserPrincipal.FindByIdentity(context, Schema.userName);

            if (user == null)
                throw new InvalidOperationException($"User '{Schema.userName}' not found");

            // Update properties only if they're specified
            if (!string.IsNullOrEmpty(Schema.fullName))
                user.DisplayName = Schema.fullName;

            if (!string.IsNullOrEmpty(Schema.description))
                user.Description = Schema.description;

            if (!string.IsNullOrEmpty(Schema.password))
                user.SetPassword(Schema.password);

            if (Schema.disabled.HasValue)
                user.Enabled = !Schema.disabled.Value;

            if (Schema.passwordNeverExpires.HasValue)
                user.PasswordNeverExpires = Schema.passwordNeverExpires.Value;

            if (Schema.passwordChangeNotAllowed.HasValue)
                user.UserCannotChangePassword = Schema.passwordChangeNotAllowed.Value;

            user.Save();

            // Handle password change required
            if (Schema.passwordChangeRequired == true)
            {
                user.ExpirePasswordNow();
            }
        }
        catch (Exception ex)
        {
            Logger.WriteError($"Failed to update user '{Schema.userName}': {ex.Message}");
            Environment.Exit(5);
        }
    }

    public static void DeleteUser(string userName)
    {
        try
        {
            using var context = new PrincipalContext(ContextType.Machine);
            using var user = UserPrincipal.FindByIdentity(context, userName);

            if (user == null)
                throw new InvalidOperationException($"User '{userName}' not found");

            user.Delete();
        }
        catch (Exception ex)
        {
            Logger.WriteError($"Failed to delete user '{userName}': {ex.Message}");
            Environment.Exit(6);
        }
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

        try
        {
            using var context = new PrincipalContext(ContextType.Machine);
            using var searcher = new PrincipalSearcher(new UserPrincipal(context));

            foreach (var result in searcher.FindAll())
            {
                if (result is UserPrincipal user)
                {
                    users.Add(new Schema
                    {
                        userName = user.SamAccountName,
                        exist = true,
                        fullName = user.DisplayName,
                        description = user.Description,
                        disabled = !user.Enabled,
                        passwordNeverExpires = user.PasswordNeverExpires,
                        passwordChangeNotAllowed = user.UserCannotChangePassword,
                        passwordChangeRequired = IsPasswordChangeRequired(user)
                    });

                    user.Dispose();
                }
            }
        }
        catch (Exception ex)
        {
            Logger.WriteError($"Failed to retrieve all users: {ex.Message}");
            Environment.Exit(7);
        }

        return users;
    }
}