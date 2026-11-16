# Part 1049 build direct tail-call lists in X registers

Eligible static unary tail calls construct proper list arguments in X registers. The emitter consumes list elements in reverse order and reuses a register only after its remaining reads. Head checks and guards still run before list construction.

The OTP probe checks repeated arguments, literals and clause selection. The [F, T, Svc] branch matches OTP's heap test, register allocation and cons sequence. OTP folds a trailing run of constants into a literal list, which remains a difference. Diameter_transport:start/3 still falls back to a frame because another clause destructures a tuple and constructs a nested argument.

The corpus has 30 fewer paired functions with an ordinary frame only in generated code. All 1021 differing generated files prepared successfully in OTP. The corpus admitted and generated 1363 of 1366 files. All byte-comparison statuses stayed the same, including ELDAPv3.

- [Full corpus](otp-corpus.json)
- [Frame counts](frames.txt)
- [Loader results](loader.txt)
- [Focused checks](focused.json)
- [Generated execution probe](after.txt)
- [OTP execution probe](otp.txt)
- [Diameter start disassembly](diameter-start.txt)
