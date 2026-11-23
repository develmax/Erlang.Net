# Part 1834 — Preserve selectors, argument registers and producer facts

The local package uses the Part 1832 full Windows OTP 29.1.1 baseline
and includes the Part 1833 compiler changes. The pinned OTP commit is
`ad05823719d77c8faee87348ea39513d4e2f99c5`. The compiler and test images
are bound to the 88-path source manifest and .NET SDK 10.0.400 on
Windows x64 runtime 10.0.11.

| Scope | Evidence |
| --- | --- |
| Private call arguments and boolean selectors | `erl_init:run/3` has the exact pinned disassembly including labels, failure line and call setup. |
| Resolved list producers and immutable bindings | Added `lists:sort/1` and `lists:usort/1` to the proper-list proof. Five additional release operands follow linear aliases and successful matches. |
| Nonnegative integer producers | Fourteen audited release arithmetic operands match the pinned ranges. The older `phash/2` does not gain a type. |
| Shared guarded atom dispatch | `ssh_channel_sup:start_child/7` reaches exact disassembly. The release probe compares successful loads and complete function-clause results. |
| Case argument construction and adjacent calls | `megaco_tcp_sup:start_accept_child/2` builds directly in X1. Two SNMP functions keep the first result live, use a two-slot frame and clear dead Y operands before the next call. The secondary guarded tuple shapes still differ. |
| Tagged tuple reconstruction | The two fresh tagged result arms share arity and dispatch. The identity arm returns the original selector and the changed-tag arm still constructs its result. The deterministic fixture is byte-identical to OTP with SHA-256 `EC262380617011F315886CDCA000582516D7D75EF16EE93267D554E52C8785E0`. |
| Outgoing scratch copies | The backward move-only proof finds 51 affected pairs in 40 modules. `snmp_app_sup:stop/0` matches exactly. Live repeated argument and tail-call controls retain behavior. |

## Frozen candidate validation

The 40-module run took 75.77 seconds with two workers. It retained all
seven existing Equal results and 33 Different, with zero Unsupported
and zero SourceRejected. There were no new or lost byte matches. Ten
changed functions gained normalized equality and zero lost equality.

ERTS prepared 30 changed images. Ordinary loading covered `crypto`
and `zstd`. The two on-load execution results and the three Megaco/SNMP
initialization results match release and prior output. The new exact
tuple fixture includes two successful return shapes and two bad
selector shapes. Its five ERTS outcomes agree on the compared images.

The compiler and test projects built successfully from the frozen
source, using previously built unchanged project dependencies. The
two builds took 38.90 and 37.76 seconds. All 13 selected permanent
tests passed. The two early compiler errors and the initial
reconstruction failure are retained. The latter revealed that the
fixture used register-case planning before the first attempted
lowering hook. The final code applies the identity proof there and
preserves ascending atom keys with descending independent blocks.

[Corpus](focused-corpus.json) · [Timings](focused-corpus-timings.json) ·
[Normalized instructions](focused-normalized.terms) · [ERTS](focused-loader.log) ·
[Tests](focused.json) · [Source and binary hashes](results.json) ·
[Independent scope controls](independent-controls.json).

The full compiler corpus and repository regression remain pending.
The highest ranked remaining cause is excess Y slots. A separate
saved-output audit identifies 93 functions in 67 modules where entry
register proofs permit removing copies but the current linear-frame
gate rejects the branch layout.
