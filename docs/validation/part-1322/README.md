# Part 1322

Emit a bounded integer operand for an entry ordering guard when all known direct calls to the private function prove a non-singleton range. The original parameter index supplies the range and the guard's register mapping supplies the X register. Exported functions, function references, and native entries retain general operands.

The focused test matches the full route/3 instruction stream. It checks both integer bounds, guard failure, and calls outside that range through the exported and indirect paths. The release net_kernel:verbose/3 now matches all instructions after label normalization.

The full Windows corpus generated all 1,366 images with 363 Equal, 1,003 Different and zero SourceRejected. Only net_kernel changed compared with Part 1321. The OTP loader accepted all differing images. ELDAPv3 remained Equal.

- [Focused check](check-integer-guard-range.ps1)
- [Focused output](probe.txt)
- [Release function comparison](net-kernel-verbose.txt)
- [Full corpus](otp-corpus.json)
- [Changed image](delta.json)
- [Instruction match](normalized-functions.txt)
- [OTP loader](loader.txt)
