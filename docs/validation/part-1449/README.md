# Part 1449 — Project multiple checked record fields into argument registers

The shared call planner accepts two or more field reads from saved variables alongside literal and saved-variable arguments. It checks each required record shape in Erlang evaluation order, keeps earlier X arguments live, and reuses a successful check within the argument list. Simple arguments are loaded after the checks. In mod_head:do_head/1 all three fields for mod_alias:path/3 go directly into x0, x1 and x2. The frame still has eight slots, compared with four in OTP.

The fixture compares direct field destinations and successful execution. When both inputs are invalid, the first record is reported. When only the second input is invalid, that input is reported. Both compiled images check a repeated source record once. The three focused .NET cases pass. The compiler DLLs used by the corpus and focused tests have identical hashes.

- [OTP and C# fixture](probe.txt), [focused tests](focused.json), [compiler hashes](compiler-hash.txt), and [release instructions](release-fields.txt)

The Windows OTP 29.1.1 corpus generated 1,366 images with zero source rejections. It reports 382 Equal, including ELDAPv3 from the C# ASN.1 generator, and 984 Different. Both loaders accept the 984 differing images. The shared planner changes 111 images and 185 functions. Frame size reaches the OTP value in five functions and moves closer in 47 others. The aggregate absolute Code size gap falls by 2,722 bytes.

The frame ranking identifies allocation-operand differences in 1,659 variable-head sequences across 535 modules. OTP tests for a nonempty list before allocation in a group of 2,863 functions across 530 modules where the current schedule differs. These groups can overlap. Every differing image still has an unequal Code chunk and at least three more unequal chunks.

The three sampled ASN.1 decoder functions allocate eleven slots in C# and one in OTP. The OTP parent calls a generated list comprehension helper, while C# emits the loop in the parent. Across the differing corpus, 2,904 functions in 511 images have a larger C# frame and a list test where OTP calls a comprehension helper. This identifies candidates to inspect for shared helper lowering. [Sample instructions](allocation-samples.txt) and [candidate ranking](inline-comprehension-ranking.txt).

- [Corpus](otp-corpus.json), [delta](summary.txt), [frame ranking](frame-shape-ranking.txt), [field scan](direct-field-destinations-after.txt), and [chunk comparison](chunks-after.txt)
