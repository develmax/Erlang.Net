# Part 1321

Reuse the input tuple register for a single guard field if the remaining clause has wildcard arguments and returns a literal. After guard success, derive the live prefix for tuple construction from its inputs and the outgoing call arguments.

The focused comparison matches the full route/3 instruction stream. The test also verifies guard failure, tuple tag failure, and preservation of the original tuple when the fallback returns it. In net_kernel:verbose/3 the extracted field register and the test_heap live count now match OTP. The remaining instruction difference is the type annotation for the Level operand.

The full Windows OTP 29.1.1 corpus produced 1,366 images with 363 Equal, 1,003 Different and zero SourceRejected. net_kernel was the only generated image whose bytes changed compared with Part 1320. The reported chunk lengths stayed the same. OTP accepted all differing images. ELDAPv3 remained Equal.

- [Focused check](check-guarded-tuple-tail.ps1)
- [Focused output](probe.txt)
- [net_kernel comparison](net-kernel-verbose.txt)
- [Changed instruction streams](changed-instructions.txt)
- [Full corpus](otp-corpus.json)
- [OTP loader](loader.txt)
