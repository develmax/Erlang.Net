# Part 1996 local outgoing root schedule

Baseline Windows OTP 29.1.1 ad05823719d77c8faee87348ea39513d4e2f99c5. The [observed ledger](../part-1990-local-parity/observed-status.json) remains 539 Equal, 827 Different, 0 Unsupported and 0 SourceRejected.

| Scope | Different | Changed images prepared | Normalized gains / losses | Corpus time |
| --- | ---: | ---: | ---: | ---: |
| [Outgoing roots](p155-shared-call-root-order/status.json) | 25 | 16 | 4 / 1 | 74.277 s |
| [Root schedule and map inline](p156-root-schedule-and-map-inline/status.json) | 26 | 1 | 0 / 0 | 68.970 s |

Each scope has zero Equal, Unsupported and SourceRejected. The differences reported for the first scope use latest observed images, some compiled before the immediate parent. An immediate-parent recompile proved the ASN.1 normalized loss predates the root schedule. The parent and current asn1db_member/3 terms both hash to 6391B003520DB0CCF69358290301E672FF053C51414E13DE9916F631F164AFEF.

A valid relocation starts from an already emitted clear, direct outgoing reads, one frame epoch and an unprotected call. A subsequent X0 assignment can follow the clear if its source does not read a retiring slot. The small map inlining shape uses the pinned cost, capture-avoiding substitution and a variable-only actual argument proof. The compiler now inlines the bounded lookup in beam_utils; its two bodies still differ and its original private helper is no longer emitted.

Twelve focused managed tests, the previous 110 paired native cases, [three SSH state transitions](ssh-state-paired.terms) and [four label rewrite and callback traces](label-rewrite-paired.terms) passed. The isolated managed project initially omitted transitive fixture source dependencies and then the runtime compressor DLL. The accepted run used the unchanged chunk parser and copied the build's native compressor beside the harness binary. See [initial fixture build](initial-root-fixture-build.log), [native binding failure](initial-compressor-binding.stderr) and [transferred source identities](source-transfer-2156.json). Whole-corpus acceptance, complete package regression, readiness and commit remain pending.
