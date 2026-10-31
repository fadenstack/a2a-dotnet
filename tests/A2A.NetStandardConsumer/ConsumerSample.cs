using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace A2A.NetStandardConsumer;

/// <summary>Uses the transport-neutral A2A API surface from netstandard2.0.</summary>
public sealed class ConsumerSample : IAgentHandler, IAgentCancellationHandler
{
    public static AgentCard CreateCard() => new()
    {
        Name = "consumer",
        Description = "netstandard2.0 consumer",
        Version = "1.0.0",
        SupportedInterfaces =
        [
            new AgentInterface
            {
                Url = "https://example.test/a2a",
                ProtocolBinding = ProtocolBindingNames.JsonRpc,
                ProtocolVersion = "1.0",
            },
        ],
    };

    public static IA2AClient CreateClient(HttpClient httpClient) => A2AClientFactory.Create(CreateCard(), httpClient);

    public static Message CreateMessage(string text) => new()
    {
        MessageId = "m1",
        Role = Role.User,
        Parts = [Part.FromText(text)],
    };

    public async Task ExecuteAsync(RequestContext context, AgentEventQueue eventQueue, CancellationToken cancellationToken)
    {
        var updater = new TaskUpdater(eventQueue, context.TaskId, context.ContextId);
        await updater.SubmitAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
        await eventQueue.EnqueueMessageAsync(new Message
        {
            MessageId = "reply",
            Role = Role.Agent,
            ContextId = context.ContextId,
            Parts = [Part.FromText("echo: " + context.UserText)],
        }, cancellationToken).ConfigureAwait(false);
        await updater.CompleteAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
    }

    public async Task CancelAsync(RequestContext context, AgentEventQueue eventQueue, CancellationToken cancellationToken)
    {
        var updater = new TaskUpdater(eventQueue, context.TaskId, context.ContextId);
        await updater.CancelAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
    }

    public static async Task<int> CountEventsAsync(AgentEventQueue eventQueue, CancellationToken cancellationToken)
    {
        int count = 0;
        await foreach (var _ in eventQueue.WithCancellation(cancellationToken))
        {
            count++;
        }

        return count;
    }
}
