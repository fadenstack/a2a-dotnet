# Fadenstack fork of the A2A .NET SDK

This repository is a fork of [a2aproject/a2a-dotnet](https://github.com/a2aproject/a2a-dotnet), maintained by Fadenstack.
It is licensed under the Apache License 2.0 (see `LICENSE`). All upstream copyright and attribution notices are kept.
This file is the notice of changes required by section 4(b) of the Apache License 2.0.

## Why this fork exists

The Fadenstack Office add-ins run on .NET Framework 4.8 (VSTO) and need the real A2A client and server types.
Upstream removed the `netstandard2.0` target of the `A2A` package in the v1 work
([#300](https://github.com/a2aproject/a2a-dotnet/pull/300)). Upstream issue
[#197](https://github.com/a2aproject/a2a-dotnet/issues/197) tracks restoring it, and a maintainer proposed a plan
in a comment on that issue. This fork implements that plan on the branch `feat/netstandard2.0`, so that the change can
become an upstream pull request. Until upstream ships `netstandard2.0` again, Fadenstack consumes this fork.

Support statement: the transport-neutral `A2A` package supports .NET Standard 2.0, .NET 8 and .NET 10.
`A2A.AspNetCore` requires .NET 8 or later. `A2A.Grpc`, `A2A.Grpc.AspNetCore` and `A2A.V0_3Compat` are unchanged and also stay on modern .NET.

## Package identity

The package id stays `A2A` and the assemblies stay signed with upstream's strong-name key (`Open.snk`), so returning to the official
package later is a version bump. Fork versions are `<upstream version>-fs.<n>`, for example `1.0.0-preview4-fs.1`.
The package on the `fadenstack` GitHub Packages feed is private, and its repository URL names this fork, because GitHub Packages
links a package only to a repository of the feed's owner. The project URL stays upstream's.

## Behavior change to know about

`IAgentHandler.CancelAsync` was a default interface member, which does not exist on `netstandard2.0`. It is now the optional interface
`IAgentCancellationHandler` with the same public contract on every target:

```csharp
public interface IAgentHandler
{
    Task ExecuteAsync(RequestContext context, AgentEventQueue eventQueue, CancellationToken cancellationToken);
}

public interface IAgentCancellationHandler
{
    Task CancelAsync(RequestContext context, AgentEventQueue eventQueue, CancellationToken cancellationToken);
}
```

`A2AServer` calls `CancelAsync` when the handler also implements `IAgentCancellationHandler`; otherwise it performs the previous default
(a transition to `TaskState.Canceled`). Handlers that overrode `CancelAsync` must add `IAgentCancellationHandler` to their declaration.

On `netstandard2.0` the public properties `A2AException.ActivatedExtensions`, `A2ARequestContext.RequestedExtensions` and
`A2ARequestContext.ActivatedExtensions` are typed `IReadOnlyCollection<string>` because `IReadOnlySet<T>` does not exist there
(`ExtensionSet` alias, `src/A2A/Polyfills/GlobalUsings.cs`). The net8.0 and net10.0 assemblies are unchanged.
`HttpRequestException.StatusCode` is not available before .NET 5, so the card resolver logs a generic status there.

## Changed and added files (relative to upstream main 8da203a)

Library (`src/A2A`)

- `src/A2A/A2A.csproj`: add `netstandard2.0`, condition `IsAotCompatible`, restore `System.Threading.Channels` and `System.Text.Json` references for it, suppress `VSTHRD003` for the `WaitAsync` fallback, add `InternalsVisibleTo` for `A2A.Net48.UnitTests`.
- `src/A2A/Polyfills/System/Runtime/CompilerServices/IsExternalInit.cs`: restored from v0.1.0-preview.2; lets `init` compile.
- `src/A2A/Polyfills/System/Runtime/CompilerServices/RequiredMemberAttribute.cs`: restored; lets `required` compile.
- `src/A2A/Polyfills/System/Runtime/CompilerServices/CompilerFeatureRequiredAttribute.cs`: restored; companion of `required`.
- `src/A2A/Polyfills/System/Diagnostics/CodeAnalysis/UnconditionalSuppressMessageAttribute.cs`: restored; used by the AOT annotations.
- `src/A2A/Polyfills/System/Runtime/CompilerServices/CallerArgumentExpressionAttribute.cs`: new; needed by the `ThrowIfNull` fallbacks.
- `src/A2A/Polyfills/System/Diagnostics/CodeAnalysis/NullableAttributes.cs`: new; `NotNullWhen` and `SetsRequiredMembers`.
- `src/A2A/Polyfills/System/Collections/Frozen/FrozenSet.cs`: new; minimal `FrozenSet<T>` and `ToFrozenSet`.
- `src/A2A/Polyfills/System/Threading/Tasks/TaskCompletionSource.cs`: new; non-generic `TaskCompletionSource`.
- `src/A2A/Polyfills/FrameworkPolyfills.cs`: new; the central framework fallbacks (`ArgumentNullException.ThrowIfNull` and related guards, `Task.WaitAsync`, `CancellationTokenSource.CancelAsync`, `HttpContent` reads with cancellation, `HttpRequestException.StatusCode`, `Dns.GetHostAddressesAsync` with a token, hashing helpers, `CryptographicOperations.FixedTimeEquals`, `GetValueOrDefault`).
- `src/A2A/Polyfills/GlobalUsings.cs`: new; the `ExtensionSet` alias (`IReadOnlySet<string>` or `IReadOnlyCollection<string>`).
- `src/A2A/Client/JsonRpcContent.cs`: compile the cancellation-token `HttpContent` overrides only where the framework has them.
- `src/A2A/A2AException.cs`, `src/A2A/Client/A2AClient.cs`, `src/A2A/Client/A2AResponseActivation.cs`, `src/A2A/Server/A2ARequestContext.cs`: use the `ExtensionSet` alias instead of `IReadOnlySet<string>`.
- `src/A2A/Client/A2ACardResolver.cs`, `src/A2A/Server/A2AServer.cs`, `src/A2A/Server/PushNotifications/InMemoryPushNotificationStore.cs`: null-forgiving operators where `netstandard2.0` lacks nullable flow annotations (`string.IsNullOrEmpty`), and a string slice instead of a span parse.
- `src/A2A/Server/A2ACustomOperationRegistry.cs`: copy dictionaries with `ToDictionary` (the dictionary copy constructor over `IEnumerable` does not exist on `netstandard2.0`).
- `src/A2A/Server/PushNotifications/DefaultPushNotificationUrlValidator.cs`: wrap `Dns.GetHostAddressesAsync` in a lambda so the token overload resolves on every target.
- `src/A2A/Server/IAgentHandler.cs`: remove the default interface member `CancelAsync`.
- `src/A2A/Server/IAgentCancellationHandler.cs`: new; the optional cancellation interface.
- `src/A2A/Server/A2AServer.Execution.cs`: call `IAgentCancellationHandler` when implemented, otherwise perform the default canceled-task transition.

Tests, samples and docs

- `tests/A2A.UnitTests/Server/AgentCancellationHandlerTests.cs`: new; focused cancellation tests.
- `tests/A2A.Net48.UnitTests/A2A.Net48.UnitTests.csproj`: new; runs a subset of `A2A.UnitTests` on `net48` against the `netstandard2.0` build.
- `tests/A2A.NetStandardConsumer/*`: new; a `netstandard2.0` library compiled against the packed `A2A` package.
- `tests/A2A.UnitTests/GitHubIssues/Issue401.cs`, `Issue495.cs`, `Server/A2AServerTests.cs`, `Server/A2AStandardRequestContextTests.cs`, `Server/PushNotificationServerTests.cs`, `Server/TaskContinuationTests.cs`, `tests/A2A.AspNetCore.UnitTests/A2AExtensionAgentCardHostingTests.cs`, `A2AHttpProcessorTests.cs`, `A2AJsonRpcProcessorTests.cs`, `A2AServiceCollectionExtensionsTests.cs`, `PushNotificationRegistrationTests.cs`, `tests/PushNotificationReceiver.Tests/PushNotificationIntegrationTests.cs`: test handlers that override `CancelAsync` now declare `IAgentCancellationHandler`.
- `tests/A2A.V0_3Compat.UnitTests/V03ServerProcessorTests.cs`: drop a `CancelAsync` that only repeated the default.
- `itk/ItkAgent.cs`, `samples/AgentServer/ProactiveAgent.cs`, `samples/FileStoreDemo/Program.cs`, `samples/PushNotificationReceiver/SdkPushNotificationDemo.cs`: handlers that override `CancelAsync` now declare `IAgentCancellationHandler`.
- `README.md`, `docs/taskmanager-migration-guide.md`: support statement and the new cancellation interface.
- `FORK.md`: this file.

CI (new files only; upstream workflows are untouched)

- `.github/workflows/fs-build-and-test.yml`: build all targets, pack, assert package contents, compile the consumer, test on net8.0, net10.0 and net48 (Windows).
- `.github/workflows/fs-sync-upstream.yml`: weekly merge of upstream main into `feat/netstandard2.0`.
- `.github/workflows/fs-publish.yml`: on tags `fs-v*`, pack (with the fork as the package's repository URL) and publish to GitHub Packages of the `fadenstack` org.
- `tests/.editorconfig`: turns off CA2026 for upstream's test code, which the CI runners' toolchain started flagging in November 2026 (upstream's CI fails the same way); drop it when upstream fixes its tests.
- `.github/scripts/assert-a2a-package.ps1`: asserts the packed package has `lib/netstandard2.0/A2A.dll` and the expected dependency groups.

## How the fork is kept current

- `remote upstream` is `https://github.com/a2aproject/a2a-dotnet.git`. The fork changes live on `feat/netstandard2.0`; `main` mirrors upstream main.
- The weekly workflow `fs-sync-upstream.yml` (Mondays 05:00 UTC, also manual) merges `upstream/main` into `feat/netstandard2.0`, builds, and runs the net8.0, net10.0 and net48 tests.
  It pushes only when the merge is clean and everything is green. When the merge conflicts or a build or test fails, it opens a pull request
  from `sync/upstream-<date>` into `feat/netstandard2.0` so a person can resolve it. (Repository setting "Allow GitHub Actions to create pull requests" must be on.)
- Manual sync: `git fetch upstream && git checkout feat/netstandard2.0 && git merge upstream/main`, then `dotnet build A2A.slnx` and the tests.
- New code upstream that uses a newer framework API will fail the `netstandard2.0` build; add the fallback to `src/A2A/Polyfills/FrameworkPolyfills.cs` rather than adding `#if` at the call site.
- Releasing: push a tag `fs-v<n>`. The workflow publishes `<upstream version>-fs.<n>` (read from `src/Directory.Build.props`) to `https://nuget.pkg.github.com/fadenstack/index.json`.
- Goal: offer the change upstream (issue #197). When upstream restores `netstandard2.0`, drop the fork and use the official package.
