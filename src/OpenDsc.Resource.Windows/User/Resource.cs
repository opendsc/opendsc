// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.DirectoryServices.AccountManagement;
using System.Security;
using System.Text.Json;
using System.Text.Json.Serialization;

using Json.Schema;
using Json.Schema.Generation;

namespace OpenDsc.Resource.Windows.User;

[DscResource("OpenDsc.Windows/User", "0.1.0", Description = "Manage local Windows user accounts", Tags = ["windows", "user", "account"])]
[ExitCode(0, Description = "Success")]
[ExitCode(1, Exception = typeof(Exception), Description = "Error")]
[ExitCode(2, Exception = typeof(JsonException), Description = "Invalid JSON")]
[ExitCode(3, Exception = typeof(SecurityException), Description = "Access denied")]
[ExitCode(4, Exception = typeof(ArgumentException), Description = "Invalid argument")]
[ExitCode(5, Exception = typeof(UnauthorizedAccessException), Description = "Unauthorized access")]
[ExitCode(6, Exception = typeof(PrincipalExistsException), Description = "User already exists")]
public sealed class Resource(JsonSerializerContext context) : DscResource<Schema>(context), IGettable<Schema>, ISettable<Schema>, IDeletable<Schema>, IExportable<Schema>
{
    public override string GetSchema()
    {
        var config = new SchemaGeneratorConfiguration()
        {
            PropertyNameResolver = PropertyNameResolvers.CamelCase
        };

        var builder = new JsonSchemaBuilder().FromType<Schema>(config);
        builder.Schema("https://json-schema.org/draft/2020-12/schema");
        var schema = builder.Build();

        return JsonSerializer.Serialize(schema);
    }

    public Schema Get(Schema? instance)
    {
        ArgumentNullException.ThrowIfNull(instance);

        using var context = new PrincipalContext(ContextType.Machine);
        var user = UserPrincipal.FindByIdentity(context, IdentityType.SamAccountName, instance.UserName);

        if (user == null)
        {
            return new Schema()
            {
                UserName = instance.UserName,
                Exist = false
            };
        }

        using (user)
        {
            return new Schema()
            {
                UserName = user.SamAccountName,
                FullName = user.DisplayName,
                Description = user.Description,
                Disabled = user.Enabled.HasValue ? !user.Enabled.Value : null,
                PasswordNeverExpires = user.PasswordNeverExpires,
                UserMayNotChangePassword = user.UserCannotChangePassword
            };
        }
    }

    public SetResult<Schema>? Set(Schema? instance)
    {
        ArgumentNullException.ThrowIfNull(instance);

        using var context = new PrincipalContext(ContextType.Machine);
        var user = UserPrincipal.FindByIdentity(context, IdentityType.SamAccountName, instance.UserName);

        if (user == null)
        {
            if (string.IsNullOrEmpty(instance.Password))
            {
                throw new ArgumentException("Password is required when creating a new user.");
            }

            user = new UserPrincipal(context)
            {
                SamAccountName = instance.UserName,
                DisplayName = instance.FullName,
                Description = instance.Description,
                Enabled = !instance.Disabled.HasValue || !instance.Disabled.Value,
                PasswordNeverExpires = instance.PasswordNeverExpires ?? false,
                UserCannotChangePassword = instance.UserMayNotChangePassword ?? false
            };

            user.SetPassword(instance.Password);

            try
            {
                user.Save();
            }
            finally
            {
                user.Dispose();
            }

            return null;
        }

        using (user)
        {
            bool changed = false;

            if (instance.FullName != null && user.DisplayName != instance.FullName)
            {
                user.DisplayName = instance.FullName;
                changed = true;
            }

            if (instance.Description != null && user.Description != instance.Description)
            {
                user.Description = instance.Description;
                changed = true;
            }

            if (instance.Disabled.HasValue && user.Enabled.HasValue && user.Enabled.Value == instance.Disabled.Value)
            {
                user.Enabled = !instance.Disabled.Value;
                changed = true;
            }

            if (instance.PasswordNeverExpires.HasValue && user.PasswordNeverExpires != instance.PasswordNeverExpires.Value)
            {
                user.PasswordNeverExpires = instance.PasswordNeverExpires.Value;
                changed = true;
            }

            if (instance.UserMayNotChangePassword.HasValue && user.UserCannotChangePassword != instance.UserMayNotChangePassword.Value)
            {
                user.UserCannotChangePassword = instance.UserMayNotChangePassword.Value;
                changed = true;
            }

            if (!string.IsNullOrEmpty(instance.Password))
            {
                user.SetPassword(instance.Password);
                changed = true;
            }

            if (changed)
            {
                user.Save();
            }
        }

        return null;
    }

    public void Delete(Schema? instance)
    {
        ArgumentNullException.ThrowIfNull(instance);

        using var context = new PrincipalContext(ContextType.Machine);
        var user = UserPrincipal.FindByIdentity(context, IdentityType.SamAccountName, instance.UserName);

        if (user != null)
        {
            using (user)
            {
                user.Delete();
            }
        }
    }

    public IEnumerable<Schema> Export(Schema? filter)
    {
        using var context = new PrincipalContext(ContextType.Machine);
        using var searcher = new PrincipalSearcher(new UserPrincipal(context));

        foreach (var result in searcher.FindAll())
        {
            if (result is UserPrincipal user)
            {
                using (user)
                {
                    yield return new Schema
                    {
                        UserName = user.SamAccountName,
                        FullName = user.DisplayName,
                        Description = user.Description,
                        Disabled = user.Enabled.HasValue ? !user.Enabled.Value : null,
                        PasswordNeverExpires = user.PasswordNeverExpires,
                        UserMayNotChangePassword = user.UserCannotChangePassword
                    };
                }
            }
        }
    }
}
