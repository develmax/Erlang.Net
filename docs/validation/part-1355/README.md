# Part 1355 — Return tuples after calls with unused results

The general emitter plans ordinary call sequences that return a flat tuple of original inputs and constants. A fresh binding can flow directly from X0 into the next call. The existing frame trimming logic keeps tuple operands live to the return and removes stack slots after their final call argument use. A nonliteral tuple uses a heap check with zero live X registers because the last call result is discarded. A constant tuple is loaded directly as a literal.

A subsequent call can take a reference to a local function with no captures. The code generator reserves one live X register if the previous result is forwarded. It combines the move and closure destination for the simple identity-like callback shape. Other callbacks retain the register swap used by OTP when a fun result annotation intervenes. The rule is based on source and register operands.

The focused fixture compares five decoded functions with label and closure ID normalization. It checks both callback arrangements, effect order, tuple results, and badmatch when a source binding is repeated. Six earlier focused regressions passed.

The case audit covers top-level clauses from the 1,001 images differing in part 1354. A binding followed by a case on a different call appears in 377 modules and 1,016 functions. In the two-expression sequences, 240 modules pass the first result to the selector call and 239 reference it in a branch. These counts overlap. A case that consumes the immediately preceding binding appears in 88 modules. In xmerl_eventp:close/1 the OTP selector stays in X0 and the cons and tuple tests are shared across the branches.

The final Windows OTP 29.1.1 run generated all 1,366 images. It found 365 Equal, 1,001 Different and zero rejected sources. ELDAPv3 is Equal. OTP accepted all 1,001 differing images for loading.

Four images changed. Eight functions gained normalized instruction matches, with no losses. The peak frame for xmerl_xsd_type:check_NMTOKENS/1 fell from two slots to the one OTP uses. Its module's Code size gap narrowed by four bytes. The first run widened the gap for c.beam by building a constant tuple on the heap. The final run restores the literal and matches the corresponding function after label normalization. Full runtime tests and readiness regeneration were not run.

- [Full corpus](otp-corpus.json)
- [Normalized function comparison](normalized-functions.txt)
- [Peak stack slots](peak-slots.txt)
- [Code size changes](code-delta.json)
- [OTP loader check](loader.txt)
- [Initial literal tuple discrepancy](c-code-delta.txt)
- [Final literal tuple instructions](c-final-instructions.txt)

- [Case continuation audit](case-continuations.txt)
- [Case audit script](rank-case-continuations.eval)
- [Bound case instruction comparison](bound-case-example.txt)
- [Two-call case instruction comparison](two-call-case-example.txt)
- [Focused check](check-call-tuple-sequence.ps1)
- [Focused output](probe.txt)
- [Entry tuple binding regression](entry-tuple-binding-regression.txt)
- [Call sequence regression](entry-sequence-regression.txt)
- [Boolean case regression](boolean-case-regression.txt)
- [Closure capture regression](capture-order-regression.txt)
- [Tuple branch regression](tuple-branch-body-regression.txt)
- [Deferred field regression](deferred-tuple-field-regression.txt)
