# Published part 353 verification

All three runs target commit `50e756f73437eb8d2882a19f729bccb09af74f3d`, against pinned OTP-29.1.1 (`ad05823719d77c8faee87348ea39513d4e2f99c5`).

| Run | Independently checked result | Evidence |
| --- | --- | --- |
| [Windows CI](https://github.com/develmax/Erlang.Net/actions/runs/38997602508) | 7,313 tests, zero failures; eleven integration outcomes; all 7,297 prior ordered test names preserved | [CI audit](audits/ci4-guard.json) |
| [Ordinary original comparison](https://github.com/develmax/Erlang.Net/actions/runs/38997623733) | 6,321 contracts and eight fd scenarios pass; all 6,316 previous ordered contracts preserved | [Semantic audit](audits/ordinary4-guard.json) |
| [Original scheduler profile](https://github.com/develmax/Erlang.Net/actions/runs/38997624227) | Five fresh original VMs with explicit `+S 2:2`; all five typed outcomes and normal cleanup pass | [Native audit](audits/native5-guard.json) |

Strict raw equality remains [refused](audits/ordinary4-strict.json) for the single predeclared `erlang-term-display-map` stdout ordering variant. The ordinary report is not normalized; no other current variant or historical raw-row change was accepted.

Raw downloaded archives, extracted artifacts, job logs, run/job metadata and download receipts are retained in `ci`, `ordinary` and `native`. The [archive index](archive-index.json) maps original paths to exact copied byte hashes. `readers` and `native-reader` preserve the reviewed passive verification sources. These readers inspect captured data and committed source; they do not execute downloaded code.

Preserve the earlier reader refusals and source corrections in the working evidence history. The final CI reader admits only the enumerated 134 unchanged non-C# Git LF representations and eight previously reviewed publication changes; it does not normalize arbitrary source. Native installed binaries/source checkout are absent from the download, so the audit validates captured provenance and hashes rather than claiming to rehash absent files. CI package payloads are bound through the accepted full-run receipts, not newly rehashed absent nupkg files.

These results complete the stated part 353 verification scope. They do not promote scheduler APIs to complete compatibility, prove physical CPU/thread affinity or establish performance parity. The command ledger stays at 16 verified, 24 known differences and 547 awaiting full verification.
