# Part 1308 — Extract tuple fields into their final registers

For an unguarded pair clause returning a unary call result and the retained field, allocate the frame after the tuple tests. Extract the call input into X1 and the retained field directly into Y0. A tuple that fails either test returns through the original-argument fallback without a frame.

- [Complete xmerl_xsd:count_occur/1 comparison](xmerl-xsd-final.txt)
- [Focused instructions and execution](probe.txt)
- [Focused command](check-tuple-field-call.ps1)
- [Guarded tuple regression check](guarded-tuple.txt)
- [List head and tail forms in the previous corpus](multi-clause-list-shapes.txt)

The complete count_occur/1 instruction stream matches OTP 29.1.1 after normalizing local labels. The focused case matches the original instructions and checks success and fallthrough. The existing guarded tuple probe also passes. No dependencies changed.

The full corpus generated all 1,366 release files. The counts are 361 Equal, 1,005 Different and zero source rejections. Only xmerl_xsd changed, and its Code size gap decreased by three bytes. OTP accepted all differing images.

- [Corpus](otp-corpus.json)
- [Delta](delta.json)
- [OTP loader](loader.txt)
- [Body categories for nonempty-list branches](list-branch-bodies.txt)
