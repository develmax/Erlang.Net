# erpc backends

The complete OTP-29.1.1 `erpc` library has a C# alternative alongside the retained original Erlang source and BEAM image. Both explicit backends expose all 33 OTP exports, including six internal exports. The runtime provides `module_info` metadata. Default OTP registration exposes the legacy `call/4`, `call/5` and `execute_call/4` entry points.

## Selection

Choose before starting the runtime:

```csharp
await using var runtime = await ProcessRuntime.CreateAsync(
    new RuntimeLibrarySelection([RuntimeModuleSelection.Managed("erpc")]));
```

The original implementation uses the same Erlang MFA API on the .NET runtime:

```csharp
var choices = new RuntimeLibrarySelection([
    RuntimeModuleSelection.Managed("erlang"),
    RuntimeModuleSelection.Managed("maps"),
    RuntimeModuleSelection.Beam("erpc", originalErpcBeam, ["erlang", "maps"])
]);
await using var runtime = await ProcessRuntime.CreateAsync(choices);
```

Supply the pinned original `src/Otp/Original/Foundation/Erlang.Otp.Kernel.Beam/Otp/ebin/erpc.beam` bytes. Its matching Erlang source remains under `Otp/src/erpc.erl`. `RuntimeLibraryManifest` supports the same Managed/Beam choices. Selection diagnostics retain backend, module image and assembly identities. Ordinary OTP registration preserves an explicitly selected erpc backend, and the legacy `rpc:call` facade dispatches through it. The .NET runtime executes the selected BEAM image directly.

The C# implementation composes the runtime's existing apply, selective receive, spawn_request, monitor/demonitor/abandon, node identity and clock primitives. Request IDs preserve OTP's improper `[ResultRef|RequestRef]` form; collections retain `RequestRef => [ResultRef|Label]`. `wait_response` leaves live requests pending, while a receive timeout abandons them. Collection errors carry reason, label and the selected retained/deleted collection. Multicall sends requests concurrently, optionally applies one local call in its caller, shares a native-time deadline and maps replies back to input order. Invalid node lists clean up requests already sent.

## Focused evidence

[The receipt](validation/erpc-pr-20261106/review/README.md) records the actual source/assembly hashes, admitted official provider, commands, earlier failures and final outcomes. The permanent corpus covers 309 complete API/error-stack observations, 18 stateful groups and three unchanged upstream cases: `call_always_spawn`, `multicall_always_spawn`, `timeout_limit`. Both implementations pass the same 330 observations. Eight existing connected RPC/spawn/monitor checks are replayed separately over the current implementation; the report records the test assembly built from the isolated checkout.

Only dynamic PID/reference/fun identities are projected by the observer; deterministic values, exception classes, reasons, error metadata, full stacks and observable mailbox effects remain compared. Some groups intentionally return narrower predicates, such as caller-versus-child identity and empty mailbox counts. This does not equate physical resource/scheduler state between runtimes.

Reproduce against the admitted official provider:

```powershell
dotnet build tools/Validation/Erlang.ErpcCompatibility/Erlang.ErpcCompatibility.csproj -m:1
pwsh -File tools/validate-erpc-original.ps1 -Output artifacts/erpc-new-capture -RunnerDll tools/Validation/Erlang.ErpcCompatibility/bin/Debug/net10.0/Erlang.ErpcCompatibility.dll
dotnet tools/Validation/Erlang.ErpcCompatibility/bin/Debug/net10.0/Erlang.ErpcCompatibility.dll . artifacts/erpc-new-capture Connected
```

The capture script refuses changed pinned sources/BEAM/suite, changed official provider files and an existing output directory. The runner refuses changed source/capture hashes, a different baseline and partial case counts. `-BacktraceDepth` produces a separate stack-depth capture.

## Acceptance limits

The comparison covers the recorded OTP 29.1.1 corpus. Universal compatibility still requires the full upstream suite with real peers, old releases and EI nodes, as well as distribution timing, resource exhaustion, scheduling and platform checks. Canonical readiness reports the three exports in the default registry. The explicit 33-export backends have a separate acceptance report.
