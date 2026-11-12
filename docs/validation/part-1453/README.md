# Part 1453 — Lower predicate filters in list comprehension helpers

The compiler extracts a single variable generator with a static predicate call or its negation. It preserves the head and tail across the call. A rejected element takes the recursive tail call, while an accepted element is kept directly or passed through the existing mapper. A proven boolean result uses one exact comparison. For other results, the compiler distinguishes bad_filter from the badarg raised by not.

The OTP fixture verifies known and unknown boolean results, negation, filtering before mapping, predicate exceptions and improper tails. All five helper bodies match after rebasing labels. The three focused .NET tests passed. In asn1ct:remove_asn_flags/1, the parent now tail-calls the helper without a frame, and the helper holds the head and tail in two slots.

- [OTP fixture](probe.txt), [ASN.1 instruction comparison](asn1ct-remove-flags.txt), [focused tests](focused.json), and [compiler hashes](compiler-hash.txt)


The full Windows OTP 29.1.1 run generated all 1,366 release images with zero source rejections. It reports 382 Equal and 984 Different, with no status transitions. ELDAPv3 from the C# ASN.1 generator remains Equal. Both loaders accept all 984 differing images. Five images changed and six functions gain normalized instruction equality. The aggregate absolute Code size gap falls by 270 bytes.

Six differing images now have exactly one differing function after normalization, compared with five before this change. The newly ranked image is erl_abstract_code.beam, where debug_info/4 still differs. One hundred images have at most five differing functions. The separate atom-selector scan finds 5,285 functions in 751 images with keys present in an OTP jump table and tested individually in C#. The scan compares keys and can include comparisons from unrelated branches.

- [Corpus](otp-corpus.json), [function deltas](normalized-functions.txt), [image ranking](image-function-differences.txt), [nearest function instructions](nearest-functions.txt), [debug_info instructions](erl-abstract-code-debug-info.txt), and [atom jump-table candidates](atom-jump-table-ranking.txt)
