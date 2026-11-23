# Part 1870 remaining common causes

These counts come from the exact Windows corpus. Opcode, frame and
helper groups overlap. The modules and functions in each instruction
group are listed in [the rank](function-rank.terms). The section
inventory has one row per mismatching file.

| Priority | Release modules | Functions or keyed bodies | Evidence and expected OTP stage | Confidence |
| --- | ---: | ---: | --- | --- |
| 1 | 826 | 7,741 | Plain allocation has excess Y slots and the same number of X roots. Audit producer and call argument placement before SSA pre-codegen frame allocation and late Y trimming. A post-allocation rewrite cannot repair an early choice that keeps a value live across a call. | High for symptoms, medium for individual lifetime causes |
| 2 | 681 | 3,273 | A move differs from init_yregs and the allocation also changes. Track which input survives each ordinary, fun and tail call. This shares dependencies with the first group. | High for matched witnesses, medium for the full group |
| 3 | 447 | 4,053 missing keys | Generated function identity, arity or extraction. The preceding census attributes 2,788 to LC, BC and MC helpers. Start with a single strict generator before grouped and nested comprehensions. Recheck function order, calls and module tables after v3_core-style lowering. | High for observed keys, medium for subsets without an individual source proof |
| 4 | 484 | 2,004 | Reference tuple projection precedes allocation. Check selector proof, argument purpose and failure edges before moving projections across calls. | High for ordering, medium for the all-module cause |
| 5 | 442 / 403 / 366 | 1,077 / 2,000 / 1,569 | Reference tuple, nonempty-list or tagged-tuple test precedes a frame. Partition by failure path and reuse the corresponding dominance and live-value proof. | High for ordering, medium for safe motion |
| 6 | 341 | 811 | Excess Y width with identical heap words and X roots in allocate_heap. Apply frame ownership and lifetime fixes first. | High for allocation evidence |
| 7 | 360 | 1,648 | Reference select_val differs from a linear exact-equality test. Check chained selector lowering and block layout after frame and helper fixes. | High for opcode grouping |
| Separate | 182 | Section count | Remaining Dbgi mismatches. Preserve exact logical include provenance from build rules and separate remaining forms from file-path differences. | High for 87 confirmed section fixes, remaining causes under investigation |

The complete section counts include Code 957, ExpT 944, Attr 931,
LocT 928, AtU8 and Line 923 each, ImpT 894, Type 868, FunT 702,
LitT 700, Dbgi 182, Docs 66, StrT 46 and Meta 1. Atom and import order,
line numbering and Type are compared separately. Changed function
layout can affect these sections. A section count alone does not
identify the first pass that caused the mismatch.

The immediate integration priority is to restore the 12 preceding
normalized body matches flagged by the complete run. The witnesses
include two move-only framed tails, redundant ordinary and closure
argument spills, direct projection, and a guard branch lifetime.
Whole-file status is measured separately.
