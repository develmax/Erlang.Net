# Part 1329

Share the first cons extraction between a two-literal prefix clause and a generic cons clause. If the prefix clause reconstructs its second cons unchanged, the recursive call can use the existing tail. Test the second head with get_hd. The prepend path constructs the accumulator cons and tail-calls before fallback allocation.

The focused probe matches the complete recursive prefix against OTP and checks function_clause arguments for an improper list. The release collapse_ws/2 also matches that prefix after label normalization. The fallback still differs in proper-list inference, closure allocation and tail calling.

The full Windows corpus generated all 1,366 images with 363 Equal, 1,003 Different and zero SourceRejected. Only xmerl_xsd_type changed. Its Code size gap narrowed by 33 bytes. OTP accepted all differing images. ELDAPv3 remained Equal.

- [Focused check](check-shared-cons-lookahead.ps1)
- [Focused output](probe.txt)
- [Release comparison](xmerl-collapse-ws.txt)
- [Full corpus](otp-corpus.json)
- [Changed image](delta.json)
- [OTP loader](loader.txt)
