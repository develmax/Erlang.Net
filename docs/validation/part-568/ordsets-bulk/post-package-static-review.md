# Additional static review after package freeze

2026-11-03. No additional build, runtime probe, source overlay change or readiness promotion. The frozen review ZIP remains unchanged.

Pinned `erl_map.c:858-918` copies a collision region into its collision node in original compacted array order. The proposal's five-argument `HamtCursorView.Branch(..., preserveCollisionOrder: true)` propagates the flag at every recursive level and keeps the sequence at the collision level. Ordinary four-argument callers retain their previous behavior. This closes the static question of whether an additional collision-key sort was intended by the pinned bulk builder; it does not replace a physical sequence execution test.

Pinned `erl_map.c:924` and following construct ordinary HAMT branches from descending swizzled hashes using a stack; ascending slot traversal must be tested independently against the proposal's ascending low-nibble groups. Only the collision-region copy order was established by this review.

The flat `MapTerm.FlatPairs` implementation reconstructs occurrences using reference-keyed queues from `ArithmeticKeyEntries`, rather than `TryGet`. It therefore does not inherently collapse distinct values for repeated references. This is a static observation; value-and-reference sequence preservation through both sorts and detached copies still requires execution checks.

An additional source gate remains at `OtpMapHashLayoutProtocol.SortedFlatEntries`: when no saved native layout is attached, its `FlatEntries` overloads sort `ArithmeticKeyEntries` again instead of reading `FlatPairs`. A bulk result of at most 32 occurrences currently uses `FromSourceFlatPairs` without a saved native layout. Under non-total fun comparison, a repeated sort can in principle alter the source's partially validated physical sequence. The all-comparison-zero count/lookup fixture does not distinguish that behavior. This is an unverified producer-to-cursor/hash transfer risk, not a newly reproduced failure. Before changing the fallback, review ordinary atom identity/provider ordering and all existing callers; alternatively preserve source flat layout through the established layout mechanism. Required controls must inspect actual `maps:to_list`, iterator, internal hash and copied physical key/value occurrence sequences, including int-min comparison triples and early validation failure.

This observation is outside the frozen 23-source ZIP and must be carried into the next agreed controls/integration stage. No speculative production fix was made under the closed runtime window.
