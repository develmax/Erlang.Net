# Constructor motion guards and numeric record replacement facts

The isolated compiler requires an actual list or tuple construction in
the new early copy window. The older adjacent literal copy remains
eligible. The same-block numeric analysis tracks successful arithmetic
values into record replacement operands, using existing live-root and
register transfer definitions.

The nine selected copy witnesses report 0 Equal, 9 Different,
0 Unsupported and 0 SourceRejected. All eight changed images prepare
in ERTS. All six lost tail-wrapper bodies recover normalized equality.
The two frame-distance increases in the Part 1865 full report return
to their pinned widths. The same early-copy restriction restores the
older two-slot frame in snmp_view_based_acm_mib:externalize_get/3.
Part 1865 reached one slot there by moving a copy after constructing a
closure. The pinned schedule uses one slot. The affected older
schedule and one remaining frame delta stay visible.

The exact package builds and passes 21 of 22 selected cases. The record
test expects TestHeap. The generated positive uses AllocateHeap after
arithmetic, retaining the producer in its three live X registers.
The first numeric rule loses the fact across that normal collection
boundary. The correction follows the pinned validator's live-prefix and
new-Y-frame semantics. All four original negative expressions remain
untyped in the measured source shapes. Native fixture results are
retained separately from the failed managed check.

A separate four-arm task uses a single compiler, the same seven release
paths, separate scratch and output directories, equal host processor
settings and one, two, two, one corpus workers. Every generated file
hash agrees. Median wall time is 85.32 seconds at one worker and 67.48
seconds with two workers, a 20.91 percent decrease for this selection.
The next selected check uses a two-worker limit. A complete corpus
continues to use the four independently balanced lanes.

The selected package remains staged. The complete Windows tally
remains 409 Equal and 957 Different from Part 1865.

[Focused result](focused.json) · [Six recovered function bodies](correction-witnesses/pilot-summary.terms)
· [Frame and source epochs](correction-witnesses/externalize-get.terms)
· [Resolved allocation witness](record-native/candidate.terms)
· [Worker measurement](worker-calibration/receipt.json).

