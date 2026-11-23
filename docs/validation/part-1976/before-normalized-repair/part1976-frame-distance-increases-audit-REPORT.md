# Part 1976 frame distance regression

The sealed metric sums absolute width differences against pinned OTP across allocations paired in physical order. Both failures have one allocation in every image. Paired BEAM disassembly reran that metric directly.

| Function | Pinned / Part 1975 / Part 1976 width | Frame distance |
| --- | --- | --- |
| crashdump_viewer:find_truncated_proc/1 | 1 / 1 / 2 | 0 → 1 |
| ets:count_mandatory/1 | 0 / 2 / 3 | 2 → 3 |

The same release functions both acquired a Y-backed select_val in the final compiler. The previous images perform atom selection in X. In find_truncated_proc, the old compiler still allocates before selection, and the pinned compiler puts the frame in the miss branch. In count_mandatory, the old compiler already uses two slots, while pinned selects on X before placing a zero-slot frame in the recursive returning branch. These are existing mismatches beyond this patch.

The new early TryEmitAtomGuardAlternatives directly selects from the source binding, which is a Y projection for each affected function-clause head. The previous exact comparisons allowed projection/register planning to consume the guard and release its slot. The final CombineAtomMembershipTests (multiKey: true) then folds the X comparison chain into a table, so the new early Y table adds a slot to functions which already formed the desired table. The sealed fixture reproduces both physical changes using Part 1975 compiler 3BCE5198E9713D66FF83A88D0115B77675CF09CE87ABED4D31956B50E028E13C and Part 1976 compiler 784C93E6500E67A1E8044EDA282783F0190D3BA37D86D7C3A852FA227B84E9C3. The pinned tag emits the expected comparison baseline. Source points are OTP 29.1.1 crashdump_viewer.erl at 1039, ets.erl at 3317 and beam_ssa_pre_codegen.erl place_frames at 1433.

The staged correction keeps original comparison branches for generic framed function entry and the grouped tuple framed fallback. All recursive pieces of such guards inherit the restriction. Body guards continue using the new atom table, and specialized register-based emission and the existing final atom membership fold continue to run. The permanent structural check exercises both source probes, requiring the previous allocation width and an X selector. The existing projected body-if, source line and compound/numeric negative checks continue to exercise the newly introduced mechanism.

Strict replay and the touched-source hashes were checked on the sealed fixture-profiles identities. The exact previous and current CLI images and pinned erlc compiled only the independent source probe for diagnosis. The staged correction still requires a build, permanent red/green and corpus measurement before asserting restored widths or gains. The production checkout and pinned checkout were untouched.

- [Three-way release listing](listings.raw), [distance output](paired.stdout)
- [Source probe](frame_distance_guard_probe.erl), [three-way fixture listing](probe-listings.raw), [fixture distance output](probe-paired.stdout)
- [Production patch](entry-guard-preserve-frame-production.patch), [permanent check](entry-guard-preserve-frame-test.patch), [source identities](entry-guard-preserve-frame-preimages.json)

