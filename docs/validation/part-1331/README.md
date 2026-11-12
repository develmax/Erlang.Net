# Part 1331

Infer proper-list results for successful lists:reverse/1 calls and propagate the list fact through lists:dropwhile/2. Remove the nil test in an empty-list call chain when every external entry into the private function proves a proper list and each recursive call preserves the input or a proper suffix. Exported functions and function references retain their checks.

The focused test checks successful calls and improper-list behavior for exported and indirect paths. Its private recursive function has no nil test in either compiler. The release collapse_ws/2 now matches the OTP instruction sequence after label normalization apart from the module-derived fun unique value.

The full Windows corpus generated all 1,366 images with 363 Equal, 1,003 Different and zero SourceRejected. Only xmerl_xsd_type changed and its Code size gap narrowed by four bytes. OTP accepted all differing images and ELDAPv3 remained Equal.

The prior corpus contains 1,125 candidate functions in 363 modules with a cons test at OTP entry and at least one generated nil test absent in OTP. The ranking is a discovery aid. Each removal still requires an input or control-flow proof.

- [Focused check](check-proper-list-entry.ps1)
- [Focused output](probe.txt)
- [Release comparison](xmerl-collapse-ws.txt)
- [Full corpus](otp-corpus.json)
- [Changed image](delta.json)
- [OTP loader](loader.txt)
- [Nil test ranking](nil-test-ranking.txt)
