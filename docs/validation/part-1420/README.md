# Part 1420 — Share list extraction across record and cons tail branches

The compiler lowers a literal-head branch with an in-register record update and a variable-head branch with a cons argument through the same list extraction. The empty branch can return a constructed tuple without allocating a frame. The record check is eliminated only when the input shape is proven from local call sites.

The proof follows forwarded parameters, record values, and the fields of tuples returned by local calls. It respects call visibility and native functions. The record update uses reuse, preserving the original record when an alias remains live. In the OTP fixture, collect/3 matches the reference instructions and both images pass the execution checks. The surrounding wrapper still differs at whole-image level. The test also exercises badrecord when an exported entry supplies an unknown value.

All 1,366 Windows OTP 29.1.1 release images were generated with zero source rejections. There are 382 Equal and 984 Different. The only changed image is asn1ct_check. Its check_object_set_objset_list_1/3 goes from a five-slot frame to a frame-free schedule and matches OTP after label normalization. The Code size gap shrinks by 101 bytes. ELDAPv3 remains Equal from C#-generated Erlang. OTP prepares all 984 differing images for loading.

The next candidate in the same high-coverage group is check_unique_sequence_tags1/3. OTP tests and extracts the list before choosing a guarded record branch or a frame-free cons tail. The current compiler allocates five slots before testing the list and extracts it again at the sibling branch.

- [Corpus](otp-corpus.json), [frame delta](frame-delta.txt), [normalized instructions](normalized-functions.txt) and [loader and Code size](loader-and-code.txt)
- [ASN.1 release instructions](asn1-list-record-tail.txt) and [next guarded continuation](list-guard-continuation.txt)
- [Oracle](check-list-record-tail.ps1), [probe](probe.txt), [previous compiler](previous-compiler.txt) and [focused tests](focused.json)
