# Entry argument frame residuals after the scoped Part 1873 and Part 1874 pilots

The baseline is the 71 distinct static and dynamic body-call rows in the frozen Part 1870 frame census. The latest completed pilot for each MFA takes precedence. The Part 1874 full-run directory did not yet contain a completed report when this audit ran. It was excluded.

| Latest available pilot | Restored Part 1865 widths | Retained Part 1870 widths | Other widths |
| --- | ---: | ---: | ---: |
| Part 1873 | 7 | 12 | 1 |
| Part 1874 | 8 | 1 | 0 |

The remaining 42 MFAs have no image in either completed pilot. The "other" row is file_sorter:file_loop2/8. Widths are separate from normalized code and file equality.

## Checked projection before ordinary argument copies

The existing FindEarlyEntryLoad CFG transfer already proves an original X value remains available at the chosen insertion point. Its backward interval permits Line, Move and constructors with a single predecessor and a destination noninterference check. The ordinary straight-call gate adds a second prefix check from save through clobber and currently rejects any tuple projection. The ordinary suffix check additionally rejects an independent Y read, even when it reads another slot and writes another call argument.

Keep the existing backward interval. Allow GetTupleElement in the earlier unique prefix only, without moving across it. Keep the source in X at insertion using current transfer logic and retain the single-use Y gate. Allow a later untyped move from a distinct Y slot to an X destination only in this projection route. The move must avoid the reload target and preserve unique fallthrough. Require local or external ordinary call and target within its explicit X prefix. Exclude dynamic calls, Send, tail calls, a closure, a collecting instruction or a branch inside the proposed prefix/suffix. Keep existing routes for these other instructions.

OTP-29.1.1 beam_validator:vi({get_tuple_element,...}) verifies the source tuple size and performs an assignment. The existing compiler transfer already accounts for the write. The beam validator's validate_body_call requires an allocated frame. OTP beam_ssa_codegen:setup_args uses order_moves after generating independent copies. The scoped C# change should preserve the existing order and uses a proven earlier position for exactly one copy.

The frozen Part 1870 wire screen yields 14 otherwise-blocked sites across 10 functions in 9 modules. Sealed Part 1873 retains 8 sites across 5 functions in 4 of those modules. These 5 are erl_lint:-check_fields/8-fun-0-/8, erl_lint:-pattern_list/4-fun-0-/4, observer_html_lib:-expandable_term_body/4-fun-1-/4, observer_traceoptions_wx:-create_styled_txtctrl/1-fun-0-/3 and reltool_mod_win:-create_editor/1-fun-0-/3. The wire screen selects zero inputs in the 15 width-restored controls. Six historical frameless tail wrappers and two closure ownership negatives also fail the screen on available frozen images.

The exact candidate output remains to be measured. Thirteen of the 14 sites have a Part 1865 direct-copy correspondence. The pinned register order differs at most of these sites and frequently uses swaps. Exact matching on the selected modules remains an independent acceptance question.

## Direct dynamic apply

A separate smaller proof can reuse the established straight prefix and suffix. OTP's beam validator validates apply with Live+2, including module and function, through validate_body_call. Keep apply_last and implicit CallFun operands excluded. This has 6 historical sites over 3 modules without widening the Y suffix. All six have Part 1865 direct-copy correspondences, four of which are also visible at the same physical register pair in pinned snmpa_symbolic_store. The published scoped pilots did not measure these three modules.
