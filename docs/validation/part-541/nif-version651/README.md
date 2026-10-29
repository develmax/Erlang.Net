# Part543 — versioned NIF admission and byte-exact loader errors

This finishes the local N1 tranche on the tested Part542 composition `4de41c4fafce7ebcbd2cde41e53fa46bdf6ebd53`. Parts541/542 have since merged into main with the same tested trees. The old WIP, source119 and all previous observations remain preserved; their results are not relabelled as final162 evidence.

## Runtime changes

Read only entry fields present in the declared supported ABI: the 64-byte base, optional VM variant/options/resource-init/minimum-ERTS tails, and the legacy 24-byte function stride before function flags exist. Preserve pinned OTP rejection of old/future versions and minor2.5, version/variant precedence, and options values that OTP ignores. Do not read a full modern structure from an older entry.

Preserve bytes for native diagnostic strings and for the local `%T` calling-module fragment. OTP constructs the returned error charlist from formatter bytes; decoding that buffer as Unicode scalars changed `195,169` into `233`. The fix is local `Latin1` byte preservation; global atom, BEAM and display encodings remain unchanged. Default `nif_error/1,2` metadata retains `erl_erts_errors` and actual argument frames. Two Linux bridge declarations now have their correct `void` return types.

## Final local acceptance

| Evidence | Release | Debug |
| --- | ---: | ---: |
| Existing native ABI scenarios | 42 | 42 |
| Complete typed/raw ABI comparisons, reviewed + ordinary | 84/84 | 84/84 |
| Additional valid Latin-1 calling-module scenarios | 4 | 4 |
| Complete module values/stacks, reviewed + ordinary | 8/8 | 8/8 |
| Managed control groups | 4/4 | 4/4 |
| Distinct affected regression cases | 639/639 | 639/639 |
| Fresh complete function comparisons / managed cleanups | 10 / 6 | 10 / 6 |

The affected selection is 633 regression cases plus six disjoint fresh-original managed function cases, with fixed per-profile assemblies and no final retry. It is not the full 9,451-case suite. Normal Tests builds refresh the directly aliased NativeBackend dependency; every common DLL is hash-equal to the final backend output.

Fresh final-plan counters610 acquisition also passes both Release modes and actual counters3/3 plus atomics3/3 retirement, zero live bytes/invocations. The native-layout metadata receipt is explicitly reused. This uses its declared six-role reference-number protocol; the separate N1/module comparisons above are strict whole-byte equality without projection or rewriting.

The module formatter regression has meaningful old-code evidence: ASCII passes both routes while three valid nonASCII names fail all six whole comparisons. The correction passes all eight. The initial unsupported non-Latin-1 module compilation and the earlier eval-versus-compiled-worker context mismatch remain separate failed preparation attempts. Both final sides use the same genuine compiled MFA worker, retaining complete stacks.

Exactly 42 native DLLs are reused from receipt `ABA726664BA76456C0666BB228E64FF569FC39C928853A42392BFFB329305228`; no native rebuild occurs during parent/module integration. Four added module scenarios use one of those existing images. The native build plans and attributes remain unchanged; only the capture plans are deliberately extended. No controlled speedup is inferred from runs on different source compositions.

## Source and delivery gates

The final162 C# files pass exact Roslyn token/literal equality, layout0 and both configurations' builds. The [current descriptor](../../part-542-owned-correction/current-source-freeze-nif543.json) is `582D45E703580AF8F4A8AB7CC67A1AA4BC473ACB358B59B1EF32068078F72B2D`. All8 source checks and13 early checks pass4,112 exact members/2,226 source guards. Cross-platform raw input and committed-blob checks remain mandatory at publication.

The focused Linux workflow reuses one pinned provider, one existing bridge and the existing42 native fixtures. Acceptance requires native84, module8, callback68, actual pthread bindings/create/join, ordinary-loader3 and cleanup on the final head. Independent failures are aggregated; dependent replay requires successful preparation. Existing `$ORIGIN/deps`, `LD_LIBRARY_PATH` and missing-dependency fixtures are reused. Debugger reuse/dump fallback is preserved; no execution-time improvement or all-TLS/C++TLS compatibility is claimed before Linux evidence.

At this source checkpoint Linux final-head CI, current review and merge remain pending. Canonical counts are708 registered /180 verified /504 pending /24 known differences; API/full-contract deltas0/0. Whole `erlang:load_nif/2` stays open. Next production work is genuine upgrade and its callback/publication ownership transition; monitor/select and broader lifecycle contracts remain separate requirements.

The [summary](summary.json) binds [evidence.zip](evidence.zip):5,223 readback-verified members,9,928,540 bytes, SHA256 `2FDA245FB3BFA7C724F4A9D32D22714D306FAF408564C32A9CF121C82EC5C796`. Executable/native memory dumps and host DLL binaries are excluded; source, raw outcomes, cleanup, provenance, receipts, meaningful failures and final source162 are retained.
