# Next original OTP pending-exception and variadic observations

Prepared source proposal only. No build, native execution, network call, Main change or Git mutation was performed. The expected policy predates new observations. Nothing here claims collection or semantic success.

## Existing Windows binary usable by root

The exact published Part290 reviewed fixture binary is:

`D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/spawn-protocol-nif-290-38688662194-terminal/artifact-11951535100/nif-fixture-build/fixture.dll`

It is 9728 bytes, SHA256 **235D9E8997B7760E120ECD5CFACA29F01D0936A8FD89EF8B23DDF7D0DB43883F**. Its source SHA256 is **7B6AF591C635DED2FE686A9E8671ACD58F9C8A6A080D47C61420F844FF33F074**. Saved provenance binds run 38688662194, Windows job 116269261154, artifact 11951535100, repository head f2e861afc3f1ff2bdfade705ecb058e7cef433d6, OTP commit ad05823719d77c8faee87348ea39513d4e2f99c5 and reviewed build plan D37C784308E2DB8238949A02FAF035736401AB86538D047D3E0E179A7F738E63.

published290-windows-provenance.json records the actual absolute proof paths and reverified hashes. This task rehashed all 22 build-receipt members, the reviewed source/headers against the Git pin, the saved native result/log, terminal successful run and ZIP. Raw PE headers independently confirm x64. Saved host success is prior evidence, not a new backend run. The DLL imports KERNEL32.dll, VCRUNTIME140.dll and api-ms-win-crt-runtime-l1-1-0.dll; root's loader environment must supply them. No DLL was loaded here.

Candidate2's existing trusted two-function profile accepts this original reviewed source contract and independently trusted binary hash. It does NOT admit the new observation fixture below. Root may use the DLL to validate candidate2's actual Runtime initializer while keeping the new oracle fixture independent.

## Thirteen predeclared original cases

fixture.c and the declaring Erlang module are new test infrastructure using pinned real erl_nif.h. They deliberately exercise tuple/list C varargs for zero, one and seventeen values (register/stack traversal), no pending exception including unchanged reason output, latest reason after repeated raise, ignored badarg, detailed pending flag/reason/sentinel observations, badarg/explicit reason replacement in both orders, and load veto taint persistence. Every case runs in a fresh original VM.

pending_detail exposes raw flag/reason observations as a final exception reason tuple; it does not insert a C pass/fail answer. Its sentinel comes from enif_raise_exception itself. Pinned erl_term.h explicitly changes THE_NON_VALUE in debug mode; raw zero must not be assumed for portable original fixtures. Candidate2's separate SDK-only ABI probe may use its own zero sentinel, but that probe is not reused byte-for-byte as the original oracle.

expected-policy.json contains independent typed projections. Raw ETF includes full class/reason/stack and complete taint lists before and after load. Checks should compare meaningful typed fields and first NIF frame, retaining the entire original stack rather than rewriting it into an expected SDK answer. The native fixture callback performs no host-failure test: host exceptions have no invented original-native counterpart.

## Collector and future workflow

collect.py adapts the existing published collector: the owned-process-group execute function is byte-identical, and check_process_capture.py is byte-identical. It requires an already built, clean-tracked exact OTP source tree and actual configured headers belonging to that tree. The public headers must match pinned blobs; generated erl_int_sizes_config.h is copied and hashed, never synthesized. It records gcc/readelf, actual runtime metadata, disassembly, process exit/stdout/stderr, compiler commands, native symbols/dependencies and raw ETF for thirteen fresh VMs. ERLC_USE_SERVER=false and inherited emulator flags are cleared. Fresh output only; timeout kills/reaps its owned process group. Successful collection explicitly leaves SemanticAssertionsExecuted=false.

workflow-proposal.yml follows the existing pinned original-job conventions. It is not installed or dispatched. Its proposed promotion directory is tools/Erlang.NifPendingVarargsObservations/original; source-plan, fixture.c, collector, Erlang sources and control helper must be copied there intact before using it. Build/install requires the parent's separately scheduled heavy gate. Existing unchanged capture controls may be retained as proof of the byte-identical function; rerunning them should be a deliberate collector-validation step, not confused with any of the thirteen semantic cases.

Independent typed semantic guard implementation remains required after raw collection. Root must also compare the corresponding candidate2 managed/ABI projections, not treat original collection as an SDK pass. Existing seven Part290 observations are not rebranded as thirteen new proofs. No arbitrary NIF loader, general ABI, Windows variadic support, resource/native-thread lifecycle or unload parity is established by these new cases.

## Interoperability policy and provenance

All new C is observation fixture/test infrastructure under work.md section 2.1, not production runtime algorithms. The Python collector is existing process-capture test tooling adapted for new cases. Pin/license provenance for upstream headers remains bound to OTP-29.1.1 Apache-2.0 material; headers are build inputs, not copied into this proposal. No license banner, LICENSE file or distributable native binary is created.
