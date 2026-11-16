# Part 1332

Use the private proper-list proof for empty-list returns and tail calls. The cons branch can forward ordinary tail-call arguments in registers. A proven proper list lets the noncons branch go directly to the empty-list body. Exported and indirectly callable functions retain the check.

The focused test compares the presence of nil tests against OTP and exercises both fallback forms and improper-list rejection. In the release diameter_make:is_path/1 the empty-list branch now matches. The earlier binary and nested-list branches still differ.

The full Windows corpus generated all 1,366 images with 363 Equal, 1,003 Different and zero SourceRejected. Six images changed and each Code size gap narrowed, by 89 bytes in aggregate. OTP accepted all differing images and ELDAPv3 remained Equal.

- [Focused check](check-proper-list-return.ps1)
- [Focused output](probe.txt)
- [Release function comparison](diameter-is-path.txt)
- [Full corpus](otp-corpus.json)
- [Changed images](delta.json)
- [OTP loader](loader.txt)
