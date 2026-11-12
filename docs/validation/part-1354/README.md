# Part 1354 — Return call results directly in tuples

The separate entry-frame planner accepts a fresh variable bound to an ordinary call and used in a flat tuple result. It uses the existing argument planner and tuple emitter. The call result remains in X0 for every occurrence in the result. The frame stores only values used across the call.

For an unguarded single tuple pattern with no pending field comparisons, the last field can be extracted into X0 after allocation when it is the sole call argument and the result does not use it. Earlier fields and all other inputs retain their existing order. A subsequent clause can return a flat tuple directly from the incoming registers.

The focused fixture checks list and tuple clauses, repeated use of the call result, and fallback results. Both functions match decoded OTP 29.1.1 instructions after label normalization. Five earlier focused regressions passed.

The tuple continuation audit covers top-level clauses in the 1,001 differing images from part 1353. It finds a sole bound call and flat use of the result in 138 modules. The largest explicit call group has 648 functions in 243 modules where an effectful call's result is discarded immediately before tuple construction. Counts describe overlapping syntax groups. In check_NMTOKENS/1, OTP constructs the foreach callback in X1 while the tokens result remains in X0. The C# image saves that result in a separate Y slot before constructing the callback.

The full Windows OTP 29.1.1 run generated all 1,366 images. It found 365 Equal, 1,001 Different and zero rejected sources. ELDAPv3 is Equal. OTP accepted all 1,001 differing images for loading.

Four images changed. Peak frame sizes in tls_client_ticket_store:handle_cast/2, array:grow/3, gb_sets:delete_1/2 and gb_trees:delete_1/2 now match OTP. The absolute Code size gaps narrowed by 49 bytes in total. No full function gained or lost a normalized instruction match. Full runtime tests and readiness regeneration were not run.

- [Full corpus](otp-corpus.json)
- [Peak stack slots](peak-slots.txt)
- [Code size changes](code-delta.json)
- [Normalized function comparison](normalized-functions.txt)
- [OTP loader check](loader.txt)
- [Focused check](check-entry-tuple-binding.ps1)
- [Focused output](probe.txt)
- [Call sequence regression](entry-sequence-regression.txt)
- [Boolean case regression](boolean-case-regression.txt)
- [Closure capture regression](capture-order-regression.txt)
- [Tuple branch regression](tuple-branch-body-regression.txt)
- [Deferred field regression](deferred-tuple-field-regression.txt)
- [Tuple continuation audit](tuple-continuations.txt)
- [Audit script](rank-tuple-continuations.eval)
- [Discarded call example](discarded-call-example.txt)
