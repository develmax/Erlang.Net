# Part 1232 — propagate receive timeouts along unchanged recursive arguments

The compiler can use a dominating integer match as a caller fact and carry that fact along a direct recursive edge that forwards the same parameter. The lexical scan follows supported branch bodies and skips nested function scopes. A recursive edge that changes the value still participates in the agreement check. The existing receive frame and argument passing remain in use.

The OTP execution probe verifies both cases. The unchanged argument becomes an immediate timeout even when the initial call appears inside a case and try expression. The conflicting argument retains a register operand.

In all five xmerl SAX parser images, receive_msg/4 uses the same immediate timeout of 10000 as OTP. The longer operand encoding increases each Code size gap by two bytes while the frame and recursive call arguments stay in place. The full Release corpus generated 1,366 images, with 357 Equal, 1,009 Different and zero rejected. OTP accepted all differing image pairs.

- [Execution probe](probe.txt)
- [Probe source](recursive_timeout_probe.erl)
- [Release timeout operands](xmerl-timeouts.txt)
- [Full corpus](otp-corpus.json)
- [Corpus delta](delta.json)
- [OTP loader](loader.txt)
