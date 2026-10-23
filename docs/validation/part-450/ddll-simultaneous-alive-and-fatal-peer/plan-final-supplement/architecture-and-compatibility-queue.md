# Architecture and compatibility queue — recorded at the Part450 safe boundary

Status: **planned**, not implemented or newly verified by this documentation supplement. Root is the sole owner of shared runtime/build/project/git changes. This supplement records coordinated owner requirements without interrupting the completed450 functional gates or performing a bulk migration. It extends the existing backend-selection plan rather than replacing working alternatives.

## P0 — eliminate known observable semantic differences

The unchanged compatibility ledger currently classifies31 MFAs as Known differences,118 as Verified compatible and559 as Needs verification. This is a ledger inventory, not proof that every historical difference still reproduces on current binaries. Nearest owner operation after publishing450: revalidate each named row with actual backend, specific cause/input and minimal reproduction; bind pinned original/source evidence and current source/DLL hashes. Record resolved/stale/open/blocked with the exact reason and proof. A stale label must be demonstrated, not assumed.

Group the observations by shared runtime dependencies and first fix genuinely reproduced semantic errors against pinned OTP29.1.1. Maintain a permanent differential regression for every applicable C# and BEAM backend, including complete values, errors/stacks, side effects and lifecycle metadata. Newly discovered differences enter the same inventory and are corrected or retain a factual blocker. Unsupported valid behavior cannot simply be renamed a permissible limitation.

Do not remove cases, weaken comparisons, silently switch backend, delete alternatives or merely reclassify rows to obtain a cosmetic zero. Preserve unstable or VM-owned native data and use explicit source/own-state protocols where byte equality is not a meaningful contract; never fabricate equality.

Acceptance: **zero unresolved known, currently observed semantic differences in the revalidated inventory**, supported by current source/DLL and original proofs. This does not close the559 Needs verification entries and is not a whole-OTP claim. No row is resolved by this plan update.

### Named baseline awaiting current revalidation

The following rows are copied by identity only from `../mfa-compatibility.json`; their existing Scope/Limits/Evidence remain there. All tasks below are open; current reproductions and backend-specific evidence are still to be collected. Baseline ledger SHA256: `42B1CAE6C46F5C3D3F7B7EF190246513DC6954EBC55B4BCD1596AD436469721C`.

| MFA | Current queue state |
| --- | --- |
| `erlang:process_info/2` | Open revalidation task; current reproduction not yet established |
| `erlang:process_info/1` | Open revalidation task; current reproduction not yet established |
| `erlang:spawn_opt/2` | Open revalidation task; current reproduction not yet established |
| `erlang:spawn_opt/3` | Open revalidation task; current reproduction not yet established |
| `erlang:spawn_opt/4` | Open revalidation task; current reproduction not yet established |
| `erlang:spawn_opt/5` | Open revalidation task; current reproduction not yet established |
| `erlang:spawn_request/2` | Open revalidation task; current reproduction not yet established |
| `erlang:spawn_request/3` | Open revalidation task; current reproduction not yet established |
| `erlang:spawn_request/4` | Open revalidation task; current reproduction not yet established |
| `erlang:spawn_request/5` | Open revalidation task; current reproduction not yet established |
| `erlang:system_flag/2` | Open revalidation task; current reproduction not yet established |
| `erlang:statistics/1` | Open revalidation task; current reproduction not yet established |
| `erlang:load_nif/2` | Open revalidation task; current reproduction not yet established |
| `proc_lib:spawn/2` | Open revalidation task; current reproduction not yet established |
| `proc_lib:spawn_link/2` | Open revalidation task; current reproduction not yet established |
| `proc_lib:spawn_opt/3` | Open revalidation task; current reproduction not yet established |
| `proc_lib:spawn_opt/5` | Open revalidation task; current reproduction not yet established |
| `proc_lib:spawn/1` | Open revalidation task; current reproduction not yet established |
| `proc_lib:spawn_link/1` | Open revalidation task; current reproduction not yet established |
| `proc_lib:spawn_opt/2` | Open revalidation task; current reproduction not yet established |
| `proc_lib:spawn_opt/4` | Open revalidation task; current reproduction not yet established |
| `proc_lib:start/5` | Open revalidation task; current reproduction not yet established |
| `proc_lib:start_link/5` | Open revalidation task; current reproduction not yet established |
| `proc_lib:start_monitor/5` | Open revalidation task; current reproduction not yet established |
| `proc_lib:initial_call/1` | Open revalidation task; current reproduction not yet established |
| `proc_lib:translate_initial_call/1` | Open revalidation task; current reproduction not yet established |
| `proc_lib:init_p/3` | Open revalidation task; current reproduction not yet established |
| `proc_lib:init_p/5` | Open revalidation task; current reproduction not yet established |
| `proc_lib:wake_up/3` | Open revalidation task; current reproduction not yet established |
| `proc_lib:hibernate/3` | Open revalidation task; current reproduction not yet established |
| `erts_internal:new_connection/1` | Open revalidation task; current reproduction not yet established |

