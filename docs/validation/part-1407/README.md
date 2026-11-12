# Part 1407 — Compile captured list filters before case

The compiler extracts a non-strict list comprehension used as a terminal case selector when its generator matches a key and a list with a required prefix. The helper tests the tuple and list shape, passes the capture and matched fields to the local call, skips nonmatching elements and raises bad_generator for an improper generator. The case emitter reuses the selector register and emits one list test for the empty and nonempty arms.

The deterministic fixture matches OTP byte for byte and agrees on empty, unique and duplicate inputs. An initial fixture also matched the targeted functions and runtime results. Its separate mapper returned a list containing a constructed tuple and allocated a frame where OTP used registers, so the permanent fixture uses a mapper that returns its list argument.

The full Windows OTP 29.1.1 run generated all 1,366 images. The strict comparison has 380 Equal, 986 Different, zero SourceRejected and zero Unsupported. ELDAPv3 remains equal. Only asn1ct_check changed. Its check_duplicate_defs/2 and generated comprehension helper match after normalizing labels, and its Code gap narrowed by 38 bytes. OTP prepared all 986 differing images for loading. The three focused compiler cases passed using the corpus compiler assembly.

All 986 differing images have a Code mismatch. Other mismatching sections include ExpT in 982 images, LocT in 964, AtU8 in 955 and Line in 952. Section counts overlap, and these counts do not establish independent root causes. For example, changing function layout can change both label operands and table entries.

- [Full corpus](otp-corpus.json)
- [Instructions before](operands-before.txt) and [after](operands-after.txt)
- [Normalized instruction changes](normalized-functions.txt) and [loader and Code sizes](loader-and-code.txt)
- [Oracle](check-captured-list-filter.ps1), [result](probe.txt) and [hashes](fixture-hashes.txt)
- [Compiler tests](focused.json)
- [Frame audit](frame-audit.txt) and [audit script](frame-audit.escript)
