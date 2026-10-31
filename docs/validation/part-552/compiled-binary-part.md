# Compiled binary_part/2 entry correction

The original OTP compiler emits `gc_bif2` for `erlang:binary_part/2`. Our register backend previously accepted only arithmetic imports in that instruction. The narrow correction admits this existing native primitive and invokes it through the ordinary internal guest bridge. Arithmetic imports keep their existing allowlist and dispatch. Both input registers are read before writing the destination; existing failure-label and exception handling is retained.

The interpreter also uses the existing `ExecutionCheckpoint` at its instruction boundary, preserving the reduction charge while resuming the real actor before ownership-sensitive work. This predecessor is explicitly included in the scoped source and its validation.

## Executed scope

Root built a source archive of published commit `7f098d4dc2590b35e8c4b872e9e24663968ba22a` plus exactly nine scoped source/tool targets. An independent byte audit verified all3,619 unchanged archive files and all nine overlay postimages. The native bridge remains baseline ABI8 in this isolated delivery; no unpublished printf, mutex, literal/token or other GC scaffolding is required. Core and backend build with zero warnings/errors.

Fresh admitted OTP-29.1.1 (`ad05823719d77c8faee87348ea39513d4e2f99c5`) compiles the worker and proves all six opcode gates. All38 complete results match in both ordinary and private-owned runtimes as raw AND deterministic ETF, including complete failure reasons, stacks, error_info and source metadata. Both runtimes close, all owned child processes settle and source/provider fingerprints remain unchanged. The actual source-entry chain is the same original `capture/0 -> spawn_monitor(Module,entry,[Parent])` in both engines; no stack projection or result normalization is used.

The38 cases comprise30 binary_part cases and eight arithmetic controls. They cover body/guard failure labels, both destination aliases, live registers, valid/invalid boundaries, operand types, catch/try and arithmetic failures. This is a finite compiled-entry contract, not full binary_part compatibility. Registered/full API deltas are0/0.

Local raw evidence is retained under `artifacts/statistics-full/gc818-scoped-head24` and `artifacts/gc-binary818/scoped-head24-01`. The English changelog records initial failed restore attempts separately; explicitly serial restore succeeded. Reproduction and exact required gates are documented in [the observer](../../../tools/Compiler/Erlang.GcBinary818Observations/README.md).

The broader active checkout separately passes53/53 affected tests after Linux mutex integration, but that result is not substituted for execution of this isolated source. Exact publication-head GitHub CI, remaining whole-API boundaries and broader physical GC acceptance are separate open gates. The existing published general CI failure is not resolved or hidden by this correction.


## Independent delivery on main

The same narrow changes were surgically ported to actual remote main d49963520e363a4db7664ccc7e7c557d3bde8421, preserving its unrelated Core state. Main-based Core/backend build passes zero warnings/errors in43.06s. Fresh original compilation again proves6/6 gates and all38 full raw/deterministic outcomes match BOTH profiles with runtime/process/source/provider cleanup. Eight unchanged existing opcode controls separately pass on these main-based DLLs;64 enrolled source/DLL inputs remain unchanged. Local evidence: artifacts/statistics-full/gc818-main25 and artifacts/gc-binary818/main25-01. This source commit excludes the unpublished experimental GC/native branch. Metadata closure and exact-head CI remain separate gates.
