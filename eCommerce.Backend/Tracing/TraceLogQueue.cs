using System.Diagnostics.Metrics;
using System.Threading.Channels;
using eCommerce.Domain.Entities;

namespace eCommerce.Backend.Tracing;

internal sealed class TraceLogQueue(IMeterFactory meters)
{
    private readonly Channel<TraceLog> channel = Channel.CreateBounded<TraceLog>(new BoundedChannelOptions(1024)
    {
        SingleReader = true,
        FullMode = BoundedChannelFullMode.Wait
    });

    private readonly Counter<long> dropped =
        meters.Create("eCommerce.Backend.Tracing").CreateCounter<long>("ecommerce.trace.dropped");

    public ChannelReader<TraceLog> Reader => channel.Reader;

    public void Enqueue(TraceLog entry)
    {
        if (!channel.Writer.TryWrite(entry))
            RecordDropped(1, "queue_unavailable");
    }

    public void Complete() => channel.Writer.TryComplete();

    public void RecordDropped(int count, string reason) =>
        dropped.Add(count, new KeyValuePair<string, object?>("reason", reason));
}
