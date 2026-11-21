using eCommerce.DL.Repositories;
using eCommerce.Domain.Entities;

namespace eCommerce.Backend.Tracing;

internal sealed class TraceLogWorker(TraceLogQueue queue, IServiceScopeFactory scopes, ILogger<TraceLogWorker> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        const int batchSize = 50;
        var batch = new List<TraceLog>(batchSize);
        while (await queue.Reader.WaitToReadAsync(stoppingToken))
        {
            while (batch.Count < batchSize && queue.Reader.TryRead(out var entry))
                batch.Add(entry);

            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
            timeout.CancelAfter(TimeSpan.FromSeconds(5));
            try
            {
                await using var scope = scopes.CreateAsyncScope();
                var repository = scope.ServiceProvider.GetRequiredService<TraceLogRepository>();
                await repository.AppendAsync(batch, timeout.Token);
            }
            catch (Exception exception) when (!stoppingToken.IsCancellationRequested)
            {
                queue.RecordDropped(batch.Count, "write_failed");
                logger.LogWarning("Could not persist {Count} trace records to SQL Server ({ErrorType}).",
                    batch.Count, exception.GetType().Name);
            }
            finally
            {
                batch.Clear();
            }
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        queue.Complete();
        try
        {
            if (ExecuteTask is not null)
                await ExecuteTask.WaitAsync(cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // The host's shutdown deadline has elapsed.
        }
        finally
        {
            await base.StopAsync(cancellationToken);
        }
    }
}
