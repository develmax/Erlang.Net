# Part 1349 — Defer a tuple field read until the call

The list-head planner leaves the last tuple field in the tuple when its only use is a unary call that builds the result list head. It reads that field into X0 after allocation and preserves the tuple through the earlier tag and guard checks. The frame holds the surviving result fields and tail in OTP order. If only a nil clause follows the arity branches, failed tuple checks go to function_clause directly. An empty-list result can reuse the tested X0 value.

The focused fixture matches the decoded normalize/1 instructions after label normalization. It checks successful branches, failure with the original argument in the stack and a variant whose guard reads the field. The tuple dispatch, literal cons, mixed cons and computed cons regressions passed.

The full Windows OTP 29.1.1 run generated all 1,366 images with 365 Equal, 1,001 Different and zero SourceRejected. One image changed. xmerl_lib:simplify_attributes/1 now matches decoded OTP instructions after label normalization, and the xmerl_lib Code size gap narrowed by six bytes. OTP accepted all 1,001 differing images for loading.

The flat tuple source audit identifies 103 functions across 65 modules with an expression sequence in at least one tuple-headed clause. It found a case result in 67 functions across 49 modules and an ordinary tail call in 65 functions across 47 modules. The groups overlap. Supporting the body emitter after the shared entry tests covers cases currently outside the standalone call planners.

- [Focused check](check-deferred-tuple-field.ps1)
- [Focused output](probe.txt)
- [Tuple dispatch regression](tuple-head-dispatch-regression.txt)
- [Literal cons regression](cons-literal-prefix-regression.txt)
- [Mixed cons regression](mixed-cons-prefix-regression.txt)
- [Computed cons regression](computed-cons-head-regression.txt)
- [Full corpus](otp-corpus.json)
- [Normalized instruction comparison](normalized-functions.txt)
- [simplify_attributes/1 instructions](simplify-attributes.txt)
- [Code size delta](code-delta.json)
- [OTP loader](loader.txt)
- [Flat tuple body audit](flat-tuple-bodies.txt)
- [Audit script](rank-flat-tuple-bodies.eval)
