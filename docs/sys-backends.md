# sys alternatives

`sys` has a complete C# implementation of all 37 non-`module_info` exports,
including generated `behaviour_info/1`, alongside the unchanged OTP-29.1.1
Erlang source and BEAM. Both alternatives execute on our .NET runtime. Original
ERTS is used only by the development oracle.

## Selecting the implementation

```csharp
RuntimeModuleSelection.Managed("sys")
RuntimeModuleSelection.Beam("sys", originalImage, dependencies)
```

Use the existing [startup library selection contract](RUNTIME_LIBRARY_SELECTION.md)
or an `ErlangLibrary` MSBuild item with `Include="sys"` and the chosen backend.
A BEAM selection must provide the actual image, its hash and explicit dependency
closure. `SysModule.Register(registry)` supports explicit initial composition.
`SysModule.Exports` describes the selected surface. The C# alternative is opt-in;
the legacy default retains its existing `terminate/2,3` registrations.

Selection precedes execution. There is no automatic fallback to original sys,
live backend switching or process-state migration. Managed source wrappers and
original BEAM retain separate module ownership and real assembly/image identity.

## Ported behavior

| Area | Included behavior |
| --- | --- |
| Requests | suspend/resume, status/state, state replacement, code change and termination; default and explicit timeouts |
| System loop | running/suspended state, selective system/parent EXIT receive, hibernation, replies before continuation/termination |
| Callbacks | optional state/status callbacks, fallback state access, bad returns, error/exit/throw handling, code-change outcomes |
| Debugging | trace, statistics, file logging, disabling debugging, identified and legacy user callbacks, removal on done/failure |
| Log data | OTP's bounded two-list/down-counter representation, ordering, options, retrieval, printing and duplicate installation rules |
| Errors | pinned source frames, request-specific exit context and configured backtrace depth |

The transport composes the selected `gen:call/reply`; hibernation uses `proc_lib`.
List primitives, process operations, files and user formatting functions remain
shared dependencies. Install their real closure before using the corresponding
feature. Calling shared helpers does not execute original sys as a fallback.
Upstream Apache-2.0 attribution is retained; no new package is required.

## Evidence and limits

[Part604](validation/part-604/README.md) records 137/137 observations per backend
and 52/52 error observations per backend at depths 0, 1, 2, 3 and 32; depth 8 is
also covered by the full corpus. All final receipts identify the same four
assemblies. The corpus includes five unchanged upstream testcase bodies, real
file output and original gen_server/gen_event/gen_statem integration. Four permanent reference controls exercise the shared BEAM tail-fun handling already present in main.

Finite Windows comparisons establish the stated scope, not universal 100% OTP
compatibility. Complete Common Test capture integration, genuine remote/global/via
peers, all platforms, physical scheduling/resources and module metadata remain
outside this acceptance. Full regression and canonical readiness regeneration
were deliberately not run. No compatibility-ledger promotion or performance
claim follows from export counts.
