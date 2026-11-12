# Part 1044 OTP register move chains

For direct tail calls with register arguments, the general fallback collects move chains in destination order, breaks cycles into swaps, and emits independent chains in reverse order. The existing constant and specialized lowering paths retain their established scheduling. The reference is setup_args and order_moves in OTP 29.1.1 beam_ssa_codegen.erl.

Three probes executed in OTP and returned the expected tuples. The decoded lifted bodies match OTP for a dependent move chain, two independent swaps, and a cycle of three registers.

The corpus tail comparison pairs lifted functions by name and arity. It compares decoded moves, swaps and direct tail calls while dropping labels, lines and headers. It found 59 new body matches, 2001 retained matches and 104 remaining differences. It found zero lost body matches in this subset.

The full corpus admitted and generated 1363 of 1366 files. All per-file byte comparison statuses remain unchanged at 342 Equal, 1021 Different and 3 SourceRejected. ELDAPv3 remains Equal. The 1021 differing files pass OTP loader preparation.

- [Full corpus](otp-corpus.json)
- [Focused checks](focused.json)
- [Lifted body comparison](tail-bodies.txt)
- [Loader results](loader.txt)
- [Dependent move probe](chain.txt)
- [Independent swaps probe](swaps.txt)
- [Three-register cycle probe](cycle.txt)
