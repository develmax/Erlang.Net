# Core aliases for matched tokens in ordered cons heads

This read-only OTP audit and isolated C# proposal use Windows OTP 29.1.1 at `ad05823719d77c8faee87348ea39513d4e2f99c5` and the verified ordinary-branch compiler `fee707c33a51aa4b3b3d89ed663714a9d6f534b72bc8aca8cc993696613c6771`. [Compiler baseline](../ordinary-branch-cons-constructions-1968/verification/verified-receipt.md).

Pinned `sys_core_alias.erl` applies the same Core key rule to both nested function-head patterns and a case clause's patterns. The candidate pattern key is a recursive exact-value description of the tuple/cons and its lexically scoped variable identities. `get_pattern_keys` at lines 256–276 descends into lists and tuple patterns. `pre` at lines 89–135 establishes the clause's substitution keys and replaces matching nonliteral constructors. `sub_cache_nodes` at lines 207–228 creates a fresh alias when the key is first reused, and `alias_pattern_keys` at lines 291–324 attaches it to the pattern. `nodes_to_key` at lines 326–355 distinguishes types, exact literals, variables, and constructor shape. The already-folded `dcorefold` listing still reconstructs directives. The `dalias` listing places a synthesized alias on each matching second-cell tuple and substitutes it into the returned list. The `no_alias,dalias` control removes both changes. [Before Core](before/epp_dodger.corefold), [after Core alias](alias/epp_dodger.core_alias), [disabled Core alias](disabled/epp_dodger.core_alias).

| Case source line | Tail call line | Matched directive atom | Synthesized Core variable |
|---:|---:|---|---|
| 537 | 539 | `define` | `_@r0` |
| 540 | 542 | `undef` | `_@r1` |
| 543 | 545 | `include` | `_@r2` |
| 546 | 548 | `include_lib` | `_@r3` |
| 549 | 551 | `ifdef` | `_@r4` |
| 552 | 554 | `ifndef` | `_@r5` |
| 558 | 560 | `elif` | `_@r6` |
| 561 | 563 | `else` | `_@r7` |
| 567 | 569 | `endif` | `_@r8` |

Every key is the exact tuple `{atom, AnnoA, Directive}` in that branch's second input list cell. In the `elif` arm, the input atom is unquoted and the output atom is quoted. They have the same term identity. The dedicated `{'if',AnnoA}` and `{'else',AnnoA}` input tuples are arity two, so they cannot alias the output arity-three tokens. The aliased value remains live across `scan_macros/2` and enters the first post-call cons directly from a saved register. The other four heads still use `AnnoA`. The nine alias arms reserve 23 words and construct four tuples and five cons cells. Both negative arity-two arms reserve 27 words and construct five tuples and five cons cells. Standard release, Core regeneration, `no_alias`, and frozen C# images confirm these counts. A separate Erlang source variant explicitly binding the same nine tokens through the unchanged C# compiler reproduces all nine scheduling counts. [Physical comparison](physical-schedules.terms), [explicit-source C# listing](explicit-csharp/scan-form-all.terms).

The C# proposal uses exact literal identities and the existing `ReturnsMatchedValue` key comparison for shallow tuples. It adds the alias only in ordinary function clauses already eligible for the pure terminal list planner, and rewrites only matching direct list heads. It does not change the earlier grouped-entry and ordered-cons paths. It requires real variable bindings and rejects wildcard and repeated head names where Core normalization could separate the key identities. A changed literal or changed arity cannot select the alias. The existing module optimization gate applies, without admitting any new compile option. A read-only scan found zero recorded `no_alias`, `no_copt` or `no_module_opt` options in the 1,366 installed reference CInf option lists. [Profile scan](profile-scan.terms).

The permanent fixture contrasts a 7-word matched-token heap reservation and direct saved tuple head against an 11-word changed-token reservation. It also checks preserved tail values, a wrong-token fallback without a call, and the original tail failure line. Pinned, disabled-alias OTP, and frozen C# images agree on all five native scenarios. The frozen image exhibits the original 11-word positive shape, which the permanent test rejects. [Pinned fixture](fixture/pinned.terms), [disabled oracle](fixture/no_alias.terms), [frozen fixture](fixture/frozen.terms), [native outcomes](fixture/pinned-results.log).

Both patches replayed with `--whitespace=error-all` and matched all recorded postimages. The isolated C# candidate and permanent regression have no SDK result. The proposal remains separate from the nonterminal case-binding change. [Production patch](production.patch), [test patch](tests.patch), [manifest](patch-manifest.json), [artifact hashes](receipt-hashes.json).
