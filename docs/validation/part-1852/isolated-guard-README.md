# Defer nested tuple projections until the guard succeeds

The candidate starts from the sealed Part 1847 compiler with ordinary kernel include provenance and the independently verified signed-mask type proposal. Reference is Windows OTP 29.1.1 at `ad05823719d77c8faee87348ea39513d4e2f99c5`, and SDK is 10.0.400. No dependency was added.

Pinned `beam_core_to_ssa:select_extract_tuple/3` projects the fields used by clause code. Pinned `beam_ssa_opt:ssa_opt_sink` moves each projection to a suitable common dominator of its uses. For a terminal checked nested tuple with a signed-mask guard, the only field used before the guard is its operand. The C# lowering checks the outer tag, opens the nested tagged tuple in X0, and projects that operand to X1. The mask operation uses a two-register initialized root prefix. Only the success edge projects the remaining fields, prefers free final outgoing registers, and reuses X0 for the last projection. The existing scheduler arranges the final argument moves and the empty-frame tail call. On inner mismatch or guard failure, the closed outer-tag fallback runs. The distinct outer mismatch returns the untouched selector. Binding-sensitive patterns, repeated tail fields, extra guard operations and other continuations retain the general emitter.

| 15 modules, one worker | Equal | Different | Unsupported | SourceRejected | Comparison |
| --- | ---: | ---: | ---: | ---: | ---: |
| Saved signed-mask baseline | 3 | 12 | 0 | 0 | 14.69 s |
| Candidate | 3 | 12 | 0 | 0 | 13.66 s |

Only `inet6_sctp` and `inet_sctp` changed. The change set is `Code` and its derived `Attr` checksum in each image. Both `open/1` functions now match the pinned instructions after normalization, with zero normalized losses and unchanged complete-file statuses. The displayed timings are individual observations and do not establish an acceleration.

The permanent fixture `beam_nested_guarded_tail` compiled by OTP with `erlc +deterministic` matches the candidate byte-for-byte at SHA-256 `FD991ECF6C23D03195AC55C618A837625BBD764BBE5890F846C1645B972C31D0`. Its SDK regression passed and failed on the baseline at the intended comparison. Pinned ERTS prepared and loaded both release images and the fixture. Fixture execution agreed on all 12 inputs, including outgoing argument order, signed integer bounds, a boxed integer, non-integer input, nested typos, and unfamiliar selectors. The two SCTP exports agreed with the release on all six invalid-port inputs each, without opening sockets.

The next distinct `inet6_sctp` gaps are `peeloff/2` field spill provenance, `peeloff_opts/2` fallback slot trimming, and `send/4` nested projections and outgoing constructor registers. The full corpus and repository gate are outside this focused verification. The new file was formatted after the staged build. The existing formatter also flagged an unrelated inherited region of `GeneralEmitter.cs`, which is outside this patch.
