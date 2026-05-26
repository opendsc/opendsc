// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Reflection;

using AwesomeAssertions;

using OpenDsc.Client.Commands;
using OpenDsc.Client.Services;

using Xunit;

namespace OpenDsc.Client.Commands.Tests;

[Trait("Category", "Unit")]
public sealed class ClientCoverageCmdletsTests
{
    [Fact]
    public void EveryClientServiceMethod_HasGeneratedCmdletClass()
    {
        var commandAssembly = typeof(NewDscServerSessionCommand).Assembly;
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

        var shortfall = new List<string>();

        foreach (var serviceType in serviceTypes)
        {
            var domain = serviceType.Name.Replace("HttpService", string.Empty, StringComparison.Ordinal);
            var serviceMethodCount = serviceType
                .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                .Count(m => !m.IsSpecialName);

            var cmdletNamespace = $"OpenDsc.Client.Commands.{domain}";
            var cmdletCount = commandAssembly.GetTypes()
                .Count(t => t.Namespace == cmdletNamespace
                         && t.Name.EndsWith("Command", StringComparison.Ordinal));

            if (cmdletCount < serviceMethodCount)
            {
                shortfall.Add($"{domain}: {cmdletCount} cmdlets < {serviceMethodCount} service methods");
            }
        }

        shortfall.Should().BeEmpty();
    }
}
