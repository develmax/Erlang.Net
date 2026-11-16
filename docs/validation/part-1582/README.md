# Part 1582 — Carry binary guard proofs into body matches

A successful is_binary guard establishes the source type for direct matches in the same clause body. Fixed and dynamic-sized binary patterns use bs_start_match4 no_fail with this proof. For a fixed byte-aligned prefix the initial length check also drops the redundant alignment test. The rule requires the actual Erlang BIF and declines disjunctive guards.

The fixture matches the OTP entry opcode for split/2. The candidate still has a different frame and register layout. The disjunctive optional/2 keeps the checked entry. OTP VM execution agreed on four outcomes, including an empty tail and badmatch with the original value. [Source](beam_guarded_body_binary.erl) · [Split instructions](split.txt) · [Disjunction](optional.txt) · [Outcomes](outcomes.txt).

Three focused tests passed, including managed execution of both guarded paths. All 1,366 sources were accepted and generated. The comparison found 390 Equal and 976 Different, with ELDAPv3 matching from the C# ASN.1 generator. The rule changed eight release images. OTP accepted all 976 differing images for loading. The full runtime suite and canonical readiness were outside this run. [Tests](focused.txt) · [Corpus](otp-corpus.json) · [Changed images](changed.txt) · [Loader](loader-and-code.txt).