## P1 — full agreed library API acceptance after the known-difference pass

Owner order is **revalidate/fix the known31, then fully validate all agreed library API against pinned OTP and correct newly found differences to acceptance**. This includes the559 Needs verification entries and relevant APIs outside default708; the registry is not a scope boundary. Inventory the agreed source exports/BIF/native/conditional/generated API and backend/platform applicability before estimating effort. No completion date is promised before that assessment.

Build an explicit source-derived coverage matrix by module/MFA, C#/BEAM backend and applicable OS, with normal/error/concurrency/resource/lifecycle partitions, complete value/error/stack/side-effect metadata, input/reproduction, current source/DLL hashes and pinned-original evidence. Inventory existing source/image and embedded snapshots first. Group dependent APIs by shared runtime/BIF/native/platform prerequisites; correct those dependencies before accepting their dependent library partitions. Preserve actual own-VM/unstable observations separately with source-backed analysis rather than pretending byte equality.

Acceptance requires all agreed mandatory partitions actually executed and supported by current proofs, with all discovered observable semantic differences fixed. A real blocker or unexecuted partition stays visible and prevents Fully validated; no waived cases, relaxed expectations, removed tests, hidden fallback or green-counter-only classification. Zero known31 alone is neither full-library acceptance nor whole OTP. Permanent differential regressions and affected/full/cold consumer gates follow the real dependency change. This section schedules work only: no new suite or implementation is run by the450 plan supplement.

## A1 — in-repository pinned original OTP snapshot

Already exists: selected original `.erl`/`.beam` files and `provenance.json` under `examples/HelloOtpLibraries/Otp`, additional source/image fixtures under `tests/Erlang.Tests/Resources`, and previous immutable validation evidence. These existing partial snapshots must be reused/audited, not duplicated blindly. They do not establish an audited complete self-contained closure for every loaded original module. A moving `../otp`, URL/latest reference, ignored artifact or download-only preparation is insufficient.

The four tracked fullsweep fixtures under `original/` are test cases, not proof of a source snapshot of the used OTP library modules. Existing embedded source/BEAM resources also require inventory/reuse; their existence alone does not establish complete snapshot acceptance.

Planned: keep the actually used original sources and required dependency closure **tracked in this repository**, pinned to OTP29.1.1/ad05823719d77c8faee87348ea39513d4e2f99c5. Record origin/upstream paths, applicable licenses/notices and SHA manifests. For every actually loaded BEAM choose tracked pinned image bytes or reproducible generation **from the tracked sources**, with pinned compiler/toolchain/options and loaded-image hash verification. Keep runtime/BIF/native dependency boundaries explicit.

Acceptance: a fresh checkout contains the source snapshot; drift/tamper is refused; reproducible compilation/load preserves exact code identity without reading a mutable upstream checkout. This full closure/build guarantee is planned, not credited by existing selected examples or450 tests.

## A2 — separate C# module libraries in this same repository

Already exists: `Erlang.Otp.Behaviours` and the current Runtime/Terms/Serialization/Compiler/MSBuild projects; explicit `RuntimeModuleSelection`, `RuntimeModuleBackend` and per-module startup ownership; `build/Erlang.Library.targets` and `ErlangLibrary` examples. These do not by themselves mean every C# module is already extracted into its own library.

Planned: define C# module library/project boundaries and runtime/native/host dependencies in **this repository**. Preserve existing implementations and reuse per-module selection/ErlangLibrary. One application/runtime may mix different BEAM and C# modules, while one module identity has exactly one explicitly selected pure backend. Refuse conflicting identities/dependencies deterministically; capture native stubs and provenance explicitly. No hidden fallback or implicit hot-state migration, and no promise that conflicting implementations of one identity execute simultaneously.

Acceptance: managed-only, BEAM-only and mixed consumers; conflict and transitive-dependency controls; API parity and cold PackageReference/package/publish validation. Extraction remains planned.

## A3 — incremental subsystem/project structure

Already exists: separate foundational projects, while many namespace-level source types remain directly in project roots. This supplement does not move them.

Planned: Root first records a target subsystem/subdirectory tree and dependency graph, then makes small reversible owned batches after the functional checkpoint. Update csproj/solution/imports/resources/generators, examples/documentation links and source guards for each actual batch. Preserve public namespaces, APIs, behavior, package contracts and alternatives; do not combine this work with speculative runtime changes.

