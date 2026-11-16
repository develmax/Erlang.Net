# Part 1304 — Match compound entry guards and the complete glu image

Lower three guard alternatives with float tests and tuple arity comparisons before stack allocation. Preserve failure fallthrough for non-tuples and combine the boolean results in OTP order. No dependency or license changes.

- [Guard execution against OTP](probe.txt)
- [Comparison of all 30 glu functions](glu-wrappers.txt)
- [Unnormalized instructions, chunks and SHA-256](glu-image.txt)
- [Full corpus](otp-corpus.json)
- [Corpus delta](delta.json)
- [OTP loader check](loader.txt)
- [Remaining differences by chunk](chunk-incidence.txt)

All 1,366 sources generated images. The corpus has 361 Equal, 1,005 Different and zero rejected sources. The only image that changed was glu, which now matches the release. OTP accepted all 1,005 differing images. The full runtime suite, readiness regeneration and CI were outside this check.

The opcode audit found 946 modules with at least one function whose first opcode difference is a test in OTP and stack allocation in the generated code. [Opcode counts and examples](opcode-incidence.txt).
