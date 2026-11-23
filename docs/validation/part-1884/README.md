# Case failure locations, local try tail calls and finite integer ranges

All 49 selected cases of 9,845 pass. The first 48/49 run found an
existing matched call projection test failure. Source lines inserted
before CaseEnd were eliminated later, but their presence before the
tagged branch rewrite hid its exact terminal error. The retake
accepts ordinary intervening lines in that proof and retains the
separate tuple and tag tests. The original fixture is unchanged.
Exact installed and generated disassemblies from the failed run
remain available to reproduce the scheduling defect.

The uncached 128-file pilot takes 904.14 seconds with 7 Equal,
121 Different, 0 Unsupported and 0 SourceRejected. It overlaps
a separate full release build. All 120 changed outputs prepare
on pinned ERTS. Two raw Line sections gain equality and none are
lost against the exact full Part 1883 parent. No normalized or
whole-file status changes are observed in the selected modules.

The real emitted case fixture matches seven OTP values or errors,
including the exact top-frame file and line for direct, multiline,
nested and source-mapped failures. The prior native fixture exposes
five actual location differences. The structural audit finds 6,562
matching-position failure candidates in 4,961 functions across 714
Part 1875 modules. Only 14 selected modules represent this group in
the current executable pilot.

A protected local fallback uses the existing tail call path after
the protected route has ended. The matching pinned ERTS fixture
checks ten returns or errors and the cleanup effect. A separate
finite range proof tracks successful bounded masks, shifts and
additions through allowed transfers. Native fixture execution matches
20 values or error reasons. Actual emitted mnesia_frag_hash matches
24 values or reasons and the actual Diameter image loads.

[Source identity](source-freeze.json) ·
[Scoped release report](pilot.json) · [Exact direct delta](compare-1883/) ·
[Original failed fixture](previous-beam-shape/focused.log) ·
[Case signals](native-case-locations/receipt.json) ·
[Terminal try](native-terminal-try/receipt.json) ·
[Finite range](native-finite-ranges/receipt.json).

