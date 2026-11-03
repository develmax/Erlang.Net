# Loader map hidden-key custody: bounded source review

State: READ_ONLY_SOURCE_REVIEW, no build, VM, original acquisition or active mutation. Review target w518repair Part528, frozen snapshot hashes in manifest.json. The snapshot already contains the parent's proposed NativeMapOwnedRoots510 fallback change.

## Conclusion

The proposed narrow change is the correct location and no new blocker was identified within this review. A flat map's key tuple is an actual child of its physical map representation. The ordinary-map fallback omitted that child while the materialized/native branch included it. ProcessTermConstruction514 initializes its exact-identity admitted set through this graph helper; later map publication checks the actual map root payload, whose first child is FlatKeys426. A genuine loader-owned tuple is deliberately not copied, so no newly constructed tuple admission can incidentally conceal the omission. Adding the real input child repairs the root inventory rather than bypassing construction capability checks.

Keep only this split:

1. Non-map: existing DebugDirtyCopyFrame424 child enumeration, unchanged.
2. Map with no NativeMapMaterialization505 layout: existing keys/values plus its exact FlatKeys426 when present.
3. Materialized/native map: existing complete rows/tree/stored-node/FlatKeys enumeration, unchanged.

Do not modify DebugDirtyCopyFrame424's copy schema, public literal predicates, Contains, IsOwnedLocal, or numeric size reporters. Do not globally exempt shared literals or compare tuple values to mint custody. A broad dirty-copy schema change would affect separate copy/protocol consumers unnecessarily.

## Ownership, lock and failure reasoning

- The new branch reads immutable map metadata and the exact tuple reference only. NativeMapMaterialization505.TryRead(map,null) is a CWT query; null runtime short-circuits Matches. It does not hash keys, look up values, request runtime/owner gates, or invoke guest callbacks.
- ValidateLocalGraph checks Storage and IsOwnedLocal for each popped object before enumerating its children. A hidden foreign or stale owned tuple therefore fails before reading its Items or taking a foreign heap gate. Appending the tuple to the returned children makes the LIFO walk examine it before the existing keys/values.
- Construction admission remains reference-based. The tuple becomes admitted because it is a genuine source child retained by BeginConstruction514, not because it is a literal or equal to an admitted object. A forged/equal tuple outside that source graph remains unadmitted.
- ProcessBinaryRegistry512 and GC already use exact-identity seen sets. Reaching keys through both Entries and the tuple adds a real container visit, not duplicate backing leases. The binary registry deduplicates backing identity as well. The map root and exact tuple remain under the same already-registered input lease while construction completes.
- Existing dynamic flat maps now also expose their real key tuple to validation/GC/custody. Their private copies remain process-owned, while only exact loader-admitted tuples use the565 global view. The change does not mark any object as literal, change atom/key ordering, mutate map bindings or suppress duplicate physical rows.
- Allocation failure during temporary child-array creation occurs before admission/GC publication; existing cleanup/collection transaction rules apply. No provisional key tuple is published merely by this enumeration. Existing materialized/native traversal and release ordering are untouched.

## Required finite controls

1. Existing565 PhysicalCustodyAndExport must pass on actual source: key tuple is ReferenceEquals to the admitted loader tuple, has no OwnedStorage506, actual map body5 words for two immediate values; dynamic equal map still owns its tuple/body8 words. Clear plus collection releases all process words; exported bindings survive retirement. The initial failure remains preserved evidence.
2. Pure graph visibility: an ordinary nonempty flat map without a materialization layout returns the exact FlatKeys object once as a direct child, plus all key/value rows. Empty map, HAMT without a flat tuple, non-map cons/tuple and materialized-map direct-child identities preserve their intended branches.
3. Hidden foreign tuple refusal: allocate a two-atom tuple in heapB, build an ordinary map whose internal copiedFlatKeys is that exact tuple and whose entries reuse its atom key objects, then attempt Validate/Adopt/Set in heapA. Require ForeignRoot before any cross-heap tuple enumeration, zero newA blocks, unchanged B roots; retire both independently. Repeat with staleB tuple after B collection and require disposed/fail-closed behavior. This is an internal storage-seam negative, not a supported public producer.
4. Capability negative: start construction from mapA, then attempt publication of a map containing a different equal portable key tuple or a separate unadmitted child. Contains must still reject the extra tuple; no literal-equality exemption. If tupleB is separately present in the actual input graph, normal admission may legitimately succeed.
5. Loader authority negative: a tuple admitted by runtimeA/codeA is not automatically loader-admitted in runtimeB. B may adopt it as a portable tuple through its normal own-storage path; it must not inherit A's marker solely by value or object transfer. Equal static LitT tuple and equal runtime-created tuple remain unmarked.
6. Backing lifetime: ordinary map with binary-valued or binary-keyed content retains one backing identity despite the duplicate graph path; release source roots, perform GC while destination remains rooted, verify bytes, then release destination and prove cleanup. Existing binary construction/reallocation controls must retain their linear complexity bounds; exact visit counters may legitimately reflect the newly visible real tuple and need honest witness updates rather than a relaxed asymptotic bound.
7. Retain materialized malformed-map/duplicate reentry and hidden HAMT node GC controls: full physical row values and native saved layout branch must remain unchanged. No new fabricated original output is needed.

## Required original/regression gates

Re-run unchanged Reference563 whole default and deterministic ETF in both managed profiles against the admitted pinned original; the two +3 shared-size leaves should be repaired through actual storage identity. Keep the original failed1111-byte streams, no normalization or edited golden. Separately acquire and execute the565 two-module source fixture to distinguish syntactically literal versus dynamic empty source/key operands and same-image versus different-image tuple identity. Run affected map payload/shared-HAMT, binary/512 custody, construction514 linearity, root GC, numeric/reference and native map-reentry regressions on final sources. No full API or whole-runtime compatibility promotion follows from this bounded source review.
