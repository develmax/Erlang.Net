# Bounded validation of pre-first-call trailing frame promotion

The confirmed delivery is transient-precall-frame-with-test.patch, SHA-256 b7fbc7e4c383e5fc06cf2594626286cc927183a7d35d1cc590d8fdfcd6408a54. It applies the conservative first-call gate and permanent regression. The optional input alias proposal was not used. The isolated compiler SHA-256 is 1bedb9fcca3c093fbba9da306e98958513c3ef787aa4e1c127cb271c14be0d4a. The base is frozen Part 1853 against Windows OTP 29.1.1 at ad05823719d77c8faee87348ea39513d4e2f99c5. The managed process used BelowNormal priority and a single builder. Compiler build 54.26 s, tiny test runner 4.91 s, corpus cohort 68.03 s. Both builds and SDK style passed.

| Bounded corpus result | Baseline | Candidate |
| --- | ---: | ---: |
| Equal | 3 | 3 |
| Different | 12 | 12 |
| Unsupported | 0 | 0 |
| SourceRejected | 0 | 0 |

The change reaches all seven observed source/destination shapes among 16 selected ranked functions. Fifteen of those shrink. A total of 24 allocations shrink across 11 modules as the fixed-point pass also exposes further reductions. Their live X operands all stay constant. Fifteen post-change allocation operands match pinned OTP exactly. The three normalized gains are cover:flush/1, cover:start/1 and cover:stop/1, with zero losses. None of the modules becomes byte-equal. The changing chunks are Code and derived Attr and FunT.

The permanent test uses the five-field projection shape from the pinned SNMP witness. Its local callee performs explicit garbage collection and hashes the group instead of returning a recoverable copy. The transient case shrinks from two slots to one and the negative case remains at two slots. Both match OTP's allocation and live counts. The test fails the unmodified compiler at its frame assertion and passes the candidate. On pinned ERTS the original, baseline and candidate return equal results for the transient and call-crossing cases with a 2,000-element group value, and preserve function_clause for wrong input. All 22 reference and generated images from the 11 changed modules load and return the expected module_info export counts. No full corpus was launched on this helper candidate.

Raw evidence is in build.log, test-build.log, test-receipt.log, test-red.log, test-green.log, cohort.json, cohort-audit.log, cohort-frames.json, cohort-scope.log, normalized.terms, load.exit, load.terms, and fixture/allocations.log and fixture/*.terms. The original static estimate and source rationale are in README.md.

