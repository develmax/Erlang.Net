# Part 1425 — Share two-cell prefixes and nested tuple returns

The compiler extracts a shared first cell, checks its literal, and dispatches on the second literal. It preserves the first tail and the original head for the variable fallback. A call nested in a returned tuple uses the saved-register construction plan to keep only the values needed after the call and allocate both tuples with one heap check.

The deterministic fixture matches OTP byte for byte. Execution checks cover both prefix keys, unmatched and partial prefixes, an improper list, and both tuple layers. The previous compiler produces different bytes. All three focused compiler tests passed.

All 1,366 Windows OTP 29.1.1 release images were generated with zero source or generation failures. The corpus has 382 Equal and 984 Different. ELDAPv3 remains Equal from C#-generated Erlang. OTP prepares all 984 differing images for loading. Twenty-three release images change. xref_parser:type/2 gains normalized instruction equality, twelve frames reach the OTP size, and the total Code gap shrinks by 921 bytes.

The entry in collect_quoted/3 now matches through the dispatch. Its selector branch still allocates six slots where OTP allocates three. The successful paths in collect_string/3 match, with the first remaining difference in the arguments to its end-of-input throw. An opcode audit finds 585 functions across 162 modules with a candidate throw-to-raise difference. The audit is a candidate list and each replacement requires a handler proof.

The separate Part 1423 full regression ended with 9,518 passed and 182 failed. Its three compiler failures reproduce on both Part 1422 and Part 1425. Canonical readiness awaits a passing full report and retains the immutable Part 561 comparison snapshot.

- [Corpus](otp-corpus.json), [instruction matches](normalized-functions.txt), [frame delta](frame-delta.txt), and [loader and Code sizes](loader-and-code.txt)
- [Token instructions before](token-before.txt), [current](token-current.txt), and [summary](token-summary.txt)
- [Fixture oracle](check-list-token-prefix.ps1), [result](probe.txt), [previous compiler](previous-compiler.txt), [hashes](fixture-hashes.txt), and [focused tests](focused.json)
- [List-entry ranking](list-entry-ranking.txt), [throw candidates](throw-candidates.txt), and [audit script](throw-candidates.escript)
- [Prior full regression](prior-full-summary.txt), [older compiler replay](older-compiler-failure-replay.json), and [current compiler replay](current-compiler-failure-replay.json)
