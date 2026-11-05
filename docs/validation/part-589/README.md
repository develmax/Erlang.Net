# Part589: update ASN.1 PR118 from timer main

Merge main29b92f63d3301ec696410d0cc4147241aa32d308 into PR118, preserving
the merged timer backend, runtime registrations and source/CI identities exactly.
Four status-page conflicts are resolved by retaining both timer/main and ASN.1
notes. Main has closed journal0105, so the intact Part588 entry moves to active
0106; closed0105 is restored byte-for-byte to main. Original Part588 reports are
historical and unchanged. The qualified file-atom expression remains the only
test source change relative to main.

Focused ASN.1 project build on the merged source: zero warnings/errors;65 host
checks pass. All26 complete outcomes/stacks match a fresh admitted OTP29.1.1
observer by deterministic raw ETF. All13 passive source admissions and5400 raw
MFA/native bindings pass. Main's existing source-plan hashes need no refresh.
The exact tested source binding and fresh comparison receipt are retained here.

Use the focused build/host/oracle commands in ../part-588/README.md with fresh
output directories. No full regression, canonical readiness refresh or manual
CI dispatch was run. This merge changes no runtime behavior or compatibility
classification; hosted final-head CI/review remain required.
