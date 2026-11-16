# Part 1452 — Lower serial mapper calls in list comprehensions

The compiler extracts a single non-strict variable generator into an OTP-named recursive helper. The helper runs a static mapper or a chain of static calls. Each call consumes the previous result and literal operands. The compiler accepts intermediate block bindings after checking that they do not replace a match against an existing parent variable. It reuses the tail slot for the mapped element and builds the cons cell after the recursive call.

The OTP fixture confirms ordered and repeated argument loads, left-to-right error order, empty input and bad_generator on improper tails. Both nested and block mapper helpers match the pinned OTP instructions after label normalization. A prebound mapper assignment still raises badmatch for a different value.

In asn1ct:compile_set/3, the parent frame drops from twelve slots to seven compared with five in OTP. The new helper keeps the same call and result sequence as OTP. It retains the bad_generator path while proper-list inputs remain unproven.

The first full run found two lost exact matches in Observer. The extractor now consults the existing specialized mapping planner before rewriting, which restores the original table projection schedule.

- [OTP fixture](probe.txt), [ASN.1 instruction comparison](asn1ct-compile-set.txt), [focused .NET tests](focused.json), and [compiler hashes](compiler-hash.txt)


The final Windows OTP 29.1.1 run generated all 1,366 images with zero source rejections. It reports 382 Equal and 984 Different, with no status transitions. ELDAPv3 from the C# ASN.1 generator and both Observer images remain Equal. Both loaders accept all 984 differing images. Fifty-five images changed. Ninety-six functions gain normalized instruction equality, including fifty-nine generated list helpers. The aggregate absolute Code size gap falls by 2,595 bytes. beam_call_types.beam has a newly emitted helper whose name is absent from the OTP image.

The inline-list-test scan now finds 2,811 candidates in 496 images. The largest remaining missing-helper shape has 841 arity-one helpers across 247 images with mapper calls, list construction and no tuple test. The corresponding arity-two group has 738 helpers across 211 images. These are rankings by instruction shape. The previous full .NET suite finished with 9,517 passing and 183 failing tests. The three focused tests passed on the final candidate.

- [Corpus](otp-corpus.json), [preserved release matches](preserved-release-images.txt), [normalized function deltas](normalized-functions.txt), [inline candidates](inline-comprehension-ranking.txt), [helper arity ranking](missing-helper-arities.txt), and [previous full .NET summary](previous-full-test-summary.json)
