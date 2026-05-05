// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Reflection;

using AwesomeAssertions;

using OpenDsc.Server.Authorization;

using Xunit;

namespace OpenDsc.Server.Tests.Authorization;

[Trait("Category", "Unit")]
public class PermissionsTests
{
    [Fact]
    public void AllScopes_ContainsAllConstStringFields()
    {
        var permissionTypes = new[]
        {
            typeof(ServerPermissions),
            typeof(NodePermissions),
            typeof(ReportPermissions),
            typeof(RetentionPermissions),
            typeof(ConfigurationPermissions),
            typeof(CompositeConfigurationPermissions),
            typeof(ParameterPermissions),
            typeof(ScopePermissions)
        };

        var fields = permissionTypes
            .SelectMany(t => t.GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy))
            .Where(f => f.IsLiteral && !f.IsInitOnly && f.FieldType == typeof(string))
            .Select(f => (string)f.GetValue(null)!)
            .ToList();

        Permissions.AllScopes.Should().BeEquivalentTo(fields);
    }

    [Theory]
    [InlineData("nodes.read")]
    [InlineData("server.settings.write")]
    [InlineData("scopes.admin-override")]
    public void AllScopes_ContainsValidScope(string scope)
    {
        Permissions.AllScopes.Should().Contain(scope);
    }

    [Theory]
    [InlineData("test")]
    [InlineData("admin")]
    [InlineData("")]
    [InlineData("nodes.read.extra")]
    public void AllScopes_DoesNotContainInvalidScope(string scope)
    {
        Permissions.AllScopes.Should().NotContain(scope);
    }
}
