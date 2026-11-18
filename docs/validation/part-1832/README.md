# Part 1832 — Typed guards and shorter frames

The current local package builds on the 402 matching files in Part 1831.
The OTP pin remains `ad05823719d77c8faee87348ea39513d4e2f99c5`.

| Cause | Focused release evidence | OTP reference |
| --- | --- | --- |
| A new atom-prefix path displaced existing whole-dispatch emission | `erl_epmd:register_node/3` and `string:strip/3` recover their normalized instruction match | `beam_core_to_ssa` groups one literal type before emitting dispatch |
| Calls preserve an argument after all direct private callers pass the same atom | `erl_init:run/3` retains two frame slots and `if_loaded/2` one, matching the pinned widths | SSA call facts and stack lifetime across a clobbering call |
| A successful integer guard implies an ordered integer comparison operand | Six comparisons now match in four changed images from nineteen representatives | Failed guard edges are excluded from the proven type |
| Straight-line sibling constructors and a named function argument occupy different registers and heap allocation regions | `diameter_misc_sup` gains byte equality in the focused sample | `beam_ssa_codegen` argument scheduling and heap barriers at calls |
| Disjoint guarded tail branches need a frame only around a nested non-tail call | `diameter_sup` gains byte equality in the focused sample | `beam_ssa_pre_codegen:need_frame` and `beam_ssa_codegen:cg_switch` |
| A collecting arithmetic result can be computed at a direct select arm before allocation | Two `mnesia_frag_hash:key_to_frag_number/2` arms now place the addition before the frame with the same four live X roots | Pinned stack frame placement and BIF validator |
| Separate frames can reuse a slot after its value dies | Both `key_to_frag_number/2` frames drop from four slots to the pinned three. The overlap case retains separate slots | Existing frame ownership proof and interference analysis |
| A branch constructs one call argument and then exits without needing saved Y values | `xmerl_xpath_scan:tokens/2` moves stack allocation after the list construction and ordinary tail | Pinned `need_frame` rule and initialized root accounting |
| A successful list-producing call supplies a proper list to a selector | Eleven changed images gain the missing type and all 19 new operands agree with the pinned producer and test | `beam_call_types` for `lists:filter/2`, `lists:flatten/1` and `--/2` |

In separate ERTS runs the original and generated `diameter_sup` images
agreed on both initialization and a real supervised tree. The list-tail
fixture agreed for an empty input, recursive input, invalid input and
an invalid accumulator. The trapping-BIF control keeps its frame.

The disjoint-frame controls reproduce 15 hash results and `badarith`
on original and generated ERTS images. The selected array fold and map
controls also agree. The new proper-list fixture covers an arbitrary
`lists:reverse/2` tail and produces identical complete BEAM bytes in
the two compilers.

The first broad private-atom variant added a redundant argument move in
`tftp_engine:lookup_callback_mod/2` and changed saved-argument layout in
`erl_epmd:wait_for_close/2`. The refined proof requires a read in a
supported continuation after the intervening call. It retains the
`erl_init` frame reductions and restores both prior image outputs in
the isolated comparison.

The frozen combined source passed all six focused test selections in
the 9,751-case catalog. Its 111-module sample completed in 130.4
seconds. There were 43 Equal, 68 Different, zero Unsupported and zero
SourceRejected. Both supervisor gains were reproduced and no previous
match was lost. ERTS prepared all 46 changed images. Normalized
comparison recorded 15 gains and zero losses within the sample.

The first full corpus attempt stopped before any shard wrote a
report. It left 436 differing output files across the five isolated
directories. The retake uses the same frozen compiler hash and full
module lists in new output directories.
[Interrupted attempt](interrupted-corpus.json).

[Sample](combined-sample.json) · [Status changes](combined-sample-status.json) ·
[Loader](combined-sample-loader.log) · [Frame tests](focused-frames.json) ·
[Type tests](focused-types.json) · [Normalized comparison](combined-sample-normalized.terms).

The normalized diagnostics reused disassembly by BEAM SHA-256 and by
the identities of ERTS, the disassembler, BEAM reader and comparison
script. On the same 46 changed images the raw diagnostic took 44.1
seconds, the first cached run took 37.2 seconds, and the warm run took
4.5 seconds. All three output hashes are identical. The serial cache
is separate from source compilation and was used only in diagnostics.
[Script](cached-normalized-delta.eval) · [Measurements](cache-metrics.json).

## Full release result

The retake recompiled all 1,366 Windows release modules in 734.1
seconds. It reports 404 Equal, 962 Different, zero Unsupported and
zero SourceRejected. The two new exact matches are `diameter_misc_sup`
and `diameter_sup`. No previous whole-file match was lost.

