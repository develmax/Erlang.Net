# gen_statem alternatives

The pinned OTP-29.1.1 `gen_statem` has a C# implementation in
`src/Otp/Managed/Erlang.Otp.Stdlib/Processes/GenStatem` and unchanged original
Erlang/source BEAM assets in `src/Otp/Original/Foundation/Erlang.Otp.Stdlib.Beam/Otp`.
Both run in our .NET runtime under the same MFA API. The development oracle is
the separately admitted original OTP VM; production BEAM execution does not
require that VM.

## Selection and dependencies

Choose the module at startup with `RuntimeModuleSelection.Managed("gen_statem")`
or `RuntimeModuleSelection.Beam("gen_statem", image, dependencies)` through the
existing [library selection contract](RUNTIME_LIBRARY_SELECTION.md).
The managed selection installs `GenStatemModule` through the new `GenStatem`
managed-library stage and records its real assembly identity. It is opt-in;
the default module registry does not acquire forty additional APIs.

The selector is not a boot distribution. Install the connected OTP dependencies
before making calls: `gen`, `proc_lib`, `sys`, logging/formatting modules and their
actual closure. `global` and user-specified `via` registries are reached when
those addressing forms are used. Missing implementations fail normally; there
is no automatic fallback to the other gen_statem backend. The validator shows
an executable explicit setup with original helpers and a real original
`init`/loader/`code_server` boot. Runtime timers, receive, process registration,
monitoring and aliases remain shared primitives.

`GenStatemModule.Register(registry)` also supports explicit initial composition
inside runtime initialization. Backend choice must precede execution; this work
does not provide live state migration or authorize replacing a running machine.
C# callbacks can be registered as ordinary runtime MFAs, just like Erlang
callback modules. `GenStatemModule.Exports` describes the selected module surface.

## Implementation

All forty non-module_info exports are implemented, including the generated
`behaviour_info/1` contract, public requests/replies and internal gen/sys/logger
callbacks. The engine ports both callback modes, optional state-enter callbacks,
all callback result forms, callback throws, module push/pop/change, postponed
events, inserted events, event/state/named timeouts, absolute timers,
cancellation tombstones, zero-timeout ordering, update/cancel, hibernation,
startup outcomes and termination. The sys transport uses the original params
and state record shapes and original sys processing.

The long-lived managed engine uses the existing opaque source-wrapper boundary
instead of assuming that C# state retained across awaits is a compiler-owned
lexical root. This deliberately makes no borrowed-heap optimization or
performance claim. The source continuation helpers retain pinned error frames;
protocol atoms, function/dependency names, status labels and log formats have
separate domain owners. Pinned source lines/private arities, sys record offsets,
receive limits and validation contracts also have semantic owners; clause lines
are explicit rather than computed by subtraction. Upstream Apache-2.0 notices are preserved.

## Focused evidence and limits

[gen_statem publication](validation/gen-statem-port-20261104/README.md) binds source, corpus, original images and
assemblies. Each backend passes seventy-four complete API observations and 311
engine scenarios against the same original OTP corpus. Error observations
compare complete stack-containing terms. This includes a 256-case event/mode
matrix, different callback modules, invalid actions/modes/results, callback
exceptions, zero/real/absolute timers, requests/collections, named startup,
monitors, enter_loop, sys suspend/resume/state replacement/code change/status,
termination and logging format values/errors. The source/export audit proves
exact retained upstream source bytes and the complete forty-export inventory.

These finite Windows results do **not** certify 100% OTP compatibility. Full
upstream gen_statem_SUITE execution, cross-platform/resource/owned-heap stress,
distributed/global/via integration, actual structured Logger handler effects
and module metadata parity remain acceptance work. Logger primary output is
disabled in this focused fixture; formatting and error stacks are compared
separately. The authentic feature bootstrap is captured from original OTP, and
an existing original code_server boot fixture supplies real dependency
processing rather than substitute return values.

Full regression and global readiness regeneration were deliberately not run,
as requested. No compatibility-ledger classification is promoted from corpus
counts. The previous generated readiness snapshot remains historical.

The [sys continuation fix](validation/part-587/README.md) retains the original
proc_lib handler while unwinding completed managed loops through a module driver.
The32-generation retention probe changes97 reachable machines to1; both complete
module comparisons remain74 API observations plus311 engine scenarios per backend.
