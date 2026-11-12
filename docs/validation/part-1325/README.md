# Part 1325

Preserve the input tuple in one Y slot across the record-producing call. Both branches of the record selection prove the tag and arity, so the emitter extracts the input field after the call and emits the update without a second record test. The plan supports both an inline update and one assigned to a fresh variable and then passed to the tail call.

The focused comparison matches both instruction streams after label normalization. It checks the default record, preservation of unchanged fields, and the original record retained in the process dictionary. The release asn1ct:save_gen_state/2 now matches all instructions after label normalization.

The full Windows corpus generated 1,366 images with 363 Equal, 1,003 Different and zero SourceRejected. Only asn1ct changed and its Code size gap narrowed by 43 bytes. OTP accepted all differing images. ELDAPv3 remained Equal.

- [Focused check](check-tuple-record-update.ps1)
- [Focused output](probe.txt)
- [Release function comparison](asn1-save-gen-state.txt)
- [Full corpus](otp-corpus.json)
- [Changed image](delta.json)
- [Instruction match](normalized-functions.txt)
- [OTP loader](loader.txt)
