# Part 1277 — Build a receive exception directly from a register

For a terminal unguarded flat receive branch that raises with a tuple, keep its single live scratch operand in X0. Check the heap before removing the matched message, then construct the exception tuple and tail-call the exception BIF.

The focused test verifies the instruction order and compares the exception value, recursion and timeout with OTP. The matching gl branch no longer spills the tuple payload. The other receive branch still needs four slots in the generated frame.

The full corpus generated all 1,366 images with 359 Equal, 1,007 Different and zero source rejections. ELDAPv3 remains Equal. OTP accepted all differing pairs. Seven generated images changed and their Code lengths stayed the same.

- [Full corpus](otp-corpus.json)
- [Corpus delta](delta.json)
- [OTP loader](loader.txt)
- [Focused execution](probe.txt)
- [Focused command](check-receive-error.ps1)
- [gl instruction comparison](gl-receive.txt)
