# Part 1567 — Fold constant fields in tagged call results

The compiler substitutes a field in a terminal tagged tuple case when every matching return from the local producer proves the same literal. Other tags and arities are excluded. Unknown return paths and recursion prevent substitution. The call and tuple test still run. This follows singleton tuple field folding in OTP 29.1.1 beam_ssa_type.erl and reuses the existing literal and branch encoders.

The fixture emits the same instructions as OTP after normalizing source filenames. Three execution cases and three selected compiler tests passed. [Fixture](../part-1566/beam_bound_case_selector.erl) · [Before](run-before.txt) · [After](run-after.txt) · [Outcomes](fixture.txt) · [Tests](focused.json).

All 1,366 sources were accepted and generated. The comparison found 390 Equal and 976 Different, including an exact match for ELDAPv3 from C# ASN.1 generation. The new rule changed no release images. OTP accepted all 976 differing images for loading. The instruction audit shows only one differing function each in ssh_bits and erl_abstract_code. The full runtime suite and canonical readiness were not rerun. [Corpus](otp-corpus.json) · [Loader](loader-and-code.txt) · [Closest modules](near.txt).
