# CPU topology setting/state prototype - 2026-10-13

New isolated archive of published8475456, pinned to OTP-29.1.1/ad05823719d77c8faee87348ea39513d4e2f99c5. Original sources are read with git show; no main, sibling or private composite edits, commits, push or publication. Previous native prototypes and evidence remain unchanged.

## Source/reuse/notices

Altered C# adaptations of erts/preloaded/src/erlang.erl cput_e2i/set_cpu_topology (Copyright Ericsson AB1996-2026), erts/emulator/beam/erl_cpu_topology.c validation/sorting/set/get (Ericsson AB2010-2025) and bif.c selected dispatch (Ericsson AB1996-2026) and erl_bif_info.c query dispatch (Ericsson AB1999-2026). Apache-2.0; original full OTP license/notices retained. Consolidated adaptation notices live here, with no source banners or private LICENSE.local.txt in packages. Existing CpuTopologyFormatting, semantic topology levels, immutable terms, registry/process runtime and source fixture harness are reused. A general .NET topology/actor package cannot implement this exact Erlang counter/validation contract; no dependency/production BEAM is introduced.

## Supported selected behavior

Pure CpuTopologyConversion.ToInternal mirrors cput_e2i: source-level child-level decisions, node/processor counters carried from the last flattened CPU, core/thread resets and increment rules, top-level implicit processor, and empty info-list tuples. External logical IDs are integers0..65535. It does not invent physical CPU numbering. set_cpu_topology/1 converts, validates and atomically exchanges configured state, returning the previous USED topology in external form.

system_flag(internal_cpu_topology,Value) validates a nonempty proper list of seven-field cpu tuples (or undefined reset), with each internal field in -1..65535. It rejects duplicate logical IDs and duplicate processor/node/processor_node/core/thread entities, sorts processor/node/processor_node/core/thread exactly, then follows the original NUMA validation loop including its first-row behavior. Rejection leaves configuration unchanged. The result is the previous USED internal topology. Undefined clears user-defined topology. The topology is immutable and belongs to its ProcessRuntime; concurrent exchanges are linearizable under the topology-state lock.

system_info(cpu_topology), system_info({cpu_topology,used|defined|detected}) and system_info({internal_cpu_topology,used|defined|detected}) observe actual configured state. The CLR runtime has no native detected-topology provider, so detected is undefined and unconfigured used falls back to that unavailable detection. This is an explicit platform limitation, not physical/native discovery parity. All original-facing source fixtures capture previous used/defined/detected values and restore defined state in after, so they do not assume original hardware topology is undefined.

The selected SystemFlag/SystemInfo handlers expose TrySystemFlag/TrySystemInfo for the parent's combined dispatcher with seq_trace or other system keys. The standalone prototype registration rejects unsupported keys with badarg; this does not claim the whole export or module is complete. Deprecated system_flag(cpu_topology,...) trap/logger warning is not implemented. OS discovery, scheduler affinity/binding, logical processor counts, CPU group maps/callbacks and native topology-resource/rebinding effects are not implemented or reported as success. Changing configuration makes the actual supported topology queries change; no physical scheduler effect is claimed.

40 independent source fixtures cover conversion, processor/node/core/thread counter assignment, sorting, previous state, reset, boundary/error/type/order/uniqueness rules and query selectors. One host test checks pure conversion, independent runtime configuration and concurrent exchange uniqueness. All prior4186 source/outcome/generated hashes/context/policies/finite-byte contracts,132 diagnostics and six exclusions are guarded. Original source-built proof is pending the parent; SDK passing is separate from native proof.

## Integration

Narrow production delta: new topology entry/conversion/validation/state/registration/domain files; one per-runtime state property; existing CpuTopologyFormatting exposes its unchanged pure formatter; CoreModules invokes the selected registration. Parent must compose the selected Try handlers with seq_trace rather than overwriting either registration. Fixture/functional/host additions are appended after all inherited cohorts. No other runtime domains or compiler semantics change.