Exactly 578 images changed from Part 1831. ERTS prepared 576 in the
batch loader. The batch loader rejected both on-load modules as
expected. Ordinary loading and SHA-256 execution for `crypto` and
compression and decompression for `zstd` matched the prior and release
images. The normalized comparison gained 37 function matches and lost
0. Of 2,136 changed function records, 882 narrowed the frame-count gap
and 9 widened it. [Raw terms](full-normalized-delta.terms) ·
[Summary](full-normalized-summary.json).

A separate audit of the saved images found 935 generated modules with
non-physical label order and 313 with adjacent label definitions. All
1,366 release images use dense physical order. Simulated physical
renumbering, adjacent label coalescing and the dependent table and
checksum updates gained zero whole files and zero Code payloads.
All six sampled rewritten images passed ERTS preparation. Use physical
renumbering after branch and block lowering converge.
[Audit](physical-label-audit-summary.json) · [Per-module data](physical-label-audit-per-module.json.gz) ·
[Loader](physical-label-audit-loader.log).

## Remaining compiler work by dependency

The first differing instruction per function was classified again on
the 962 changed release outputs. Signals overlap and depend on earlier
lowering. The C# compiler generated these outputs from the original
source and its build parameters.

| Priority | Diagnostic signal | Modules | Functions | Pinned passes and confidence |
| --- | --- | ---: | ---: | --- |
| 1 | Excess plain frame slots with equal X roots | 839 | 7,847 | `beam_ssa_pre_codegen:reserve_yregs` and `beam_trim`. High for the operand delta. |
| 1 | Excess heap-frame slots with equal heap and X roots | 362 | 871 | Same allocation dependency. High for the slot delta. |
| 2 | A tuple projection leads an allocation | 510 | 2,229 | Frame placement and tuple projection scheduling. Medium. |
| 2 | A tuple test leads an allocation | 459 | 1,153 | Entry match lowering and frame placement. Medium. |
| 3 | A move leads `init_yregs` with a different frame | 685 | 3,342 | `beam_ssa_codegen` after slot and root allocation. Medium. |
| 3 | A heap check leads a combined heap-frame allocation | 394 | 1,532 | `cg_alloc` after frame placement. Medium. |
| 4 | A function from the release is absent | 453 | 4,085 | Inlining, dead-code elimination and generated function identities. Low until audited by origin. |
| 4 | Switch dispatch leads an equality chain | 361 | 1,650 | `beam_core_to_ssa` and simplification. Medium. |
| 5 | Physical label order differs | 935 | 46,259 | `beam_clean:renumber_labels`. High for the ordering cause. Its isolated simulation had no whole-file gains. |
| 5 | Type, line, atom and import payloads differ | 877, 927, 928, 901 | See normalized and section inventories | `beam_ssa_type`, `beam_dict` and `beam_asm` consume the results of earlier passes. Separate causes require witnesses. |

[Function rank](full-function-rank.terms) · [Section inventory](full-section-inventory.json).

The five-way 111-module benchmark took 67.4 seconds compared with
93.2 seconds on four processes and produced the same status hash.
The 734.1-second full retake was slower than the preceding package.
The worker count, compiler source and workload distribution differ
between those full runs. Per-module timing will guide the next
partition.

The whole solution build, layout check and Tooling validation passed,
including the disabled-preprocessing control. The first detached
regression attempt produced four original OTP boot failures in DETS.
The same assembly passed the attached DETS filter and a detached
DETS control with standard input held open.

The full retake with standard input held open completed 9,751 cases
in 3,781.65 seconds. It passed 9,748 and returned exit code 1. The three
remaining timeouts occurred in the original string Jaro resource
contract, original DETS observation and original gen/sys observer.
The DETS retake reached the observation after boot. Each of the three
failed cases subsequently passed in a fresh process on the same test
assembly. The full gate remains red and canonical readiness retains the
last passing evidence. [Isolated retakes](isolated-receipts.json).

[Full regression](regression-pipe-full.json.gz) · [Failure details](regression-pipe-failures.json) ·
[Stdout](regression-pipe.stdout.log.gz) · [Stderr](regression-pipe.stderr.log.gz) ·
[Process receipt](regression-pipe-receipt.json) · [First stopped attempt](detached-regression-stopped.json) ·
[Attached DETS](dets-attached-retake.json) · [Input pipe control](dets-pipe-control.json).

[Full corpus](otp-corpus.json) · [Section inventory](full-section-inventory.json) · [Full timing](timing.json) ·
[Full status changes](status-changes.json) · [Loader](loader.log) ·
[Crypto](crypto-execution.log) · [Zstd](zstd-execution.log) · [Tooling](tooling-validation.log).
