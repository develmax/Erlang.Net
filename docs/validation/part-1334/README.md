# Part 1334

Allocate the cons-result frame before extraction. When the call needs only the tail and the return uses the head, extract the head directly into Y0. When the call also uses the head, keep an X-register copy for argument setup.

The focused rebuild function matches the complete OTP instruction sequence. The test also executes a call that uses the saved head as an argument, an improper-list failure and a literal-head case. In the release, ex1:end_of_table/1 gains a complete instruction match after label normalization. edoc_lib:unique/1 now allocates the matching frame before extraction and still differs in how the head reaches the call.

The full Windows corpus generated 1,366 images with 363 Equal, 1,003 Different and zero SourceRejected. Two generated images changed by hash while their reported chunk lengths stayed the same. OTP accepted all differing images and ELDAPv3 remained Equal.

- [Focused check](check-cons-frame-extraction.ps1)
- [Focused output](probe.txt)
- [SNMP function comparison](ex1-end-of-table.txt)
- [Edoc function comparison](edoc-unique.txt)
- [Full corpus](otp-corpus.json)
- [Changed images](changed-images.txt)
- [Instruction matches](normalized-functions.txt)
- [OTP loader](loader.txt)
