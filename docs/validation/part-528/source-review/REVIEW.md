# Part528 source preservation review

Reviewed current w518repair production sources against main baseline c2a4ff547d14ced76dec802bcfac41d14fdd6225. Exact per-target preimage/current digests in review.json. Read-only source review; no build, native/VM execution, tests, source changes or git mutation.

**No concrete blocking source defect found in this bounded review.** This does not certify all180previously verified contracts; final regression and exact-head CI remain necessary.

## Default versus private profile

* Default ordinary make_ref still constructs portable immutable words and reconstructs the same FullId. The added marker is internal provenance, not wire data or equality. Public constructors/decoder do not gain marker; exact base type and same stable LocalNodeIdentity gate the private slab representation. Alias/resource/PID-derived cases retain their previous domains. Node publication continuity keeps the stable identity semantics.
* Only private guest adoption/import turns eligible ordinary refs into actual3cell storage. Getter reads header/raw data from the active owning block; copy/export reconstructs a portable reference before host or cross-owner escape. Foreign-owned and stale storage is checked before reading. Existing root retention, construction publication rollback and physical retirement remain authoritative; no public heap budget is enabled.
* Loader-map correction also intentionally affects ordinary execution: source-proven dynamic literal key tuples now belong to the exact linked BEAM image. Capture accepts empty literal map source, flat-sized unique literal keys and map association; register/dynamic keys and unsupported cases retain existing construction. Registration occurs after validation; equal runtime/static tuples receive no marker. Weak exact-instance/image tables and code-held tuples retain provenance without retaining a ProcessRuntime through reference payloads.
* Owned flat-map rows accept either exact loader tuple Items or actual local owned sequence. The new NativeMapOwnedRoots510 ordinary-map branch includes FlatKeys itself, allowing foreign/stale/unadmitted hidden tuple children to be checked/rooted. Enumeration performs immutable graph/CWT reads without runtime/native-owner acquisition under heap gate. Existing native physical-map/HAMT traversal is unchanged.
* Allocation567 is private observation only: occupied header+raw+pointer cells are separately counted, checked before collection retirement. Existing PayloadWords fields and Collect() return retain meanings. Generations, survivor/old policy, default options, public process_info/system_flag/statistics and collector routing are not enabled/reinterpreted.

## Prior180 contracts

The current supported-MFA and compatibility-ledger JSON definitions are identical to the reviewed main baseline (supported-MFA working-tree CRLF differs from Git LF only; ledger raw bytes match):180Verified compatible/504Needs verification/24Known differences remain. No registry/primitive registration, parser/error stack formation, syscall/native loading, process_info/default order, scheduler flag, GC routing or source compiler implementation is altered by this production delta. The shared term/equality/serialization surface is affected indirectly through ReferenceTerm getters and map loader identity, so source similarity alone cannot prove all prior180. Existing whole value/full stack/failure controls must execute on final candidate sources.

## Acceptance and remaining limits

Root reported9existing controls and unchanged original Reference5631111byte default+deterministic parity in both profiles; those reports are context, not independent execution receipts from this reviewer. Required completion gates: final extra hidden foreign/stale/unadmitted flat-key control, new genuine two-module loader-map complete report under both profiles, affected logical/resource/BEAM/literal/serialization/host-escape/negative-role regressions, source-plan seals and exact-head CI.

The corrected ordinary-ref domain is not all references: public/ETF/external/resource/alias/magic cases remain explicitly root-only or separate. Loader key lifetime covers selected linked-code lifetime; whole code-purge literal migration remains separate. Full function/binary/native/message/ETS/stack ownership, automatic/default physical GC, min/max actual budgets, global GC events and full NIF lifecycle/ABI remain open. None of the24Known may be promoted by this review. No original output or comparison criterion was rewritten.
