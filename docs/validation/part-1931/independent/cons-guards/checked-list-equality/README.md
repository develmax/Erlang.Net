# Ordered list clause sharing

The four sequential patches make the installed Windows OTP 29.1.1 `ordsets.beam` byte-equal. Its SHA-256 is `417595cfd47ff9dc43673cee03083776cbacd3abb61166bf58c61dc6a13cce1d`. The comparison used a clean output and one worker. The 35-module check reported 28 Equal, seven Different and no unsupported or rejected sources. It retained all 23 explicit Part 1927 raw controls and four additional exact standard libraries.

| Patch and evidence | General rule | Normalized exact ordsets functions |
| --- | --- | ---: |
| [Two cons columns](../proposal.json) | Use one constructor decision and one head/tail extraction per column across three contiguous ordered clauses. Local failures retain the original terms and remain separate from guard fallthrough. Reuse the existing branch call and allocation planner. | 16 to 21 of 25 |
| [One cons column](../one-column/proposal.json) | Allocate only for the recursive insertion arm. Preserve the checked list in X for the remaining prepend and identity arms. | 21 to 22 of 25 |
| [List continuation](../cons-call-continuation/proposal.json) | Put the checked cons tail directly in the sole Y slot needed across the producer call. With a two-cell source pattern, retain the singleton return as an unframed branch. | 22 to 24 of 25 |
| [List equality](proposal.json) | Annotate both numeric comparison operands only on the successful edges of their distinct resolved `is_list/1` checks. | 24 to 25 of 25, established by complete image identity |

## References and implementation reuse

All source and stage listings use pinned reference OTP-29.1.1 at `ad05823719d77c8faee87348ea39513d4e2f99c5`. The [initial SSA](../pinned-stages/ordsets.ssa) shows the shared cons projections before optimization. The [pre-codegen listing](../pinned-stages/ordsets.precodegen) records the one-slot frame and direct Y destinations for both continuation variants. The [optimized SSA](../pinned-stages/ordsets.ssaopt) records both equality operands as `maybe_improper_list(any(), any())`. The pertinent compiler source functions are `beam_core_to_ssa:match_con/4`, `beam_ssa_pre_codegen:place_frames/1`, `beam_ssa_pre_codegen:find_yregs/1` and `beam_ssa_type:infer_type/4`. The OTP source is Apache-2.0. The implementation uses existing C# entry planners, register moves, BIF resolution and type encoding. It adds no dependencies and copies no OTP implementation.

The starting snapshot is accepted Part 1927 source archive SHA-256 `08443e380f1273a534b6927b1dabdf1baeedf4481f53e3d4d13a960062bbed9b` with unchanged inputs from synthesis base `1a5d3057eddddffdd8b9e0e8dfd6655d0a9df9d2`. For each package the receipt records expected preimage and resulting bytes. Both patch streams passed strict isolated Git replay in order.

## Checks and limits

- [Targeted raw](ordsets-only.json) and [35-module raw scope](scoped-corpus.json) ran in 3.95 and 48.42 seconds of comparison respectively. `beam_disasm` normalized local function labels only in intermediate diagnostics. Raw scope used `SequenceEqual` over complete images.
- [Four permanent tests](focused-test.log) passed in a local focused harness against the final compiler. They assert projection counts, allocation and tail-call placement, guard-proven operand types, branch results and malformed calls. Each package includes its `Erlang.Tests` harness registration.
- Native ERTS loaded intermediate images alongside pinned behavior witnesses. [Two-column ordsets](../native-retake.log) has 500 exact cases, [one-column insertion](../one-column/native-add-final.log) has 100, and [nested intersection](../cons-call-continuation/native.log) has 815. Each validates values or exception class, reason and the first OTP stack frame. [gb_sets subset](../one-column/gb_sets-native.log) has 100 and the continuation witness checks [gb_sets](gb_sets-native.log) and [sets](sets-native.log) with 2,220 operations each.
- The linked compiler, remaining previously exact modules outside this scope, and complete 1,366-image corpus still require the root integration gate. No Git or GitHub publication was performed.

