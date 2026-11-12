# Part 1344 — External conversion calls

The ordinary call planner accepts ten more conversion BIF signatures, including radix conversions and a binary slice conversion. The existing resolver still determines whether a call targets a local function or the Erlang BIF. The focused check exercises both targets, return values and an invalid argument. The radix conversion chain matches decoded OTP instructions after label normalization. The earlier nested chain check also passed.

The full Windows OTP 29.1.1 corpus generated all 1,366 images with 364 Equal, 1,002 Different and zero SourceRejected. Forty-three functions gained matching decoded instructions. The Code size gap narrowed in 73 modules. It widened by one byte in asn1ct_parser2 and by ten bytes in erl_lint. The net reduction was 957 bytes. OTP accepted all differing images. ELDAPv3 and error_handler remain Equal.

The function gap audit uses the part 1343 corpus. It found 14 modules with one differing decoded function after label and closure ID normalization. Nine belong to wx.

- [Focused check](check-conversion-call.ps1)
- [Focused output](probe.txt)
- [Nested chain regression](chain-regression.txt)
- [Corpus report](otp-corpus.json)
- [Changed images](changed-images.txt)
- [Code size differences](code-delta.json)
- [Instruction matches](normalized-functions.txt)
- [OTP loader](loader.txt)
- [Baseline function gap audit](function-gap-audit.txt)
