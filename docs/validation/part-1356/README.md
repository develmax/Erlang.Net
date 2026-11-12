# Part 1356 — Plan register cases after calls

The register case planner accepts a call bound immediately before a case. If the case tests that value directly, the planner uses X0 when the branches do not need a separate binding. If the selector is a second ordinary call, it reads the first result from X0 and saves it only when a branch uses it. A stack slot last used as the selector's first argument can hold the first result after a swap. Other saved values and argument order are preserved.

For a case selected by trunc/1, round/1, floor/1 or ceil/1, the original number can remain in X0 and the integer result goes to X1. The comparison and arithmetic use the proven operand types. Singleton list patterns are checked before stack allocation. Tuple results reuse the existing heap and operand planner with the actual live register.

The focused fixture compares six functions with OTP after label normalization and checks ordinary selectors, preserving and recycling a stack slot, singleton list failure, badarg, rebinding, and a shadowed rounding function. Six earlier focused regressions passed.

The source audit groups top-level cases in functions whose normalized instructions differ, using the 1,001 images from part 1355. Cases without an unguarded variable arm occur in 735 modules. Literal-only arms appear in 488 modules, including true/false pairs in 450. Nonboolean literal cases appear in 131 modules, with simple results in 28. The groups can overlap. Cases with multiple cons arms occur in 98 modules.

The final Windows OTP 29.1.1 corpus run generated all 1,366 images. It found 365 Equal, 1,001 Different and zero rejected sources. ELDAPv3 is Equal. OTP accepted all 1,001 differing images for loading.

Fourteen images changed. Twelve functions gained normalized instruction matches, including xmerl_xpath_pred:ceiling/2 and floor/2, and none lost a match. Peak stack slots fell in 15 functions and reached the OTP count in 13. The Code size gaps narrowed in all 14 changed images by 278 bytes in total. Full runtime tests and readiness regeneration were not run.

- [Full corpus](otp-corpus.json)
- [Normalized function comparison](normalized-functions.txt)
- [Peak stack slots](peak-slots.txt)
- [Code size changes](code-delta.json)
- [OTP loader check](loader.txt)
- [Literal case instruction examples](literal-case-example.txt)
- [Focused check](check-two-call-case.ps1)
- [Focused output](probe.txt)
- [Call tuple sequence regression](call-tuple-sequence-regression.txt)
- [Entry tuple binding regression](entry-tuple-binding-regression.txt)
- [Call sequence regression](entry-sequence-regression.txt)
- [Boolean case regression](boolean-case-regression.txt)
- [Closure capture regression](capture-order-regression.txt)
- [Tuple branch regression](tuple-branch-body-regression.txt)
- [Case pattern audit](case-list-patterns.txt)
- [Case audit script](rank-case-list-patterns.eval)
