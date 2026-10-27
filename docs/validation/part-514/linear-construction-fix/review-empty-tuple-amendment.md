# Amendment: nested empty-tuple custody defect

This amendment supersedes the earlier no-blocker conclusion for the reviewed pre-fix514 source. The parent identified a reachable input omitted from that bounded review; this review independently confirms it from actual constructors. No execution or active source edit was performed by this reviewer.

P1: Nested empty tuples are rebuilt as fresh portable wrappers and fail enclosing-node custody validation.

`Term.Tuple(params Term[]) => new(items)` creates a new TupleTerm each call. Its constructor shares GlobalEmptyStorage428 for zero cells, but the TupleTerm wrapper itself is distinct. `ProcessTermHeap506.CreateTuple(empty)` calls Term.Tuple(). During nested adoption, empty tuple Source is not owned and is still TupleTerm, so it passes neither existing base case. Its Built array is empty, CreateTuple creates a distinct wrapper, and PublishConstructedNode511 returns immediately because it has no owned payload. The source-root inventory contains the original wrapper only. An enclosing owned cons or tuple then tests the fresh child's reference identity against custody.Contains and throws UnadmittedChild.

Finite actual reproduction supplied by parent: `Term.Tuple(Term.Tuple(), Term.List(Term.Tuple()))` passed to AdoptGuestInto507. This requires no opaque callback or reflective mutation; it is an ordinary portable graph accepted by the public term constructors.

Minimal source correction: add `frame.Source is TupleTerm { Items.Count: 0 }` to AdoptGuest507's existing reuse/base case. That preserves the original source-rooted empty wrapper, avoids allocating a zero-cell identity that is not admitted, and introduces no new payload/backing. Root-level empty tuples already use the analogous fast path in AdoptGuestInto507. Admitting arbitrary non-owned wrappers in Publish would widen capability unnecessarily.

Required acceptance: preserve the existing aggregated scaling bounds/observations and three controls; add the actual nested-empty test, retain its red stack and then green output after this one-condition change. Prefer additionally assert nested empty source aliases are reused to prevent regression. Final source hashes must be re-bound after integration; the earlier snapshot binds the defective pre-fix bytes and remains historical evidence.
