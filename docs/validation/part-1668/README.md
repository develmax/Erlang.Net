# Part 1668

The compiler recognizes direct list comprehension results as proper lists. In a private function receiving only proper lists, the empty fallback can omit a redundant nil test. Failures of tests on the cons head branch to function_clause. This mirrors the type reasoning in OTP 29.1.1 beam_ssa_type. The existing call-site proof and entry emitter are reused.

The fixture now matches the OTP empty branch for the proven input and retains the nil test for unrestricted input. Cons extraction and frame allocation still differ. The OTP VM verified both images and three focused checks passed. [Fixture](beam_proper_cons_entry.erl) · [Instructions](disassembly.txt) · [VM](outcomes.txt) · [Tests](focused.txt).

All 1,366 release sources were accepted and compiled. The comparison recorded 398 Equal and 968 Different. ELDAPv3 remains byte-identical. Only xmerl_xsd changed. Its Code section moved eight bytes closer to OTP. The OTP loader accepted each differing image. [Corpus](otp-corpus.json) · [Code and loader](loader-and-code.txt) · [Functions](normalized-functions.txt).

The full runtime suite and readiness regeneration were outside the validation scope.
