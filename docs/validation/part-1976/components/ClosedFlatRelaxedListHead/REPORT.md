# Flat fixed-length generator heads after the singleton stage

The frozen candidate is Part 1973 compiler f685472e21eddb969dc244fa620539ff5d145c8d267b0f8e9486a7f30dad2476. The pinned source is Windows OTP 29.1.1 at ad05823719d77c8faee87348ea39513d4e2f99c5. This directory is separate from the sealed singleton-stage directory. Both incremental patches use its hash-verified postimages and replay strictly.

| Adjacent shape | Functions | Decision |
| --- | ---: | --- |
| Closed flat head with distinct variable fields, no filters and a frame after the final nil test | 8 | Admit as the same source rule |
| Closed flat head with an additional entry guard | 1 | Preserve guard-specific scheduling |
| Nested tuple refinement inside the closed head | 1 | Preserve stage ownership and extra field checks |
| Frame allocated before a nested read | 9 | Preserve separate allocation planning |
| Remaining early projection shape | 1 | Retain default |

The eight positives span snmpm_config, megaco_stats, megaco_messenger and httpc_manager. source-positive.terms matches every physical candidate to one concrete unfiltered source generator, using its pinned abstract forms, function owner and closed head width. The measured widths are two, three and four. The matched pinned paths check the outer cons, place the next outer list in X0, advance through every inner cell in one scratch cursor, and branch to the same unframed skip on a failed inner test. The nil test shares that skip target. The outer test has its own nil/invalid-source continuation. The tested final prefix stays initialized because each new field is defined into the next scratch X, the reused cursor remains defined, and incoming X arguments are untouched except for X0 on the already-proved outer cons edge.

The incremental C# patch retains the original four-clause source proof, self-recursion, matching captures, nil result and bad_generator cause. It admits a nonempty closed flat head of distinct named variables and threads the same scratch cursor through the existing structural planner. The planner's register bound remains active. All fields are preserved until the success frame and actual line, heap, register and frame emission stay with that planner. Open tails, repeated variables, refinements, guards and strict mismatch paths remain on their existing codepaths.

Nine native probes pass on both pinned and frozen Part 1973, matching the improper outer input stack and line. They include finite 2?4-element lists, malformed inner lists, open inner tails, repeat bindings, typed guards and nested tuples. The incremental permanent test extends the sealed singleton structural regression to two-, three- and four-cell helpers. It checks that every cursor read and rejection stays unframed, and that the valid arm owns the continuation frame after the final nil test. Both touched C# files parse without diagnostics. There has been no SDK build, candidate execution or measured parity gain.

Pinned beam_core_to_ssa match_con/select_types preserves the separate cons domain, and beam_ssa_pre_codegen place_frames/find_yregs uses the checked success edge and kept fields. The source and narrowed physical records are retained in source-positive.terms, source-shapes.terms and multi-cell.terms. The fixture's pinned precodegen listing independently preserves register lifetimes in the same shape.
