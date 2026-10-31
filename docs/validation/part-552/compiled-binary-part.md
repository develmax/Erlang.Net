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


Publication metadata: the unchanged main13 checker passes13/13 on the final scoped source and metadata, with isolated Git membership. The mandatory Linux workflow reuses its existing pinned original build and compares this38-row contract in both profiles independently of native-bridge readiness; its aggregate requires whole raw/deterministic equality, complete runtime closure and all six actual compiler gates. Existing NIF gates and raw artifacts remain required. Linux/GitHub execution is pending until the exact published head finishes.


Windows checkout correction: the initial CI preflight failure is retained. Two scoped attributes rules preserve the new observer and current descriptor's exact bytes. Actual checkout-index with core.autocrlf=true verifies25 paths, and the unchanged13-plan checker passes on those real checkout bytes. No product source or hash-mode weakening is involved; exact new-head CI is separate.


## Readiness correction after actual Windows full run

Exact f545 Windows executes9451/9451 tests and original configured catalog1321/1321, then readiness rejects stale current source bindings for BeamRegisterInterpreter.cs and BeamRegisterValidation.cs. The independent Linux run passes38 complete rows in both profiles and retains84 native/8 module-text/68 legacy/pthread/3-loader/cleanup gates. Preserve the Windows failure; no green CI/merge is claimed.

Audit all4373 source/evidence rows against2398 exact Git blobs: only these two current-source hashes differ. Update the two bindings in both current ledger and readiness output, preserving every Entry/classification/evidence and all other source rows. The six unchanged production MfaCompatibility classes run against the actual main-based ModuleRegistry/Core/Otp exports and all original evidence files:708 registered,2226 source bindings,180 verified,504 pending,24 known PASS. Initial diagnostic wrapper used uppercase baseline JSON keys; retain its failed run and close only confirmed own PID39140 before correcting lowercase keys. Corrected host build has zero warnings/errors and execution02 exits0. No production source change or compatibility promotion. New exact-head CI remains mandatory.


## Final both-inventory early gate and actual Windows proof

007d again passes9451/9451 but fails the separate native infrastructure readiness validator: its MapFamily plan and Backend Program SourceBindings remain stale. Preserve the failed result and the preceding MFA-only local coverage gap. Refresh exactly those two native fields; all statuses/milestones/evidence/historical data remain unchanged. Both unchanged production validators now pass with actual708 exports/MFA2226source rows and native inventory.

The new early checker verifies4714 current raw source/evidence rows before SDK and leaves full readiness mandatory. Its real filesystem sensitivity reproduces exactly the two native defects in0.328s, while corrected files pass. The initial nine-file proposal is rejected before publication for missing Windows RAW protection. The ten-file successor adds one exact -text rule and reseals the acyclic actual dependency graph with original modes. Actual Windows checkout22path proof, actual13/13 preflight,4714/4714 early gate and both production validators pass on real Windows bytes. Runtime/src/bridge/worker files remain unchanged. New exact-head CI/review/merge remain mandatory; this is not whole binary_part/native/GC promotion. See changelog0095 for retained preparation failures and scope.


Completed007d also exposes a stale actual monitor CLI selector, separate from early input hashes. Use the current sealed169-row path/SHA from inputs.json rather than the historical hardcoded freeze, keep collector and historical data unchanged, and reseal workflow dependencies. Actual normal candidate test build0warnings/errors and fresh monitor Original/.NET10wholeRAW streams pass with five managed cleanup families/source+DLL integrity. Full actual Tool Readiness passes using the digest-verified CI9451/9451 report; not a new local full run. Prepared prior c2fe is retained/unpublished. New exact-head CI still required.
