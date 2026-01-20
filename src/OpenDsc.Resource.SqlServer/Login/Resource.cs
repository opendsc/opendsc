// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Collections.Specialized;
using System.Text.Json;
using System.Text.Json.Serialization;

using Json.Schema;
using Json.Schema.Generation;

using Microsoft.SqlServer.Management.Smo;

using SmoLoginType = Microsoft.SqlServer.Management.Smo.LoginType;

namespace OpenDsc.Resource.SqlServer.Login;

[DscResource("OpenDsc.SqlServer/Login", "0.1.0", Description = "Manage SQL Server logins", Tags = ["sql", "sqlserver", "login", "security"])]
[ExitCode(0, Description = "Success")]
[ExitCode(1, Exception = typeof(Exception), Description = "Error")]
[ExitCode(2, Exception = typeof(JsonException), Description = "Invalid JSON")]
[ExitCode(3, Exception = typeof(ArgumentException), Description = "Invalid argument")]
[ExitCode(4, Exception = typeof(UnauthorizedAccessException), Description = "Unauthorized access")]
[ExitCode(5, Exception = typeof(InvalidOperationException), Description = "Invalid operation")]
public sealed class Resource(JsonSerializerContext context)
    : DscResource<Schema>(context),
      IGettable<Schema>,
      ISettable<Schema>,
      ITestable<Schema>,
      IDeletable<Schema>,
      IExportable<Schema>
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

    public Schema Get(Schema instance)
    {
        var server = SqlConnectionHelper.CreateConnection(instance.ServerInstance, instance.ConnectUsername, instance.ConnectPassword);

        try
        {
            var login = server.Logins.Cast<Microsoft.SqlServer.Management.Smo.Login>()
                .FirstOrDefault(l => string.Equals(l.Name, instance.Name, StringComparison.OrdinalIgnoreCase));

            if (login == null)
            {
                return new Schema
                {
                    ServerInstance = instance.ServerInstance,
                    Name = instance.Name,
                    Exist = false
                };
            }

            var roles = StringCollectionToArray(login.ListMembers());

            return new Schema
            {
                ServerInstance = instance.ServerInstance,
                Name = login.Name,
                LoginType = MapLoginType(login.LoginType),
                DefaultDatabase = login.DefaultDatabase,
                Language = login.Language,
                Disabled = login.IsDisabled,
                PasswordExpirationEnabled = login.LoginType == SmoLoginType.SqlLogin ? login.PasswordExpirationEnabled : null,
                PasswordPolicyEnforced = login.LoginType == SmoLoginType.SqlLogin ? login.PasswordPolicyEnforced : null,
                DenyWindowsLogin = (login.LoginType == SmoLoginType.WindowsUser || login.LoginType == SmoLoginType.WindowsGroup)
                    ? login.DenyWindowsLogin
                    : null,
                CreateDate = login.CreateDate,
                DateLastModified = login.DateLastModified,
                HasAccess = login.HasAccess,
                IsLocked = login.LoginType == SmoLoginType.SqlLogin ? login.IsLocked : null,
                IsPasswordExpired = login.LoginType == SmoLoginType.SqlLogin ? login.IsPasswordExpired : null,
                IsSystemObject = login.IsSystemObject,
                ServerRoles = roles?.Length > 0 ? roles : null
            };
        }
        finally
        {
            if (server.ConnectionContext.IsOpen)
            {
                server.ConnectionContext.Disconnect();
            }
        }
    }

    public SetResult<Schema>? Set(Schema instance)
    {
        var server = SqlConnectionHelper.CreateConnection(instance.ServerInstance, instance.ConnectUsername, instance.ConnectPassword);

        try
        {
            var login = server.Logins.Cast<Microsoft.SqlServer.Management.Smo.Login>()
                .FirstOrDefault(l => string.Equals(l.Name, instance.Name, StringComparison.OrdinalIgnoreCase));

            if (login == null)
            {
                CreateLogin(server, instance);
            }
            else
            {
                UpdateLogin(login, instance);
            }

            return null;
        }
        finally
        {
            if (server.ConnectionContext.IsOpen)
            {
                server.ConnectionContext.Disconnect();
            }
        }
    }

    public TestResult<Schema> Test(Schema instance)
    {
        var actualState = Get(instance);
        bool inDesiredState = true;

        if (instance.Exist == false)
        {
            inDesiredState = actualState.Exist == false;
        }
        else if (actualState.Exist == false)
        {
            inDesiredState = false;
        }
        else
        {
            if (instance.LoginType.HasValue && actualState.LoginType != instance.LoginType)
            {
                inDesiredState = false;
            }

            if (!string.IsNullOrEmpty(instance.DefaultDatabase) &&
                !string.Equals(actualState.DefaultDatabase, instance.DefaultDatabase, StringComparison.OrdinalIgnoreCase))
            {
                inDesiredState = false;
            }

            if (!string.IsNullOrEmpty(instance.Language) &&
                !string.Equals(actualState.Language, instance.Language, StringComparison.OrdinalIgnoreCase))
            {
                inDesiredState = false;
            }

            if (instance.Disabled.HasValue && actualState.Disabled != instance.Disabled)
            {
                inDesiredState = false;
            }

            if (instance.PasswordExpirationEnabled.HasValue &&
                actualState.PasswordExpirationEnabled != instance.PasswordExpirationEnabled)
            {
                inDesiredState = false;
            }

            if (instance.PasswordPolicyEnforced.HasValue &&
                actualState.PasswordPolicyEnforced != instance.PasswordPolicyEnforced)
            {
                inDesiredState = false;
            }

            if (instance.DenyWindowsLogin.HasValue && actualState.DenyWindowsLogin != instance.DenyWindowsLogin)
            {
                inDesiredState = false;
            }

            if (instance.ServerRoles != null && instance.ServerRoles.Length > 0)
            {
                var actualRoles = new HashSet<string>(
                    actualState.ServerRoles ?? [],
                    StringComparer.OrdinalIgnoreCase);
                var desiredRoles = new HashSet<string>(instance.ServerRoles, StringComparer.OrdinalIgnoreCase);

                if (!desiredRoles.SetEquals(actualRoles))
                {
                    inDesiredState = false;
                }
            }
        }

        actualState.InDesiredState = inDesiredState;

        return new TestResult<Schema>(actualState);
    }

    public void Delete(Schema instance)
    {
        var server = SqlConnectionHelper.CreateConnection(instance.ServerInstance, instance.ConnectUsername, instance.ConnectPassword);

        try
        {
            var login = server.Logins.Cast<Microsoft.SqlServer.Management.Smo.Login>()
                .FirstOrDefault(l => string.Equals(l.Name, instance.Name, StringComparison.OrdinalIgnoreCase));

            login?.Drop();
        }
        finally
        {
            if (server.ConnectionContext.IsOpen)
            {
                server.ConnectionContext.Disconnect();
            }
        }
    }

    public IEnumerable<Schema> Export()
    {
        var serverInstance = Environment.GetEnvironmentVariable("SQLSERVER_INSTANCE") ?? ".";
        var username = Environment.GetEnvironmentVariable("SQLSERVER_USERNAME");
        var password = Environment.GetEnvironmentVariable("SQLSERVER_PASSWORD");
        return Export(serverInstance, username, password);
    }

    public IEnumerable<Schema> Export(string serverInstance, string? username = null, string? password = null)
    {
        var server = SqlConnectionHelper.CreateConnection(serverInstance, username, password);

        try
        {
            var logins = new List<Schema>();
            foreach (Microsoft.SqlServer.Management.Smo.Login login in server.Logins)
            {
                if (login.IsSystemObject)
                {
                    continue;
                }

                var roles = StringCollectionToArray(login.ListMembers());

                logins.Add(new Schema
                {
                    ServerInstance = serverInstance,
                    Name = login.Name,
                    LoginType = MapLoginType(login.LoginType),
                    DefaultDatabase = login.DefaultDatabase,
                    Language = login.Language,
                    Disabled = login.IsDisabled,
                    PasswordExpirationEnabled = login.LoginType == SmoLoginType.SqlLogin ? login.PasswordExpirationEnabled : null,
                    PasswordPolicyEnforced = login.LoginType == SmoLoginType.SqlLogin ? login.PasswordPolicyEnforced : null,
                    DenyWindowsLogin = (login.LoginType == SmoLoginType.WindowsUser || login.LoginType == SmoLoginType.WindowsGroup)
                        ? login.DenyWindowsLogin
                        : null,
                    CreateDate = login.CreateDate,
                    DateLastModified = login.DateLastModified,
                    HasAccess = login.HasAccess,
                    IsLocked = login.LoginType == SmoLoginType.SqlLogin ? login.IsLocked : null,
                    IsPasswordExpired = login.LoginType == SmoLoginType.SqlLogin ? login.IsPasswordExpired : null,
                    IsSystemObject = login.IsSystemObject,
                    ServerRoles = roles?.Length > 0 ? roles : null
                });
            }
            return logins;
        }
        finally
        {
            if (server.ConnectionContext.IsOpen)
            {
                server.ConnectionContext.Disconnect();
            }
        }
    }

    private static void CreateLogin(Server server, Schema instance)
    {
        var loginType = instance.LoginType ?? LoginType.SqlLogin;

        var login = new Microsoft.SqlServer.Management.Smo.Login(server, instance.Name)
        {
            LoginType = MapToSmoLoginType(loginType)
        };

        if (!string.IsNullOrEmpty(instance.DefaultDatabase))
        {
            login.DefaultDatabase = instance.DefaultDatabase;
        }

        if (!string.IsNullOrEmpty(instance.Language))
        {
            login.Language = instance.Language;
        }

        if (loginType == LoginType.SqlLogin)
        {
            if (string.IsNullOrEmpty(instance.Password))
            {
                throw new ArgumentException("Password is required when creating a SQL login.");
            }

            if (instance.PasswordExpirationEnabled.HasValue)
            {
                login.PasswordExpirationEnabled = instance.PasswordExpirationEnabled.Value;
            }

            if (instance.PasswordPolicyEnforced.HasValue)
            {
                login.PasswordPolicyEnforced = instance.PasswordPolicyEnforced.Value;
            }

            login.Create(instance.Password);
        }
        else
        {
            login.Create();
        }

        if (instance.Disabled == true)
        {
            login.Disable();
        }

        if ((loginType == LoginType.WindowsUser || loginType == LoginType.WindowsGroup) &&
            instance.DenyWindowsLogin == true)
        {
            login.DenyWindowsLogin = true;
            login.Alter();
        }

        if (instance.ServerRoles != null)
        {
            foreach (var role in instance.ServerRoles)
            {
                login.AddToRole(role);
            }
        }
    }

    private static void UpdateLogin(Microsoft.SqlServer.Management.Smo.Login login, Schema instance)
    {
        bool altered = false;

        if (!string.IsNullOrEmpty(instance.DefaultDatabase) &&
            !string.Equals(login.DefaultDatabase, instance.DefaultDatabase, StringComparison.OrdinalIgnoreCase))
        {
            login.DefaultDatabase = instance.DefaultDatabase;
            altered = true;
        }

        if (!string.IsNullOrEmpty(instance.Language) &&
            !string.Equals(login.Language, instance.Language, StringComparison.OrdinalIgnoreCase))
        {
            login.Language = instance.Language;
            altered = true;
        }

        if (login.LoginType == SmoLoginType.SqlLogin)
        {
            if (instance.PasswordExpirationEnabled.HasValue &&
                login.PasswordExpirationEnabled != instance.PasswordExpirationEnabled.Value)
            {
                login.PasswordExpirationEnabled = instance.PasswordExpirationEnabled.Value;
                altered = true;
            }

            if (instance.PasswordPolicyEnforced.HasValue &&
                login.PasswordPolicyEnforced != instance.PasswordPolicyEnforced.Value)
            {
                login.PasswordPolicyEnforced = instance.PasswordPolicyEnforced.Value;
                altered = true;
            }

            if (!string.IsNullOrEmpty(instance.Password))
            {
                login.ChangePassword(instance.Password);
            }
        }

        if ((login.LoginType == SmoLoginType.WindowsUser || login.LoginType == SmoLoginType.WindowsGroup) &&
            instance.DenyWindowsLogin.HasValue && login.DenyWindowsLogin != instance.DenyWindowsLogin.Value)
        {
            login.DenyWindowsLogin = instance.DenyWindowsLogin.Value;
            altered = true;
        }

        if (altered)
        {
            login.Alter();
        }

        if (instance.Disabled.HasValue)
        {
            if (instance.Disabled.Value && !login.IsDisabled)
            {
                login.Disable();
            }
            else if (!instance.Disabled.Value && login.IsDisabled)
            {
                login.Enable();
            }
        }

        if (instance.ServerRoles != null)
        {
            UpdateServerRoles(login, instance.ServerRoles);
        }
    }

    private static void UpdateServerRoles(Microsoft.SqlServer.Management.Smo.Login login, string[] desiredRoles)
    {
        var currentRolesArray = StringCollectionToArray(login.ListMembers()) ?? [];
        var currentRoles = new HashSet<string>(currentRolesArray, StringComparer.OrdinalIgnoreCase);
        var targetRoles = new HashSet<string>(desiredRoles, StringComparer.OrdinalIgnoreCase);

        var rolesToAdd = targetRoles.Except(currentRoles);
        var rolesToRemove = currentRoles.Except(targetRoles);

        foreach (var role in rolesToAdd)
        {
            login.AddToRole(role);
        }

        var server = login.Parent;
        foreach (var roleName in rolesToRemove)
        {
            var serverRole = server.Roles[roleName];
            serverRole?.DropMember(login.Name);
        }
    }

    private static LoginType MapLoginType(SmoLoginType smoLoginType) => smoLoginType switch
    {
        SmoLoginType.WindowsUser => LoginType.WindowsUser,
        SmoLoginType.WindowsGroup => LoginType.WindowsGroup,
        SmoLoginType.SqlLogin => LoginType.SqlLogin,
        SmoLoginType.Certificate => LoginType.Certificate,
        SmoLoginType.AsymmetricKey => LoginType.AsymmetricKey,
        SmoLoginType.ExternalUser => LoginType.ExternalUser,
        SmoLoginType.ExternalGroup => LoginType.ExternalGroup,
        _ => LoginType.SqlLogin
    };

    private static SmoLoginType MapToSmoLoginType(LoginType loginType) => loginType switch
    {
        LoginType.WindowsUser => SmoLoginType.WindowsUser,
        LoginType.WindowsGroup => SmoLoginType.WindowsGroup,
        LoginType.SqlLogin => SmoLoginType.SqlLogin,
        LoginType.Certificate => SmoLoginType.Certificate,
        LoginType.AsymmetricKey => SmoLoginType.AsymmetricKey,
        LoginType.ExternalUser => SmoLoginType.ExternalUser,
        LoginType.ExternalGroup => SmoLoginType.ExternalGroup,
        _ => SmoLoginType.SqlLogin
    };

    private static string[]? StringCollectionToArray(StringCollection? collection)
    {
        if (collection == null || collection.Count == 0)
        {
            return null;
        }

        var result = new string[collection.Count];
        collection.CopyTo(result, 0);
        return result;
    }
}
