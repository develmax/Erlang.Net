# Part 1350 — Emit complex list branches after the entry tests

The shared list and tuple entry planner passes sequence and case bodies to the existing expression emitter. Each such branch plans its own frame and saves its referenced inputs. Fallback paths stay in registers through their last pattern test.

A fresh call result immediately passed to an ordinary tail call uses the existing call chain plan. A tuple tag already tested in the destination register can serve as a tail-call argument without a second move.

The focused fixture matches the decoded run/1 instructions after label normalization. Its frame/1 probe verifies the shared entry tests, both case results, expression sequences and original input preservation on mismatch. The previous C# version allocated eight slots on every path. The current C# version allocates five and six slots in its two complex branches. OTP uses one and three slots there. The fixture also calls closures returned from arity branches emitted in reverse source order. The deferred field, tuple arity, literal cons, call chain and mixed cons regressions passed.

The full Windows OTP 29.1.1 run generated all 1,366 images with 365 Equal, 1,001 Different and zero SourceRejected. In 411 functions across 199 modules, the generated allocation moved away from entry where OTP also begins without allocation. The Code size gap narrowed in 206 modules and widened in 21, with a net reduction of 11,634 bytes. OTP accepted all differing images for loading.

Of the 256 changed images, leex gained a normalized instruction match in inc_file_name/1. Eight lifted functions in dbg_ieval and erl_eval lost normalized matches. Closure emission orders captures by their Y slots, so changes in the branch frame can change the lifted function's argument order. Match the OTP capture order while retaining branch-local allocation.

- [Focused check](check-tuple-branch-body.ps1)
- [Focused output](probe.txt)
- [Previous probe allocation](previous-probe-frame.txt)
- [Deferred field regression](deferred-tuple-field-regression.txt)
- [Tuple arity regression](tuple-head-dispatch-regression.txt)
- [Literal cons regression](cons-literal-prefix-regression.txt)
- [Call chain regression](entry-call-chain-regression.txt)
- [Mixed cons regression](mixed-cons-prefix-regression.txt)
- [Full corpus](otp-corpus.json)
- [Entry frame changes](entry-frames.txt)
- [Entry frame audit script](compare-entry-frames.eval)
- [Normalized instruction comparison](normalized-functions.txt)
- [Code size delta](code-delta.json)
- [OTP loader](loader.txt)
