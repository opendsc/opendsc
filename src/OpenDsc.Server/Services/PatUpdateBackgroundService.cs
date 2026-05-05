// Copyright (c) Thomas Nieto - All Rights Reserved
// You may use, distribute and modify this code under the
// terms of the MIT license.

using System.Threading.Channels;

namespace OpenDsc.Server.Services;

/// <summary>
/// Queue for PAT last-used updates that are processed in the background.
/// </summary>
public interface IPatUpdateQueue
{
    void Enqueue(Guid tokenId, string ipAddress);
    ChannelReader<(Guid TokenId, string IpAddress)> Reader { get; }
}

/// <summary>
/// Bounded channel-based implementation of <see cref="IPatUpdateQueue"/>.
/// Drops the oldest update when the channel is full to avoid blocking auth.
/// </summary>
public sealed class PatUpdateQueue : IPatUpdateQueue
{
    private readonly Channel<(Guid TokenId, string IpAddress)> _channel =
        Channel.CreateBounded<(Guid, string)>(new BoundedChannelOptions(512)
        {
            FullMode = BoundedChannelFullMode.DropOldest,
            SingleReader = true
        });

    public ChannelReader<(Guid TokenId, string IpAddress)> Reader => _channel.Reader;

    public void Enqueue(Guid tokenId, string ipAddress)
    {
        _channel.Writer.TryWrite((tokenId, ipAddress));
    }
}

/// <summary>
/// Background service that drains the <see cref="IPatUpdateQueue"/> and persists
/// last-used timestamps for Personal Access Tokens.
/// </summary>
public sealed partial class PatUpdateBackgroundService(
    IPatUpdateQueue queue,
    IServiceScopeFactory scopeFactory,
    ILogger<PatUpdateBackgroundService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        LogStarting();

        await foreach (var (tokenId, ipAddress) in queue.Reader.ReadAllAsync(stoppingToken))
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var patService = scope.ServiceProvider.GetRequiredService<IPersonalAccessTokenService>();
                await patService.UpdateLastUsedAsync(tokenId, ipAddress);
            }
            catch (Exception ex)
            {
                LogUpdateFailed(tokenId, ex);
            }
        }
    }

    [LoggerMessage(Level = LogLevel.Debug, Message = "PAT update background service starting")]
    private partial void LogStarting();

    [LoggerMessage(Level = LogLevel.Warning, Message = "Failed to update last-used timestamp for PAT {TokenId}")]
    private partial void LogUpdateFailed(Guid tokenId, Exception ex);
}
