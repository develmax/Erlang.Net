# Shared-frame interior argument copies in Part 1875

The full accepted uncached Part 1875 input has 410 Equal and 956 Different modules. Its existing rank reports 7,718 MFAs in 826 modules with disassembled excess plain allocation widths and equal live X. The Python scan decoded all ranked modules and paired 7,689 of those functions. There were 2,790 paired MFAs whose first opcode mismatch was move versus init_yregs, and 5,778 paired the same first body call with a straight generated setup. Counts below overlap.

| Shape in the same first body-call cohort | Functions | Modules |
| --- | ---: | ---: |
| An entry save first read after the call | 2,815 | 632 |
| A single pre-call reload with original X availability unproved | 96 | 64 |
| A single pre-call reload with original X available | 29 | 27 |
| An entry save with no semantic read | 28 | 25 |
| Pinned allocate(0), generated adds only init_yregs before the matched call | 541 | 191 |
| Same post-call list head/tail check in that zero-frame subset | 122 | 24 |

The large list-check subset exposed a checked value copied through Y solely inside an interior argument schedule. For example CryptographicMessageSyntax-2009:dec_CertificateChoices/2 spills x2 to y0 in two alternative arms, loads the independent second argument, reloads y0 into x0, calls and constructs the result. The pinned image copies x2 directly into x0 for both corresponding call targets. This slot's only semantic references are those two pairs. The ordinary call still requires allocate(0)/deallocate(0).

The shared-frame scan excluded allocation changes, trim, exception handlers, receive markers, mailbox instructions and debug annotations. It found 613 locally provable direct copy pairs in 415 functions across 211 modules. Of these, 44 functions across 36 modules have at least one whole physical slot whose non-init references belong entirely to those pairs. Twenty functions across 18 modules could cover the entire frame width gap. An immediate-entry conservative replay using individual reaching definitions independently qualifies 35 relevant pairs in 32 functions across 26 modules, including 16 functions across 14 modules with sufficient full-width coverage. Frozen disassembly establishes the observed operand shapes. Executing the pass could alter downstream scheduling and would require normalized and byte checks.

The emitter's existing EliminateEntryArgumentCopies scans spills in the owning frame allocation's setup, including a separate branch-owned allocation if present. It cannot reach saves below a first body call while that same frame remains active. OTP-29.1.1 beam_ssa_pre_codegen.erl defines frame-required values in find_yregs, reserves their registers in reserve_yregs, then allocates from live_intervals. Its beam_ssa_codegen.erl setup_args uses direct X moves via order_moves for body call arguments. These passes explain the distinct input/interior lifetimes without reliance on names.

The patch extends the existing normal CFG's per-definition HasSingleStackCopyUse proof. It requires one plain allocated frame and an unchanged source X on a straight moves-only segment, checks the static call's explicit argument prefix, verifies every Y operand reference except initialization is one of the admitted moves, and forwards the untyped Y reload. The next existing frame rewrite removes the now-unused saves and initialization, or compacts an interior slot using the original frame ownership and slot renumbering. Keeping original stores until successful compaction preserves every initialized GC root when compaction cannot prove the layout.

Positive release controls include CryptographicMessageSyntax-2009:dec_CertificateChoices/2 (two sibling saves to the only slot), mnesia_frag:search_last/5 (two definitions of its highest slot), and compile:write_binary/3 (an interior Y slot with two higher live slots). A negative control is ct_config:allocate/2, whose y3 is reloaded as an argument and remains live for a later exact match. The permanent raw fixture asserts both branch copies, retained frame ownership at width zero, compaction of a nontrailing slot with a surviving root, and rejection of a later Y read, clobber, or intervening collection.

The original patch applies to sealed Part 1877 archive D39673CA5923FC65F9138D3438DE466D7D24A1D3273738B92E6C2876D932E4B1. The fixture hunk was rebased for sealed Part 1878 archive 466AD88E43B8EACEF2BAFB0E46949175EC0CA2420F3F1EF4FB853799790FC7BE. No candidate compiler build, VM admission, normalized instruction comparison or whole-file acceptance was run in this diagnostic lane.

