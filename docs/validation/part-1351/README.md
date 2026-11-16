# Part 1351 — Preserve capture order and keep case inputs in X registers

A list tail remains a distinct pattern binding when extraction overwrites the list input register. The branch emitter supplies the binding order to closure emission. Captures and lifted parameters use that order while the branch frame keeps its physical slot layout.

A case with an ordinary selector call can use the existing register case planner after the outer list and tuple checks. The planner saves inputs used by the case arms and takes selector-only operands from the X registers.

The focused fixture verifies both closures, the nil branch, both case results and the original input on mismatch. Both lifted functions and choose/1 match decoded OTP instructions after label normalization. The case branch allocates one slot. The earlier compiler failed the capture-only instruction comparison. The complex branch, deferred field, tuple dispatch, literal cons and call chain regressions passed.

Before this change, 343 of the 411 functions whose allocation moved away from entry still had a larger peak frame than OTP. These functions span 182 modules. The case planner is one way to reduce this gap when only some inputs survive the first call.

The full Windows OTP 29.1.1 run generated all 1,366 images with 365 Equal, 1,001 Different and zero SourceRejected. Nine images changed. All eight lifted functions in dbg_ieval and erl_eval recovered their normalized instruction matches. The 411 functions with allocation moved away from entry are the same as in part 1350.

Peak stack slots fell in seven functions across six modules. Six of those functions now have the OTP peak. The Code size gap narrowed in seven modules and widened by nine bytes in erl_eval and six in dbg_ieval, for a net reduction of 176 bytes. OTP accepted all differing images for loading.

- [Focused check](check-capture-order.ps1)
- [Focused output](probe.txt)
- [Previous capture comparison](previous-probe.txt)
- [Complex branch regression](tuple-branch-body-regression.txt)
- [Deferred field regression](deferred-tuple-field-regression.txt)
- [Tuple dispatch regression](tuple-head-dispatch-regression.txt)
- [Literal cons regression](cons-literal-prefix-regression.txt)
- [Call chain regression](entry-call-chain-regression.txt)
- [Previous peak slot gap](entry-slot-gap.json)
- [Full corpus](otp-corpus.json)
- [Normalized instruction comparison](normalized-functions.txt)
- [Entry frame preservation](frame-preservation.txt)
- [Peak slot changes](peak-slots.txt)
- [Peak slot audit script](compare-peak-slots.eval)
- [Code size delta](code-delta.json)
- [OTP loader](loader.txt)
