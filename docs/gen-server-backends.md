# gen_server alternatives

OTP29.1.1 `gen_server` now has a complete42-export C# alternative in `src/Otp/Managed/Erlang.Otp.Stdlib/Processes/GenServer`. The original `.erl` and `.beam` assets are retained byte-for-byte. Both execute under the same MFA API on the .NET runtime. The separate host `Erlang.Otp.GenServer` facade remains available.

## Selection

Choose `RuntimeModuleSelection.Managed("gen_server")` or `RuntimeModuleSelection.Beam("gen_server", image, dependencies)` when creating `RuntimeLibrarySelection`. Managed selection installs `GenServerModule` through its explicit managed-library stage. The startup snapshot records the selected assembly/image identity. There is no fallback, automatic replacement of an Erlang server or live state migration.

The selector is not a boot distribution. Install the connected original `gen`, `proc_lib`, `sys`, logging, formatting and other dependency modules before use. `global` and caller-provided `via` registries are used when those naming forms are requested. The focused validator provides executable composition with original helpers and a real original init/loader/code_server boot. Its group leader uses the existing runtime I/O device; no gen_server, sys, proc_lib or CT result is mocked.

## Implementation

All42 non-module_info exports are implemented, including behaviour_info, startup/monitoring, call/cast/reply, broadcasts/multi_call, request collections, enter_loop, system callbacks and both Logger report formats. The C# engine implements callback result and throw handling, continuation priority, ordinary/zero/absolute timeouts, timeout/hibernate action options, timer cancellation/stale-message handling, hibernation, parent exits, optional callbacks, termination and guaranteed stop replies. System state preserves the original server_data record and four-element state list; original sys owns suspension/resumption and its successor loop. Source-tail boundaries and callback source sites preserve exception stacks.

The long-lived engine uses the existing opaque source-wrapper boundary and existing terms/process facilities. It adds no borrowed-heap optimization or external actor package. Protocol atoms, function names, dependencies, status keys, log formats, record offsets and source sites have separate semantic owners. Ericsson Apache-2.0 attribution is retained.

## Evidence and limits

The [focused receipt](validation/gen-server-port-20261105/README.md) records source/BEAM/assembly/corpus identities. Each backend passes107 complete API observations and70 scenarios against official OTP29.1.1, including four unchanged upstream tests: calling_self, get_state, replace_state and reply_by_alias_with_payload. Effects include actual local/via startup, aliases, request collections, multi_call, state replacement/code change/debug/status, all callback-error origins, parent exit and exactly-once termination. Complete returned terms and stacks are compared. Only known caller/server PID bindings are projected to named markers; no stack frame or error reason is dropped.

This finite Windows acceptance does not certify universal100% OTP compatibility. The full upstream suite, distributed/global integration, every scheduling/resource/owned-heap interleaving, cross-platform behavior, actual structured Logger handler delivery and module metadata remain separate acceptance gates. Logger primary delivery is disabled in the focused fixture; formatting is compared separately. No performance claim, full regression or canonical readiness refresh is made.
