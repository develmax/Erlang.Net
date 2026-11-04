# Architecture decisions

## Current decisions

[gen_statem alternatives and constant ownership](decisions/0007-gen-statem-alternatives.md) record the module-specific user authorization, retained OTP assets, runtime reuse and finite acceptance scope.

Accepted main [PR103](https://github.com/develmax/Erlang.Net/pull/103) adds native async accounting/cancellation records. Their [complete original decisions](history/main-103-decisions/index.md) are preserved separately from the six architecture records below; bounded native controls do not establish whole statistics acceptance.

The [decision index](decisions/index.md) contains separate documents for:

- [Baseline and immutable term model](decisions/0001-baseline-and-term-model.md)
- [Runtime ownership and OTP backends](decisions/0002-runtime-and-otp-backends.md)
- [Compiler pipeline and source mapping](decisions/0003-compiler-and-source-mapping.md)
- [Evidence and compatibility](decisions/0004-evidence-and-compatibility.md)
- [Packaging, licensing and delivery](decisions/0005-packaging-and-delivery.md)
- [Pattern and closure binding scope](decisions/0006-pattern-scope.md)

Each document preserves its original decision text and identifies current interpretation and superseded assumptions. Current project instructions take precedence over historical checkpoint permissions or architecture wording. These documents do not establish that every planned compiler/runtime capability is implemented.

## Historical decisions and checkpoint records

The complete previous document is preserved in [ordered decision history](history/decisions/index.md), including all numbered architecture decisions, per-part implementation choices, acceptance records, failed attempts and pending gates. No records were deleted.

## Maintenance

Give a new architectural decision its own document with context, decision, status, consequences and evidence links. Update the index. Record implementation progress in the active [changelog](../CHANGELOG.md), and preserve superseded records with an explicit status. Keep current compatibility in the [compatibility overview](COMPATIBILITY.md).
