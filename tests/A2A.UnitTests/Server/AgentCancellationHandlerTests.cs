using Microsoft.Extensions.Logging.Abstractions;

namespace A2A.UnitTests.Server;

public class AgentCancellationHandlerTests
{
    private static async Task<(A2AServer Server, InMemoryTaskStore Store)> CreateServerAsync(IAgentHandler handler)
    {
        var store = new InMemoryTaskStore();
        await store.SaveTaskAsync("t1", new AgentTask
        {
            Id = "t1",
            ContextId = "ctx-1",
            Status = new TaskStatus { State = TaskState.Working },
        });
        var server = new A2AServer(handler, store, new ChannelEventNotifier(), NullLogger<A2AServer>.Instance);
        return (server, store);
    }

    [Fact]
    public void AgentHandler_DeclaresOnlyExecuteAsync_OnEveryTarget()
    {
        // Cancellation is optional and must not depend on default interface members, which netstandard2.0 lacks.
        var methods = typeof(IAgentHandler).GetMethods();

        var method = Assert.Single(methods);
        Assert.Equal(nameof(IAgentHandler.ExecuteAsync), method.Name);
        Assert.Equal(nameof(IAgentCancellationHandler.CancelAsync), Assert.Single(typeof(IAgentCancellationHandler).GetMethods()).Name);
    }

    [Fact]
    public async Task GivenHandlerWithoutCancellationHandler_WhenCancelTask_ThenTaskTransitionsToCanceled()
    {
        // Arrange
        var handler = new ExecuteOnlyHandler();
        var (server, store) = await CreateServerAsync(handler);

        // Act
        var result = await server.CancelTaskAsync(new CancelTaskRequest { Id = "t1" }, null);

        // Assert
        Assert.Equal(TaskState.Canceled, result.Status.State);
        Assert.Equal(0, handler.ExecuteCalls);
        Assert.Equal(TaskState.Canceled, (await store.GetTaskAsync("t1"))!.Status.State);
    }

    [Fact]
    public async Task GivenHandlerWithCancellationHandler_WhenCancelTask_ThenCancellationHandlerRuns()
    {
        // Arrange
        var handler = new CancellingHandler();
        var (server, _) = await CreateServerAsync(handler);

        // Act
        var result = await server.CancelTaskAsync(new CancelTaskRequest { Id = "t1" }, null);

        // Assert
        Assert.Equal(TaskState.Canceled, result.Status.State);
        Assert.Equal("t1", handler.CancelledTaskId);
        Assert.Equal(1, handler.CancelCalls);
        Assert.Equal(0, handler.ExecuteCalls);
    }

    [Fact]
    public async Task GivenCancellationHandlerThatFails_WhenCancelTask_ThenFailureIsNotMaskedByDefault()
    {
        // Arrange
        var handler = new CancellingHandler { Failure = new InvalidOperationException("cleanup failed") };
        var (server, _) = await CreateServerAsync(handler);

        // Act & Assert
        await Assert.ThrowsAnyAsync<Exception>(() => server.CancelTaskAsync(new CancelTaskRequest { Id = "t1" }, null));
        Assert.Equal(1, handler.CancelCalls);
    }

    private class ExecuteOnlyHandler : IAgentHandler
    {
        public int ExecuteCalls { get; private set; }

        public Task ExecuteAsync(RequestContext context, AgentEventQueue eventQueue, CancellationToken cancellationToken)
        {
            ExecuteCalls++;
            eventQueue.Complete();
            return Task.CompletedTask;
        }
    }

    private sealed class CancellingHandler : ExecuteOnlyHandler, IAgentCancellationHandler
    {
        public int CancelCalls { get; private set; }

        public string? CancelledTaskId { get; private set; }

        public Exception? Failure { get; init; }

        public async Task CancelAsync(RequestContext context, AgentEventQueue eventQueue, CancellationToken cancellationToken)
        {
            CancelCalls++;
            CancelledTaskId = context.TaskId;
            if (Failure is not null)
            {
                throw Failure;
            }

            var updater = new TaskUpdater(eventQueue, context.TaskId, context.ContextId);
            await updater.CancelAsync(cancellationToken: cancellationToken);
        }
    }
}
