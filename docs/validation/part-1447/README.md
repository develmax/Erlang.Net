# Part 1447 — Omit unused case tuple fields

The case emitter substitutes wildcards for fresh bindings that occur once in the pattern and are unused in the guard, branch body or continuation. It retains prebound and repeated variables, captured values, and exported branch bindings. It reuses the closure reference collector and keeps the original clause for the existing flat register-arm fast path.

The fixture verifies that a three-element tuple with unused fields emits only its shape and arity checks. It also checks repeated equality, matching an existing variable, a guard reading a field, a closure capture, and a value used after the case join. OTP execution confirms both acceptance and rejection.

- [Fixture instructions and execution](probe.txt), [focused .NET tests](focused.json), and [compiler hashes](compiler-hash.txt)

The full Windows OTP 29.1.1 run generated all 1,366 release images. It reports 382 Equal, including ELDAPv3 from the C# ASN.1 generator, and 984 Different. Both runtimes accepted all 984 differing images. The change reaches 427 images and 1,405 functions. Fifty-nine frame sizes reach the OTP value. In snmp_shadow_table:delete_table/1 the frame changes from three to two, while OTP has three. The aggregate absolute Code size gap fell by 14,351 bytes. Whole-image and normalized function equality did not change.

The unused projection scan fell from 1,890 functions in 485 images to 1,493 functions in 398 images. In mod_head:do/1 the projection count falls from 17 to nine, equal to OTP. The frame falls from five slots to three, while OTP has one. The direct destination scan finds 3,689 functions in 708 images with a projected field moved to an X register where OTP projects the same field index directly to the destination.

- [Corpus](otp-corpus.json), [delta](summary.txt), [release instructions](release-fields.txt), [unused field scan](unused-tuple-fields-after.txt), and [direct destination scan](direct-field-destinations-after.txt)
