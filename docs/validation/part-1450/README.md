# Part 1450 — Extract captured list comprehension helpers

The compiler extracts a single non-strict list generator after a fresh binding to a local call. The helper receives the generator result and one captured parameter. The existing comprehension machinery supplies the recursive list construction and the invalid-generator path. A parent whose producer already receives the correct argument registers saves only the captured parameter before the call and tail-calls the helper.

The OTP fixture checks list order, the empty list, bad generators, mapper errors and an element-level try/catch. Both parent instruction sequences match after removing their label declarations. In the three sampled ASN.1 decoder parents, the C# frame falls from eleven slots to one, matching OTP. The generated helpers for complex expressions still differ in frame and exception handling.

- [Fixture and instruction comparison](probe.txt), [focused tests](focused.json), [compiler hashes](compiler-hash.txt), and [release parent samples](allocation-samples.txt)


The final Windows OTP 29.1.1 run generated all 1,366 release images with zero source rejections. It reports 382 Equal, including ELDAPv3 from the C# ASN.1 generator, and 984 Different. Both loaders accept all 984 differing images. Eight images change. Fifteen parent functions reach normalized instruction equality and fifteen helpers are newly emitted. The new ASN.1 helpers use eleven slots where OTP uses four. The aggregate absolute Code size gap falls by 82 bytes.

The candidate scan now finds 2,889 functions in 505 images with larger frames and inline list tests where OTP invokes a list comprehension helper. The next source shapes include tuple-pattern generators in asn1ct:parse_and_save/2 and asn1ct:print_structured_errors/1. The previous full .NET test run reports failures. The three focused tests on the final candidate passed.

- [Corpus](otp-corpus.json), [normalized comparisons](normalized-functions.txt), [frame deltas](frame-delta.txt), [remaining candidate scan](inline-comprehension-ranking.txt), and [observed failures from the previous full suite](previous-full-test-failures.txt)
