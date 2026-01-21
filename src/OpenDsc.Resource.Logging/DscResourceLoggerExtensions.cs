// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace OpenDsc.Resource.Logging;

/// <summary>
/// Extension methods for adding DSC resource logging to <see cref="ILoggerFactory"/>.
/// </summary>
public static class DscResourceLoggerExtensions
{
    /// <summary>
    /// Adds a DSC resource logger provider that outputs JSON messages to stderr.
    /// Respects the DSC_TRACE_LEVEL environment variable for filtering.
    /// </summary>
    /// <param name="builder">The logger factory builder.</param>
    /// <returns>The logger factory builder for method chaining.</returns>
    public static ILoggingBuilder AddDscResource(this ILoggingBuilder builder)
    {
        builder.Services.AddSingleton<ILoggerProvider, DscResourceLoggerProvider>();
        return builder;
    }

    /// <summary>
    /// Adds a DSC resource logger provider that outputs JSON messages to stderr.
    /// Respects the DSC_TRACE_LEVEL environment variable for filtering.
    /// </summary>
    /// <param name="factory">The logger factory.</param>
    /// <returns>The logger factory for method chaining.</returns>
    public static ILoggerFactory AddDscResource(this ILoggerFactory factory)
    {
        factory.AddProvider(new DscResourceLoggerProvider());
        return factory;
    }
}
