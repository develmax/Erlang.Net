# Part 1421 — Share list entry with a guarded record case

The compiler uses one nonempty-list test and one extraction for both list branches. It tests the record head before reading the selected field. If the record or field test fails, it runs the shared cons tail. The selected and empty branches each use the saved-call planner and allocate only the slots needed by their own calls.

The deterministic fixture matches OTP as a complete image. Both binaries pass execution checks for the selected branch, the ordinary sibling, a record with a different tag, an empty input, and an improper list. The previous compiler differs on the fixture. All three selected compiler tests passed.

The Windows OTP 29.1.1 comparison generated all 1,366 images with zero source rejections. There are 382 Equal and 984 Different. ELDAPv3 is Equal from C#-generated Erlang. OTP prepares all 984 differing images for loading. Only asn1ct_check changed. Its check_unique_sequence_tags1/3 now matches OTP after label normalization. The largest frame shrinks from five slots to two. The Code gap shrinks by 105 bytes.

The remaining list-entry ranking still prioritizes a variable list head and calls with cons arguments. The next case, remove_name_collisions2/4, uses a call on the head before a second call selects a tuple branch. Extend the shared saved-call plan to carry those prefix values into the selector. Full runtime verification, canonical readiness regeneration and publication remain pending.

- [Corpus](otp-corpus.json), [normalized instructions](normalized-functions.txt), [frame delta](frame-delta.txt) and [loader and Code sizes](loader-and-code.txt)
- [ASN.1 reference instructions](asn1-guarded-list-case.txt), [list-entry ranking](list-entry-ranking.txt) and [next selector prefix](list-selector-prefix.txt)
- [Oracle](check-record-guard-list.ps1), [probe](probe.txt), [previous compiler](previous-compiler.txt), [fixture hashes](fixture-hashes.txt) and [focused tests](focused.json)
