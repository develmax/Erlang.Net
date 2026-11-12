# Part 1353 — Forward consecutive call results

The call-chain planner accepts consecutive call bindings when each result has exactly one use in the next call, directly or as the member of a singleton list. It preserves source call order and uses the existing frame trim and slot relocation logic. Other list branches can take the first ordinary call's operands from X registers and save only the original values referenced by the remaining expressions.

The focused fixture matches the decoded walk/2 and pack/1 instructions after label normalization. Its branch/1 probe checks that an operand used only by the first call stays out of Y registers. The fixture also checks the fallback values. The boolean case, capture order, complex branch, deferred field, tuple arity and call chain regressions passed.

The source audit scans top-level clauses in the previous 1,001 differing images. It found 161 linear forwarding functions across 97 modules. Another group has 437 functions across 229 modules with call-only sequences that require more general scheduling. The value-returning group starts with a call binding in 672 modules, including tuple results in 313 and case results in 312. These counts describe source shapes and can overlap.

The full corpus generated all 1,366 images. The byte comparison found 365 Equal and 1,001 Different, with zero rejected sources. ELDAPv3 is Equal. OTP accepted every differing image for loading. At least two section lengths differ in 989 images, including a Code size difference in all 1,001 and a Type size difference in 901.

Across the 163 changed images, 140 functions in 96 modules gained a normalized instruction match and none lost a match. Peak stack slots decreased in 150 functions across 106 modules, reaching the OTP count in 87. All 411 functions with previously delayed allocation retained it and one gained it. Code size gaps narrowed in 114 modules and widened in three, reducing the total absolute size gap by 1,461 bytes. Full runtime and readiness checks were not run.

- [Full corpus](otp-corpus.json)
- [Normalized function comparison](normalized-functions.txt)
- [Peak stack slots](peak-slots.txt)
- [Delayed allocation comparison](frame-preservation.txt)
- [Code size changes](code-delta.json)
- [OTP loader check](loader.txt)
- [Focused check](check-entry-sequence.ps1)
- [Focused output](probe.txt)
- [Boolean case regression](boolean-case-regression.txt)
- [Capture regression](capture-order-regression.txt)
- [Complex branch regression](tuple-branch-body-regression.txt)
- [Deferred field regression](deferred-tuple-field-regression.txt)
- [Tuple arity regression](tuple-head-dispatch-regression.txt)
- [Call chain regression](entry-call-chain-regression.txt)
- [Source sequence audit](call-sequences.txt)
- [Audit script](rank-call-sequences.eval)
