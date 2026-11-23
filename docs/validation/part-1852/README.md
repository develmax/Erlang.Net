# Part 1852 — Keep independent construction live and lower nested tuple guards

The call-result scheduler keeps X0 rooted through an independent
constructor heap check. A heap-neutral nullary BIF and its list or
tuple construction use X1. The original Y store occurs just before
outgoing argument setup overwrites X0. The proof excludes reads of
the saved slot and deliberate dependent constructions.

A successful signed mask proves an unbounded integer in a guarded
comparison. When the incoming variable has no stronger range,
the negative literal does not provide a finite bound. The
SSA optimization option gates this annotation.

The nested guard path extracts only the required field before the
guard. The proven record and that field form the live X prefix.
The success path extracts the remaining fields and schedules the
direct tail call with the existing argument permutation helper.
The matching tag with a bad payload uses the original rejection.
Other tags return the selector unchanged.

| Independent release sample | Equal | Different | Unsupported | SourceRejected | Outcome |
| --- | ---: | ---: | ---: | ---: | --- |
| Independent call-result construction | 6 | 3 | 0 | 0 | Exact 112 Code bytes of megaco_tcp_sup:init/1 |
| Signed mask | 3 | 12 | 0 | 0 | Exact inet6_sctp Type chunk |
| Nested guard | 3 | 12 | 0 | 0 | Two exact normalized SCTP open/1 bodies |

No previously matched file is lost in these samples. Pinned
ERTS confirms the supervisor child data and optional error
paths. The signed-mask and nested cases confirm their real
and fixture success and failure observations. The nested fixture
matches OTP byte-for-byte, and the tests reproduce the tracked
defects on their preceding individual candidates.

All 35 linked focused cases pass on the frozen source. This
report preserves the isolated proofs and does not generalize
them to a release compilation or the full local runtime
matrix. No new dependency or runtime MFA was added.

[Source and assembly identity](source-freeze.json) · [Focused tests](focused.json) ·
[Call-result proof](isolated-construction-README.md) ·
[Signed mask proof](isolated-mask-README.md) ·
[Nested guard proof](isolated-guard-README.md).
