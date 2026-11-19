# Tagged fallback frames, branch trims, local integer facts and shared receive tails

The isolated compiler starts from the Part 1862 archive. Four changes
reuse the existing register, stack ownership and label checks. Pinned
beam_ssa_pre_codegen, beam_trim, beam_ssa_type and beam_ssa_codegen
provide the source contracts for the scope.

| Change | Verified release outcome |
| --- | --- |
| Place zero-slot frames inside a frameless checked-tag fallback | file:read_file_info/1 and beam_ssa_pp:format_tuple_set/1 gain normalized equality. Local guard, tagged success, caught/uncaught exception and shared fallback controls execute in pinned ERTS. |
| Trim one live suffix slot through a proven branch owner | Eight matching trim operations across six tested functions in inet6_sctp, inet_sctp, mnesia_lib, dbg_wx_win, reltool_sys_win and cover. Two owning branches and a throwing cleanup path execute in pinned ERTS. |
| Annotate the adjacent result of a local source-proven integer producer | mnesia_frag_hash: key_to_frag_number/2 gains both pinned integer operands from power2/1, and add_frag/1 gains the pinned arithmetic operand. Opt-out and mixed, ranged and dynamic controls pass. |
| Share identical terminal receive bodies at different tuple arities | megaco_tcp_accept:tcp_clear/1 matches the pinned one-remove schedule and gains normalized equality. The fixture preserves the remaining mailbox across a second zero-time receive. |

All 16 declared C# checks pass from 9,813 registered cases. The uncached
27-image selection takes 75.87 seconds and records 4 Equal, 23 Different,
0 Unsupported and 0 SourceRejected. It has no status changes. All ten
changed images prepare in ERTS. Three of eleven comparable changed
bodies gain normalized equality with zero losses. Complete chunk equality
does not change. Four native fixture pairs agree on values, errors or mailbox
contents. The complete release result on the preceding frozen compiler
remains 409/1,366. A complete managed gate is pending for this source.

[Frozen source](source-freeze.json) · [Focused cases](focused.json)
· [Release selection](pilot.json) · [Native preparation](pilot-loader-summary.terms)
· [Function delta](pilot-summary.terms) · [Frame and type schedules](scope-schedules.terms)
· [Native behavior](native-fixtures/receipt.json).

