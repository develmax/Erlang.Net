# Part 1274 — Forward an immediately consumed receive call result

For a terminal receive sequence, inline a fresh call result into the next expression when its only use follows a unary static call or the last field of a constant-prefix tuple or list. Preserve the binding when the rest of the sequence reads it, including in a pattern.

The focused probe verifies adjacent producer and consumer calls, the order of their observable effects and a variant that must preserve the binding. In gl:rec/1 the result of io_lib:format/2 reaches lists:flatten/1 without a stack store. The frame falls from six slots to five. OTP uses one slot.

The full corpus generated all 1,366 images with 359 Equal, 1,007 Different and zero source rejections. ELDAPv3 remains Equal. OTP accepted all differing pairs. The three changed images all moved closer by Code size, by 10 bytes in aggregate.

- [Full corpus](otp-corpus.json)
- [Corpus delta](delta.json)
- [OTP loader](loader.txt)
- [Focused execution](probe.txt)
- [Focused command](check-receive-forward.ps1)
- [gl instruction comparison](gl-receive.txt)
