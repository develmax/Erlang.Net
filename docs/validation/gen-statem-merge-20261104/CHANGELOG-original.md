## Part586 — 2026-11-04 — Reconcile gen_statem PR112 with file/gen_event main

**Part status:** merge resolution implemented; bounded checks and immediate PR publication follow.
**Objective and scope:** resolve PR112 conflicts with main c6ce68bfc and refresh required source identities without full regression.

### Changes

Preserve code, file, gen_event and gen_statem registration/selection together. Preserve main's enum order and append GenStatem. Keep both BEAM send-error capture from main and the PR's badfun frames. Start all source plans from main's policy/membership and refresh only declared byte identities, sizes and dependent producer/workflow pins.

Both branches allocated Part580. Preserve main's file receipt at that path and relocate the unchanged gen_statem publication receipts to [a dedicated namespace](../validation/gen-statem-port-20261104/README.md), retaining their original narrative separately. New evidence belongs to Part586; older comparison/assembly results remain historical, not a claim about this merge head.

### Decisions and compatibility

Reuse existing registration and source-admission tools; OTP29.1.1/ad058237 stays pinned. No package, source-language rewrite, default registry/status promotion or workflow-trigger change. Original Erlang/BEAM assets remain unchanged. The review finding about retaining async sys continuations remains open and is separate from this urgent merge/hash repair.

### Validation and handoff

[Repair receipt](../validation/part-586/README.md) records the actual build, bounded call tests and passive hash admissions. Full regression/readiness and manual CI dispatch are unrun. Push immediately to existing PR112 after those checks; no merge or review-thread resolution is authorized. Commit: `fix(gen_statem): reconcile main and refresh source identities`.

