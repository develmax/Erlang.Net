# Call selector argument construction

The starting compiler is the frozen Part 1833 source overlay plus the independently validated common PID guard patch. The Windows OTP 29.1.1 release is pinned to `ad05823719d77c8faee87348ea39513d4e2f99c5`.

The pinned `beam_ssa_pre_codegen:reserve_regs/1` reserves the incoming arguments' X registers. Its backward `reserve_xregs` traversal prefers the call's outgoing registers for values that will be passed to the next call. This patch reuses the existing C# argument-construction planner and entry-call emission. A direct terminal case may consume the precise construction plan when at least one unchanged input already occupies its call argument register, all constructed arguments reach their outgoing registers directly, and the case does not need an original entry argument. The existing move and frame paths remain available for overlapping placements, effectful arguments, surviving entry values, and unsupported calls. No third-party .NET or production BEAM dependencies are introduced.

In `megaco_tcp_sup:start_accept_child/2` the whole instruction prefix through `supervisor:start_child/2` matches pinned OTP. It preserves X0, builds the list and both nested tuples in X1, accounts for 13 heap words and two live X registers, and emits `allocate_heap` with zero Y slots. The complete BEAM remains different. Its Code section decreased from 547 to 537 bytes with a 517-byte reference code section. The post-call error tuple, remaining Code differences, LitT, Attr and Type require separate work.

The seven-module comparison retained 4 Equal and 3 Different, with zero Unsupported, SourceRejected, gains or losses. Only `megaco_tcp_sup` changed bytes. The before and after comparison stages took 7.681 s and 6.395 s using two workers.

The ERTS release-image probe verifies the exact child argument with a controlled supervisor process. Success, the error return and the malformed-result `case_clause` matched in all three images. A second OTP fixture checks X1 construction with X0 preserved, X0 with X1 preserved, a source argument surviving the call, and a mismatched tag. All four behavior checks matched. The two positive fixture prefixes match through their local calls.

Raw evidence is in `before.json`, `after.json`, `megaco-outgoing.log` and `fixture-evidence.log`. The named test is `compiler/beam/selector-builds-in-outgoing-argument`. The integrating owner handles full corpus, full validation, readiness, and the changelog.

The final formatted source passed all three focused tests (`focused-formatted.json`) with zero build warnings and errors. The change-only diff omits three unrelated pattern-indentation suggestions from the full-file SDK formatter. The final formatted cohort output is in `after-formatted.json`. The generated images are identical to `after.json` for each differing module. The byte and status delta is in `status-delta.json`.
