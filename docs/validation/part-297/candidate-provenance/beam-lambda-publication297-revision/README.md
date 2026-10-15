# BEAM lambda publication prerequisite correction

Source-only revision against `artifacts/root-nif-beam297-isolated/candidate`; no Main, build, runtime or Git changes by this worker. This retains root's explicit KeyValuePair fixture-constructor corrections. Apply all four files after verifying parent hashes. Existing NifBeamEntryHostChecks registration is extended; no new registration is needed.

The original blanket FunT rejection preceded image-owned BEAM closure publication. Part 297 now supplies exact prepared/standalone image ownership and captured-image leases. The replacement does not accept an arbitrary lambda table: BeamLambdaReader still validates native count/byte/atom/label bounds; BeamLinkedCodeReader verifies every declared target name, total arity, free-variable count and referenced creation/call operands. BeamChecksumReader still rejects incomplete trailing lambda bytes. PublicationMetadata now explicitly runs BeamRegisterValidation on **every declared lambda target**, including targets unreachable from exports. That validation retains recursive reachable local/closure checks, supported imports/opcodes, registers, receive plans and frame validation. The native-record rejection is unchanged. The obsolete blanket-rejection diagnostic is removed because it has no other references.

New assertions are added to the existing HOST without weakening any prior assertion:

- Change the genuine fixture's FunT free-variable count to exceed total arity; PrepareBeam must reject the malformed real byte input.
- Independently read a valid linked fixture, inject an unsupported instruction definition into a declared lambda and remove its exports in managed metadata. Direct PublicationMetadata.Read must reject that unexported lambda. This proves removal of the blanket guard did not accidentally leave unexported table entries unvalidated. The injection changes only an owned instruction reference, not a shared opcode definition or original bytes.

Existing genuine closure publication, named-entry dispatch, old/current, standalone, cancellation and pending-captured closure checks remain required. This correction was not built or run by this worker and makes no new native/original OTP parity claim.
