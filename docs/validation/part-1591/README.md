# Part 1591 — Relocate live checked-call arguments before trimming frames

A checked call chain can relocate its one retained value into a dying input slot after preparing the other arguments. It trims the dead prefix, then loads X0 from the relocated value. The final call can trim the remaining slot after loading it. The move and trim sequence follows the relocation recipe in OTP 29.1.1 beam_trim.erl. The planner reuses the existing relocation and move scheduler. No dependency was added.

Three focused tests passed. The managed regression verifies successful reuse and a failed call-result check after the trim. OTP VM execution agrees on both results. The fixture's whole image still differs because OTP can prove the fixture payload is constant. [Fixture](beam_trimmed_checked_payload.erl) · [Disassembly](fixture-disassembly.txt) · [Outcomes](outcomes.txt) · [Tests](focused.txt).

All 1,366 release sources were accepted and generated. There were 391 Equal and 975 Different. The sole changed release image, wx_misc, now matches byte for byte, including metadata. ELDAPv3 remains Equal from the C# ASN.1 generator. OTP accepted all 975 differing images for loading. The full runtime suite and canonical readiness were outside this run. [Corpus](otp-corpus.json) · [Status changes](status-changes.txt) · [Loader and Code](loader-and-code.txt).
