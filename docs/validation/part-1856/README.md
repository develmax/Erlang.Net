# Independent lowering checks for the next linked compiler

The source starts with the frozen Part 1855 snapshot against
Windows OTP 29.1.1 at ad05823719d77c8faee87348ea39513d4e2f99c5.
The latest uncached whole release result remains 408 Equal,
958 Different, 0 Unsupported and 0 SourceRejected. The independent
scopes below overlap. Their measurements belong to their own
compiler identities and exact case lists.

| Cause | Selected result | Instruction or lifetime result | Native check |
| --- | --- | --- | --- |
| Heap placement before an in-place tuple projection in a rooted X register. | 5 Equal, 7 Different, no whole-file changes. | Seven normalized function gains and no losses. | All seven changed images prepare and the SSH and EDoc observations agree. |
| Two checked entry projections before the first consuming predicate whose branches own different non-tail calls. | 3 Equal, 6 Different, no whole-file changes. | Sixteen opcode-prefix gains, no full normalized change. The final pending-tail proof preserves the complete ct_snmp candidate image. | Twelve fixture runs agree and the four changed module images load with equal exports. |
| Materializing the named recursive function as a value only when it escapes direct call position. | 1 Equal, 8 Different, no whole-file changes. | Eleven redundant inner fun creations removed in five modules. Direct call counts, helper inventory and FunT identity shape stay stable. | All five changed images prepare and ten fixture outcomes agree. |
| Ordering proven literal arguments with the preceding call result, only when all outgoing registers are covered. | 3 Equal, 9 Different, no whole-file changes in 49.35 s. | Eighteen complete argument windows gain exact disassembled equality in ten functions. No complete normalized or section losses. | All nine changed images prepare. The Mnesia supervisor returns identical flags and nine complete child specifications. |
| Constructing a pure cons head tuple after the ordinary call that produced the tail. | 2 Equal, 6 Different, no whole-file changes. | One complete normalized gain in tprof, closing the Part 1855 frame distance for that function. | Native tprof updates and deduplicates the actual trace map in the same order. Eight fixture observations and the error source line agree. |
| Copying a bounded terminal tuple continuation into the returning predecessors of a case. | 5 Equal, 3 Different, no whole-file changes in 19.34 s. | Four raw disassembled body gains and twenty-five after normalizing label IDs. No losses. tr_MId/2 matches in all three Megaco transformers. | Actual Megaco calls preserve results and error stacks. A non-returning tail edge omits its source caller and a callee that can return preserves it. |

Each scope has a permanent regression and a pinned source rationale.
The original argument-order test selected shapes that already matched.
An earlier isolated build also skipped its patch because the scratch
directory belonged to a different Git worktree prefix. The nested
fixture reproduces the real mismatch and the fixed isolated build
checks the new source before compilation. The final fixture is
red on the original compiler and green on the candidate.

The 46-image linked pilot retains 8 Equal, 38 Different,
0 Unsupported and 0 SourceRejected in 135.40 seconds. All
32 changed images prepare in pinned ERTS. Thirty-three of the
129 changed function bodies gain normalized equality and none
lose it. Two comparable frame profiles move closer and none
move further away. Raw section equality stays stable. The
Mnesia and tprof execution traces match both the Part 1855
and official images. The linked Megaco scope also returns
matching values and case_clause terms.

The initial 43/44 focused run exposes an assertion that
requires a shared frame after two projected fields. The linked
tail-case path instead emits both register projections followed
by select_val and branch-local frames, matching pinned OTP.
The revised fixture asserts the linked dispatch shape and retains
the independent tagged and shared-call controls. It fails on
Part 1855 and passes the exact linked compiler. The test-only
retake preserves the compiler and corpus binaries and passes
all 44 selected cases. The manifest confirms only the fixture
changed between the two frozen payloads.

Full release and managed acceptance for these changes are
pending. The complete corrected Part 1855 managed gate stays
in its original worktree with its frozen compiler.

[Heap placement](heap-placement/README.md) · [Entry projections](entry-projection/VALIDATION.md) ·
[Named closures](named-closure/README.md) · [Call-order windows](argument-order/pilot-windows-fixed.terms) ·
[Deferred cons head](cons-head/README.md) · [Tail-case continuation](tail-case/VERIFIED.md) ·
[Applied baseline](applied-baseline.json).

[Linked pilot](pilot.json) · [Pilot timing](pilot-receipt.json) ·
[Native admission](pilot-loader.terms) · [Native execution](pilot-execution.json) ·
[Normalized instructions](pilot-summary.terms) · [Raw sections](pilot-section-delta.json) ·
[First focused failure](first-focused.json) · [Fixture correction](fixture-retake.md) ·
[Frozen identity](source-freeze.json) · [Passing focused report](focused.json).
