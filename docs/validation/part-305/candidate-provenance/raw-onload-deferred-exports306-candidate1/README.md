# Raw on_load deferred exports: candidate 306

Artifact-only implementation on an exact snapshot of the 303 source, independent of the future 305 merge. Main and Git were not changed. The separate 305 exact-image HOST row must be preserved when merging this candidate's single Program addition. One HOST registration here includes all six cases and allocation controls; it does not add a second registration for those controls.

## Source-derived behavior

Pinned OTP `ad05823719d77c8faee87348ea39513d4e2f99c5`, `beam_bif_load.c:819-940`, raw false clears deferred export addresses while retaining the pending module. A later true promotes that image, and only remaining deferred addresses become global exports. `BeamOnLoadTicket.HasDeferredExports` starts true. Raw false permanently clears it; raw true publishes the same image and descriptor, but registers exports only while the latch is true. Repeated false does not discard pending code. Current old global bodies remain callable while pending. Existing saved local functions retain their exact images.

This does not implement wrapper false/pending purge, stage/fun fencing, a privileged VM purger, or a public NIF loader. High-level host on_load APIs remain a distinct surface.

## Actual original observations and resource projection

Six finals are extracted from the independently reviewed original9 archive: fresh/current/deleted_old crossed with direct_true/false_false_true. The original workflow `38734860285`, attempt1, artifact11973920550 remains historically FAILED because its initial guard rejected valid function stack frames. Repaired passive review of the same bytes passed; there was no original rerun. The canonical full report hash is `70FA85303FE66218A01A6CBB73B3F5553F73CED05B83DEEF4CDAE8E990D360B2`.

`expected-finals.json` retains full typed final fields, raw ETF hashes and run/pin/source-plan/report provenance. Its six fields were independently checked against lifecycle's separate projection. Full raw six ETFs, build provenance, receipt, metadata and source-plan are retained under `sources/`; the projection makes no SDK pass claim by itself. Original compiled `old.beam` and `new.beam` are copied byte-for-byte, and HOST verifies their hashes before use. Their bytes were never patched to make the six positive cases pass.

The HOST uses real `PrepareBeam`, raw loader MFAs, the actual on_load interpreter, the actual pending local fun stored by `erlang:put/2` in the dictionary, and the old fun captured before deletion. It compares loaded/old/export state, the export set, returned atoms or exception class/reason, and both local functions. Full physical stack equality and export ordering are deliberately not asserted. Direct true publishes exports; false/false/true leaves the image loaded with zero exports and global body `error:undef`; pending fun still returns `new_body`, and an existing old fun returns `old_body`.

The explicit fixture-only `os:getenv/1` registration returns false for the precise fixture key, taking the same non-`not_ok` branch. It is not an implementation or native parity claim for the OS module or environment values. The BEAM import whitelist permits exactly this import and real existing `erlang:put/2`, then delegates to ordinary ModuleRegistry.Call; no default getenv body is installed. A missing runtime registration therefore remains ordinary undef.

## Bounded prerequisites and malformed controls

The exact new binary initially failed before semantic assertions because `allocate_heap` was absent from the bounded linker/interpreter. Six existing production files now recognize it as allocation of stack slots with live register count from operand2. The heap descriptor uses the existing allocation decoder; CLR allocation is not presented as ERTS physical heap accounting. Stack-frame validation, zero-frame control flow and register liveness participate in the same checks as allocate. Unknown allocation tags, unsigned operand requirements and bounds remain enforced.

Pinned `beam_file.c` marshal_allocation_list explicitly sums repeated kinds, so duplicates are retained, not invented as malformed. Five negative controls reject signed stack/live operands, too many allocation entries, unknown allocation kind, and signed allocation count. A positive duplicate-kind control preserves the native summation behavior. These controls mutate copies only, separately from the six original positive inputs.

`sources/pinned-source-bindings.json` binds the exact full OTP loader/ops/raw-finalization source blobs. Two prerequisite failures (unsupported allocate_heap and external import admission) and their original receipts are retained and separately classified as **not semantic red**. Once both prerequisites were supported, actual semantic red passed fresh_direct_true and failed fresh_false_false_true at the exported-body assertion. Only then was the deferred-export latch changed.

## Legacy coverage and limitations

The single incorrect final-global-body expectation in BeamLoaderDirectChecks now requires absent export and `error:undef` after false/false/true. Its earlier assertions that the old body remains callable while pending are preserved. Its source/generated/native-BEAM-wrapper variants remain covered; those wrappers are managed execution of original compiled BEAM, not native ABI execution.

Adjacent future NIF ownership limitation: publishing zero exports leaves no registered source owner and the current BEAM descriptor has null SourceOwner. A captured pending local fun still invokes correctly, but a later internal NIF reservation from that zero-export current image would currently fail OwnerMismatch. No public load_nif is activated; fixing independent image-owner association is a separate prerequisite before activation. This candidate does not claim all NIF caller ownership routes are complete.

Final validation details and exact parent/result hashes are in validation-result.json and promotion-map.json. New resources use an explicitly owned EmbeddedResource folder rather than changing the resource closure of older proof fixtures. Native source-plan input count remains unchanged by these edits because every production file modified here already exists; 305's two new runtime partials remain the only new production files in the combined increment.
