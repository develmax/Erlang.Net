# Line-aware frame compaction and proper source stack facts

The frozen compiler preserves a live arithmetic X operand across
Allocate and AllocateHeap, clearing facts about Y at the new frame.
It recognizes the X-only failure relay while preserving its ordinary
Line. A fixed-point source analysis proves a proper-list value at a
private entry and specializes the corresponding unique failure to [].

All 24 declared managed checks pass. Both compiled native fixtures
agree on values and failure reasons. These include improper tails,
escaped calls, integer and float replacement and badarith. The fresh
48-path selection at two workers takes 348.60 seconds. It reports
5 Equal, 43 Different, 0 Unsupported and 0 SourceRejected. All 39
modified files prepare in ERTS and no raw file status changes.

The widened success frames from Part 1866 are restored exactly in all
231 measured functions. None moves farther in that original affected
cohort. This recovers their Part 1865 widths. The candidate produces
the pinned badmatch [] operand in all 133 measured generated Yecc
functions without reading reference image bytes as a compilation input.

Compared with the last full compiler, seven bodies gain normalized
equality and none lose it. Six frame distances increase. Four return
to earlier Part 1862 allocation sizes after the copy guard. Two
generated functions gain a slot in multiple branch frames after the
type substitution. These last differences are under test with the
substitution moved after final frame compaction. Four frame distances
move closer. No complete raw chunk gains or losses are measured.

[Focused tests](focused.json) · [Release scope](pilot.json)
· [Frame widths and operand facts](list-schedules.terms)
· [Frame stage attribution](frame-epochs-widths.terms)
· [Native fixture checks](native-fixtures/receipt.json).

