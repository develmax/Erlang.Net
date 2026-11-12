# Part 1314

Atom clauses with distinct replacement values and an identity fallback use select_val directly on the input register. The focused comparison reverses the source order of two keys and verifies an unfamiliar atom and a tuple through the fallback.

For a nested external call that leaves its own source line active, the computed argument path passes that location to the outer call. List subtraction uses the active location when it belongs to the same source file. This removes duplicate line instructions on the straight-line call sequence in crypto_ec_curves:curves/0.

The generated crypto_ec_curves.beam matches the Windows OTP 29.1.1 release byte for byte. Both files have SHA-256 48D80123640F2E2EED5EFA9A63D95BFCBC8E82056DCCF4E85DD371AC891F9D15.

- [Focused command](check-atom-replacement.ps1)
- [Atom replacement instructions](probe.txt)
- [Release function and byte comparison](crypto-ec-curves.txt)
- [Atom replacement forms in the previous corpus](atom-replacement-forms.txt)

The final run accepted all 1,366 sources and generated 1,366 files. It reported 362 Equal and 1,004 Different. No previously equal image changed status. Among the 466 changed images that still differ, the Code size gap decreased in 455, increased in ten and was stable in one. OTP accepted every differing image.

- [Full corpus](otp-corpus.json)
- [Changed images](delta.json)
- [Matching atom dispatch functions](atom-functions.txt)
- [OTP loader](loader.txt)
- [Remaining unused result in the SNMP filter](near-equal-snmp.txt)
