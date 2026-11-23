# Integer type proof for a successful negative guard mask

Base uses the sealed Part 1847 source with the independently verified ordinary kernel include provenance patch. Reference is the Windows OTP 29.1.1 release and pinned OTP source `ad05823719d77c8faee87348ea39513d4e2f99c5`; SDK is 10.0.400. No dependency was added.

Pinned `lib/compiler/src/beam_call_types.erl` models `band` as an integer result on success. `lib/compiler/src/beam_bounds.erl` has no finite bounds for `band` when one operand is an unknown integer and the other is a negative mask. The compiler now records that integer proof on the comparison which follows an existing register guard arithmetic step. Failure targets and arithmetic instructions are unchanged. Positive masks, other operators and other lowering paths retain their current proofs.

| 15 modules, one worker | Equal | Different | Unsupported | SourceRejected | Comparison |
| --- | ---: | ---: | ---: | ---: | ---: |
| Baseline | 3 | 12 | 0 | 0 | 16.10 s |
| Candidate | 3 | 12 | 0 | 0 | 14.69 s |

Eight images changed, affecting 18 normalized function bodies with zero new or lost normalized-function matches. `inet6_sctp` now matches `Type` exactly, leaving only `Code` and its derived `Attr` checksum. No other `Type` match or whole-file match changed. The permanent guard fixture checks the exact pinned `Type` chunk and the typed operand at the comparison. The SDK harness passed, pinned ERTS prepared all eight changed images, and the standalone fixture agrees with pinned OTP on 12 outcomes. The changed `inet6_sctp` also loads and agrees with pinned OTP on six negative, out-of-range integer and invalid float inputs to `open/1`. Raw reports and transcripts are retained.

Remaining `inet6_sctp` code differences:

| Function | Remaining differences |
| --- | --- |
| `open/1` | The release tests outer and nested tags and reads the port before checking the mask. It projects remaining fields after the guard, keeps them in X registers and uses the empty frame. The candidate eagerly saves the selector and record fields in Y and repeats the outer tag test on failure. `inet_sctp:open/1` shares the shape. |
| `peeloff/2` | The candidate and release preserve the original socket and projected socket in opposite Y order. The release projects the new socket directly into its saved slot before the registration call. |
| `peeloff_opts/2` | Two failure continuations still differ in result save placement, slot kills, branch ordering and frame trim. |
| `send/4` | Nested record/list matches spill six short-lived projections. The release keeps them in X registers and builds the outgoing record in its final X1. |

The next common dependency is nested tagged-record selection with guard-demanded projections before other fields. The full corpus and repository gate were not run for this isolated proposal. Timing is a single observation per build, and the changes do not establish a speed improvement.
