# Part 1451 — Extract list comprehensions that project tuple fields

The compiler lowers a single non-strict list generator over an existing variable to an OTP-named recursive helper when the body projects a field from a flat tuple pattern. It accepts plain tuples and tuples with a literal atom tag. The helper checks the head, skips a nonmatching head, recurses on a matching tail, and projects the saved field before constructing the result. An improper tail raises bad_generator with the offending value.

The OTP fixture covers tagged and untagged tuples, wrong arities, nonmatching heads, empty lists and improper tails. Both helper bodies match after label normalization. All three focused .NET cases passed. Formatting the new file was followed by a rebuild and a byte-identical fixture check.

- [Fixture instructions and results](probe.txt), [focused tests](focused.json), [compiler hashes](compiler-hash.txt), and [formatting check](probe-image-hashes.txt)

The full Windows OTP 29.1.1 run generated all 1,366 release images with zero source rejections. It reports 382 Equal, including ELDAPv3 from the C# ASN.1 generator, and 984 Different. Both loaders accept all 984 differing images. Thirteen images changed. Eighteen functions gained normalized instruction equality, including thirteen list helpers. The aggregate absolute Code size gap fell by 534 bytes. The current compiler emits two additional helpers in compile.beam whose corresponding names are absent from the release.

The inline list-test scan finds 2,875 remaining candidates in 505 images. A separate instruction scan counts 1,856 OTP helpers absent by name and arity in 367 generated images with a mapper call, list construction and no tuple test. That is the largest measured group of missing helpers. The OTP-only discarded-mapping fixture confirms that calls retain their effects, improper tails still raise bad_generator, and a parent guard rejects a non-list before reaching the helper.

- [Corpus](otp-corpus.json), [instruction deltas](normalized-functions.txt), [frame deltas](frame-delta.txt), [inline candidates](inline-comprehension-ranking.txt), [missing helper shapes](missing-helper-shapes.txt), and [discarded-mapping oracle](discarded-oracle.txt)
