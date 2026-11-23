# Part 1995 local return graph and helper identities

Baseline Windows OTP 29.1.1 ad05823719d77c8faee87348ea39513d4e2f99c5. The accumulated [observed ledger](../part-1990-local-parity/observed-status.json) remains 539 Equal, 827 Different, 0 Unsupported and 0 SourceRejected.

| Scope | Different | Changed images prepared | Normalized gains / losses | Corpus time |
| --- | ---: | ---: | ---: | ---: |
| [Pure return bindings and guard](p151-projected-return-bindings/status.json) | 51 | 4 | 8 / 0 | 197.520 s |
| [Record returns and multiple clauses](p153-projected-record-branches/status.json) | 32 | 11 | 7 / 0 | 190.917 s |
| [Closed entry proof and helper identities](p154-local-proof-and-helper-identities/status.json) | 14 | 2 | 1 / 0 | 48.913 s |

Each scope has zero Equal, Unsupported and SourceRejected. The record extension closes three core-to-SSA functions and v3_core:new_var/2. The finite atom switch closes prim_tty:move/3. The final scope outlines the two discarded lexical list loops in ct_run:rel_to_abs/1 and restores its parent body. Anonymous identity of the protected opaque callback now matches beam_clean's release declaration. Its body and enclosing caller still differ in normalized comparison. The remaining ASN.1 and regular expression branch forms retain non-frame instruction differences.

The final eight managed focused tests and 110 native paired fixture observations passed. Separate native checks matched [terminal cursor output](terminal-paired.terms), [core compiler self-hosting observations](core-compiler-paired-hashes.json), [beam_clean observations](beam-clean-paired.terms) and [discarded helper effects and invalid generators](lexical-list-effects.terms). The first terminal probe missed a per-process NIF load after explicit reload. The corrected run initializes that NIF before exercising cursor movement. The initial [compiler](initial-record-branch-compiler.log), [fixture build](initial-record-fixture-build.log) and [finite clause assertion](initial-closed-switch-fixture.stderr) are retained. See [transferred source identities](source-transfer-2154.json). Whole-corpus acceptance, complete package test report and readiness remain pending.
