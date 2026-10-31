namespace A2A;

/// <summary>
/// Defines the agent's execution logic. Implement this for the easy path
/// where <see cref="A2AServer"/> handles task lifecycle, persistence,
/// and observability. Implement <see cref="IAgentCancellationHandler"/> as well to customize
/// task cancellation.
/// </summary>
public interface IAgentHandler
{
    /// <summary>
    /// Execute agent logic for an incoming message.
    /// Use <see cref="TaskUpdater"/> to emit task lifecycle events, or write
    /// directly to <paramref name="eventQueue"/> for message-only responses.
    /// </summary>
    /// <param name="context">Pre-resolved context with IDs, existing task, and message.</param>
    /// <param name="eventQueue">Channel to write response events to.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task ExecuteAsync(RequestContext context, AgentEventQueue eventQueue, CancellationToken cancellationToken);
}
