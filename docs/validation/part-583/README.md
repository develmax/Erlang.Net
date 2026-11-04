# Part583 — Preserve source-freeze bytes on Windows checkout

Head208c3a010 failed [PR110 Validate run41831944999](https://github.com/develmax/Erlang.Net/actions/runs/41831944999/job/126581636325).
The uploaded preflight report identifies exactly one failure among thirteen
plans: Monitor645, `Focused freeze hash differs`. Resource529 and MFA/native
source admissions passed; SDK/runtime work was skipped after the passive gate.

The Part582 source freeze was created with LF but was outside the existing
immutable-evidence attribute rules. A fresh Windows checkout with
`core.autocrlf=true` converted it to CRLF. Its expected raw SHA256 is
`5CBD99529AD75E6563CA6A0CD7949ED78BE6F0434DC00949F64B497B89DB3775`;
the filtered bytes hash to
`AC9D33B746168A0845ACB0868D9437B4F624E389E643D1B8E650F7E28B624E4E`.
The local working file retained LF, so the previous local admission did not
exercise the checkout conversion and did not catch this defect.

Extend the existing `-text -whitespace` evidence rules to the relocated file
checkpoints580/581, the current582 freeze and this583 receipt. Preserve the
freeze's original bytes and expected digest. Refresh declared attributes-file
identities and dependent producer/workflow pins; do not normalize raw admission
checks, weaken their comparisons or modify historical freezes.

Add a permanent CI helper regression test that runs actual Git checkout filters
against every current runtime source freeze with `core.autocrlf=true` and
`false`, requiring the resulting raw bytes to match the declared digest. Its
saved RED result reproduces this exact Monitor645 hash mismatch before the
attribute fix; its GREEN result covers both checkout modes after the fix.

`checks/` retains the hosted failure, local RED/GREEN reports, passive source
admissions, CI helper tests and documentation check. The refresh helper/audit
are under `source-plan-refresh/`; they only update current byte identities and
dependent pins. No product code, native execution state, acceptance status or
compatibility classification changes. No product build/VM, full local
regression or manual CI dispatch is needed for this attributes/metadata repair.
Hosted source admission must pass on the new PR head before this failure can be
considered repaired in CI. Remaining file provider/native gates are unchanged.
