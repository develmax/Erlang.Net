# Part 1696

The BEAM writer applies the beam_block move and swap rewrites from OTP 29.1.1. It checks temporary register liveness before folding an exchange. When preparing call arguments it also places a stack/X0 swap before independent moves that neither read nor overwrite the exchanged registers. The pass reuses the BEAM operand reader and compact writer. No dependency was added.

The control fixture now matches the deterministic OTP image byte for byte. The OTP VM returned the expected term for the call chain. The build and all three selected tests passed. [Fixture](../../../tests/Erlang.Tests/Resources/Beam/beam_call_argument_swap.erl) · [Disassembly](fixture-check.txt) · [Tests](focused.txt).

The Windows corpus accepted and compiled all 1,366 sources. 398 images matched, including ELDAPv3 from the C# ASN.1 generator. 968 differed and all were accepted by the OTP loader. 17 images changed. Nine more functions match normalized OTP disassembly. No prior function match was lost. Code section lengths were unchanged. [Corpus](otp-corpus.json) · [Function comparison](normalized-functions.txt) · [Loader and Code](loader-and-code.txt).

Full runtime tests and readiness regeneration were outside this run. An audit of the preceding snapshot found 109 functions across 46 modules where the sole normalized instruction delta was a move of an atom or nil into X0. Several inspected examples are redundant after exact tests.
