// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Text.Json.Serialization;

using OpenDsc.Resource;

namespace TestResource.Multi;

public sealed class UserSchema
{
    public required string Name { get; set; }
    public string? FullName { get; set; }
    [JsonPropertyName("_exist")]
    public bool? Exist { get; set; }
}

[DscResource("TestResource.Multi/User", "1.0.0", Description = "Manages user accounts", Tags = ["user", "account"], SetReturn = SetReturn.State, TestReturn = TestReturn.State)]
[ExitCode(0, Description = "Success")]
[ExitCode(1, Description = "Invalid parameter")]
[ExitCode(2, Exception = typeof(Exception), Description = "Unhandled error")]
public sealed class UserResource(JsonSerializerContext context) : DscResource<UserSchema>(context),
    IGettable<UserSchema>, ISettable<UserSchema>, ITestable<UserSchema>
{
    private static readonly Dictionary<string, string> _users = new()
    {
        ["TestUser"] = "Test User"
    };

    public UserSchema Get(UserSchema instance)
    {
        var exists = _users.TryGetValue(instance.Name, out var fullName);
        return new UserSchema
        {
            Name = instance.Name,
            FullName = exists ? fullName : null,
            Exist = exists == false ? false : null
        };
    }

    public SetResult<UserSchema>? Set(UserSchema instance)
    {
        var changedProperties = new List<string>();

        if (instance.Exist == false)
        {
            if (_users.ContainsKey(instance.Name))
            {
                _users.Remove(instance.Name);
                changedProperties.Add(nameof(UserSchema.Exist));
            }
        }
        else
        {
            if (!_users.TryGetValue(instance.Name, out string? value))
            {
                _users[instance.Name] = instance.FullName ?? string.Empty;
                changedProperties.Add(nameof(UserSchema.Exist));
            }
            else if (value != instance.FullName)
            {
                _users[instance.Name] = instance.FullName ?? string.Empty;
                changedProperties.Add(nameof(UserSchema.FullName));
            }
        }

        var actualState = Get(instance);
        return new SetResult<UserSchema>(actualState)
        {
            ChangedProperties = changedProperties.Count > 0 ? [.. changedProperties] : null
        };
    }

    public TestResult<UserSchema> Test(UserSchema instance)
    {
        var current = Get(instance);
        var differingProperties = new List<string>();

        if (instance.Exist != current.Exist)
        {
            differingProperties.Add(nameof(UserSchema.Exist));
        }

        if (instance.Exist != false && instance.FullName != current.FullName)
        {
            differingProperties.Add(nameof(UserSchema.FullName));
        }

        return new TestResult<UserSchema>(current)
        {
            DifferingProperties = differingProperties.Count > 0 ? [.. differingProperties] : null
        };
    }
}