Acceptance per real change: exact semantic delta0; build plus affected/full gates; clean/rebuild/incremental and cold PackageReference/publish; no cycles, lost resources or broken links. Update manifests only from the verified batch baseline. No bulk migration or project extraction is authorized by this documentation-only supplement.

## Owner order and boundaries

1. Finish450 publication/exact-head CI without interrupting its gates.
2. Root refreshes the31 named current reproductions/backend/source/DLL proofs, groups common dependencies and fixes observed semantic differences first. Helpers may audit isolated evidence; shared changes stay with Root.
3. Fully validate all agreed library API, including559 unverified and relevant APIs beyond default708, in source-derived backend/platform/dependency partitions; fix new differences to actual acceptance. Establish snapshot/oracle/build prerequisites as required by this dependency graph, without narrowing scope to the registry.
4. Complete the canonical pinned source/image closure and reproducible load/build contract by reusing existing tracked snapshots.
5. Design/extract C# module libraries with explicit mixed-backend dependencies.
6. Carry out the recorded subsystem layout in small verified batches after the relevant functional checkpoint.

These are queued obligations, not implementation or compatibility credit. Existing full-contract classifications remain unchanged until their own acceptance is met.

## A2/A3 prerequisite — source, assembly and test-boundary crosswalk

The original work.md section4 proposes26 src and10 test directories. Its exact qualifications are "Suggested structure" and "Adjust project boundaries when needed"; acceptance preserves justified library/test/dependency boundaries, not a mechanical requirement for36 projects. Current Erlang.Net.slnx has7 src projects and one executable test project, tests/Erlang.Tests. Additional tools, examples and benchmarks are separate and must not be miscounted as those test assemblies. Current Compiler references Otp.Behaviours and Runtime; the executable tests reference Compiler, Serialization, Otp.Behaviours and multiple tool/probe assemblies. These are current dependencies to assess, not proof of a cycle or missing functionality.

Before extraction or layout implementation, record a complete matrix: **expected section4 item → actual source path/assembly/test project → matched/merged/renamed/missing boundary → gap or explicit justification → proposed dependency and validation ownership**. Cover every suggested source and test item, including Core/Terms/PatternMatching, Runtime/Processes/Scheduling/Messaging/Distribution, Serialization/Bifs/Modules, Kernel/Stdlib/Sasl/Applications/Behaviours, compiler frontend/semantics/code generation, C# preprocessor/MSBuild/analyzers/tooling, Interop/Diagnostics, and all ten test categories. A missing named assembly is not evidence that its functionality is absent. Reuse actual implementation and test inventory; identify what is intentionally combined and what needs separation.

This crosswalk is mandatory planning input to A2/A3: preserve and justify library, test and dependency boundaries, CI selection, resources, generators and documentation coverage. Merely moving root-level files into folders does not satisfy it. Update the intended dependency graph and target layout against this matrix before small migration batches. The matrix and migration are not completed by this450 documentation supplement; no projects are moved or new functional tests run here.

## A4 — mandatory future scheduler architecture

Source read in full: D:/Distr/Work/Codex/Erlang.Net/mandatory scheduler architecture.md,1204 bytes, SHA256 A19FB1A4E5FF8EC994FA09F735BE64B30AA013ED61BD3FAADF700C3E2B4D463B. This future requirement follows the established P0→P1 priorities; it does not interrupt450 or start scheduler implementation now.

Implement one execution engine with three interchangeable strategies: CompatibleScheduler prioritizes Erlang/BEAM semantics, fairness, preemption, process priorities and correct signals; OptimizedScheduler uses CLR work stealing, JIT-friendly execution, efficient layouts, useful CPU affinity and fewer allocations without violating semantics; DeterministicScheduler controls ordering and reproducible scheduling decisions for deterministic concurrency tests. All share one process model, engine, mailboxes, signals and OTP implementation. Select at runtime without recompiling Erlang; compiler and C# preprocessor output must remain scheduler-independent. Do not duplicate runtimes or OTP, or remove existing alternatives.

Gap: existing managed scheduling controls do not establish these three strategies or their runtime selector. Plan the selector/dependency boundary and inventory existing scheduling behavior before implementation. Acceptance requires the same semantic suite for all three and independent per-mode benchmark results. Derive common workloads and conditions from work.md section17: spawning, throughput/latency, selective receive, supervision, timers, binaries, serialization and distribution; measure applicable allocation/GC/CPU/memory/latency/scaling under identical inputs, toolchain, environment and correctness gates. The scheduler specification sets no numerical threshold or concrete workload parameters; define and record a fair protocol before runs rather than inventing targets or speedup claims. Preserve per-mode correctness, performance and limitations separately. This is planned only, without scheduler edits, benchmark execution or compatibility credit.
