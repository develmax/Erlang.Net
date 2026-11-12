# Part 1424 — Share mixed list entry across conversion branches

The compiler extracts the list head and tail once across tuple, atom, guard and variable clauses. The two conversion branches allocate their own frames and pass the converted cons head to the tail call. For a case on a single literal prefix followed by a call result, the compiler compares the suffix and constructs the prefixed value in the returning branch. The nil path checks the list input before testing other arguments.

The deterministic fixture matches OTP byte for byte. Both binaries execute the skip and conversion branches, empty and invalid list inputs, the prefixed case branches and a producer with an improper suffix. A separate probe verifies that a non-list later argument raises function_clause. The test harness includes that case.

All 1,366 Windows OTP 29.1.1 release images were generated. The corpus has 382 Equal, 984 Different and zero source or generation failures. ELDAPv3 remains Equal from C#-generated Erlang. OTP prepares all 984 differing images for loading. Thirty-five release images change, and asn1ct_imm:bit_flags/2 and sofs:strict/2 match after label normalization. Eighteen function frames move closer to OTP and three reach its size. The total Code size gap shrinks by 644 bytes.

The largest remaining group covers 215 functions in 131 modules. The next candidates are the quoted and string token collectors and the caught record branch in componentrelation_leadingattr/5.

- [Corpus](otp-corpus.json), [instruction matches](normalized-functions.txt), [frame delta](frame-delta.txt), and [loader and Code sizes](loader-and-code.txt)
- [bit_flags instructions](bit-flags.txt), [list-entry ranking](list-entry-ranking.txt), and [later list argument](late-list-input.txt)
- [Fixture oracle](check-list-cons-conversion.ps1), [result](probe.txt), [previous compiler](previous-compiler.txt), [hashes](fixture-hashes.txt), and [focused tests](focused.json)
- [Replay of the first five prior full-suite failures on the older compiler](previous-regression-replay.json)
