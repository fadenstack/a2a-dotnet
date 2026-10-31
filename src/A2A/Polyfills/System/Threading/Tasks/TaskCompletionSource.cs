// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if NETSTANDARD2_0
#pragma warning disable CA2016, RCS1141, VSTHRD003
namespace System.Threading.Tasks;

/// <summary>
/// Non-generic <c>TaskCompletionSource</c>, which is not available before .NET 5.
/// </summary>
internal sealed class TaskCompletionSource
{
    private readonly TaskCompletionSource<bool> _source;

    public TaskCompletionSource() => _source = new TaskCompletionSource<bool>();

    public TaskCompletionSource(TaskCreationOptions creationOptions) => _source = new TaskCompletionSource<bool>(creationOptions);

    public Task Task => _source.Task;

    public bool TrySetResult() => _source.TrySetResult(true);

    public void SetResult() => _source.SetResult(true);

    public bool TrySetException(Exception exception) => _source.TrySetException(exception);

    public bool TrySetCanceled(CancellationToken cancellationToken = default) => _source.TrySetCanceled();
}
#endif
