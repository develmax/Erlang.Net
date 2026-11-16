# Part 1320

A clause that matches one tuple can construct a tuple argument and tail-call without allocating a stack frame. The planner reuses the matched tuple register for its last field when that register holds the constructed argument and the field is consumed by the constructor alone. Guard failure and an extra call argument can keep the field in a separate register.

The focused OTP check matches the full route/2 instruction stream and executes the fallback, guard, and extra argument cases. The full corpus produced 1,366 images with 363 Equal, 1,003 Different and zero SourceRejected. Five release Code size gaps narrowed by a combined 272 bytes. No release function gained or lost an instruction match after label normalization. OTP accepted all 1,003 differing images, and ELDAPv3 remained Equal.

In net_kernel:verbose/3, the reference reuses the tuple register for the guarded field and passes one live register to test_heap. In ct_framework:required_default/5, OTP moves the tuple clause ahead of the atom clause and extracts into the outgoing tuple register.

- [Focused check](check-tuple-entry-tail.ps1)
- [Focused output](probe.txt)
- [Full corpus](otp-corpus.json)
- [Changed images](delta.json)
- [OTP loader](loader.txt)
- [Guarded tuple example](net-kernel-verbose.txt)
- [Tuple and atom dispatch example](ct-required-default.txt)
