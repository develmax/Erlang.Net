# Part 1423 — Share record-head dispatch for a rebuilt list argument

The compiler tests and extracts the first list cell once. It checks the record and dispatches on its scalar or tagged tuple field. In an update branch it checks the second list item, copies its fields into the first record and constructs the recursive cons argument in registers. An absent second item reaches the ordinary list branch. An invalid second record raises badrecord.

The deterministic fixture matches OTP byte for byte. Both images execute all three update branches, the singleton and empty list cases, an unrelated record field, an invalid second item and an invalid list input. The previous compiler produces different bytes.

The Windows OTP 29.1.1 run generated all 1,366 images with zero source rejections and emission failures. There are 382 Equal and 984 Different. ELDAPv3 remains Equal from C#-generated Erlang. OTP prepares all 984 differing images for loading. Only asn1ct_check changes. Its merge_tags2/2 matches OTP after label normalization, with 61 instructions instead of 156 and no stack frame instead of 12 slots. The Code gap shrinks by 386 bytes.

The remaining list-entry work includes mixed tuple and atom checks before guarded call branches in bit_flags/2, and deferring the frame until after the shared list and record tests in componentrelation_leadingattr/5.

The three focused compiler tests passed with the same compiler assembly used for the corpus. The full local regression run is still in progress. Canonical readiness regeneration awaits its completed report. The comparison snapshot remains part 561.

- [Corpus](otp-corpus.json), [normalized instructions](normalized-functions.txt), [frame delta](frame-delta.txt), and [loader and Code sizes](loader-and-code.txt)
- [ASN.1 before](asn1-before.txt), [current](asn1-current.txt), and [function comparison](merge-tags2-summary.txt)
- [Fixture oracle](check-list-record-merge.ps1), [result](probe.txt), [previous compiler](previous-compiler.txt), and [hashes](fixture-hashes.txt)
- [Focused compiler tests](focused.json) and [compiler hashes](compiler-hash.txt)
- [List-entry ranking](list-entry-ranking.txt) and [bit_flags instructions](bit-flags.txt)
