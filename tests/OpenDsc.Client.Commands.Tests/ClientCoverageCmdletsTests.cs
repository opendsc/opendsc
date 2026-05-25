// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Reflection;

using AwesomeAssertions;

using OpenDsc.Client.Services;

using Xunit;

namespace OpenDsc.Client.Commands.Tests;

[Trait("Category", "Unit")]
public sealed class ClientCoverageCmdletsTests
{
    [Fact]
    public void EveryClientServiceMethod_HasGeneratedCmdletClass()
    {
        var commandAssembly = typeof(ConnectDscServerCommand).Assembly;
        var serviceTypes = new[]
        {
            typeof(ConfigurationHttpService),
            typeof(CompositeConfigurationHttpService),
            typeof(NodeHttpService),
            typeof(HealthHttpService),
            typeof(SettingsHttpService),
            typeof(ScopeHttpService),
            typeof(ParameterHttpService),
            typeof(ReportHttpService),
            typeof(UserHttpService),
            typeof(GroupHttpService),
            typeof(RoleHttpService),
            typeof(RegistrationKeyHttpService),
        };

        var missing = new List<string>();

        foreach (var serviceType in serviceTypes)
        {
            var domain = serviceType.Name.Replace("HttpService", string.Empty, StringComparison.Ordinal);
            var methods = serviceType.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                .Where(m => !m.IsSpecialName);

            foreach (var method in methods)
            {
                var stem = method.Name.EndsWith("Async", StringComparison.Ordinal)
                    ? method.Name[..^5]
                    : method.Name;

                var expected = $"OpenDsc.Client.Commands.{domain}{stem}Command";
                if (commandAssembly.GetType(expected, throwOnError: false) is null)
                {
                    missing.Add(expected);
                }
            }
        }

        missing.Should().BeEmpty();
    }
}
