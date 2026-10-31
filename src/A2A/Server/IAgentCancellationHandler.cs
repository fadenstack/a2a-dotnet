namespace A2A;

/// <summary>
/// Optionally customizes task cancellation for an <see cref="IAgentHandler"/>.
/// When the handler passed to <see cref="A2AServer"/> also implements this interface,
/// the server calls <see cref="CancelAsync"/> for a cancel request. Otherwise the server
/// transitions the task to <see cref="TaskState.Canceled"/> itself.
/// </summary>
public interface IAgentCancellationHandler
{
    /// <summary>
    /// Handle task cancellation. Implement for custom cleanup logic (abort LLM calls,
    /// release resources) and emit the terminal state with <see cref="TaskUpdater.CancelAsync"/>.
    /// </summary>
    /// <param name="context">Pre-resolved context with IDs, existing task, and message.</param>
    /// <param name="eventQueue">Channel to write response events to.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task CancelAsync(RequestContext context, AgentEventQueue eventQueue, CancellationToken cancellationToken);
}
