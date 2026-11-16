# Part 1697

The BEAM writer removes moves of atoms, nil and integers already proven by exact tests or unambiguous select_val edges. A selection label must have consistent explicit incoming values and no fallthrough. Unknown instructions and writes to the tracked register invalidate the fact. The change reuses the existing BEAM reader and follows the value tracking in OTP 29.1.1 beam_jump. No dependency was added.

The full Windows corpus accepted and compiled all 1,366 sources. 398 images matched byte for byte, including ELDAPv3 from the C# ASN.1 generator. The OTP loader accepted all 968 differing images. 429 images changed compared with Part 1696. 129 additional functions match normalized OTP instructions and no prior match was lost. [Corpus](otp-corpus.json) · [Function comparison](normalized-functions.txt) · [Loader and Code](loader-and-code.txt).

The permanent control fixture matches the deterministic OTP image byte for byte. The OTP VM also checked both result arms of an exact guard, nil and nonempty input, and a shared select_val target with different keys. The build had zero warnings and three focused tests passed. Full runtime tests and readiness regeneration were outside this run. [Fixture](../../../tests/Erlang.Tests/Resources/Beam/beam_proven_value_returns.erl) · [Disassembly](fixture-check.txt) · [Tests](focused.txt).
