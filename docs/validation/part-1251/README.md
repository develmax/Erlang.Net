# Part 1251 — Restore selector coordinates on atom case failures

Restore the selector location before `case_end` when emitted branches add line instructions. In `asn1ct:check/2`, the failure edge again points to line 1724, matching Windows OTP 29.1.1 after label normalization.

The execution probe confirms the selector line in the exception stack and checks both successful arms. The part 1250 probe without a branch call produces the same image as before.

The full run generated 1,366 images with 359 Equal, 1,007 Different and zero rejected sources. Forty images changed. Of 51 new explicit failure locations, 30 match a missing release location by function, file basename and line. In `test_server_node:start_which_node/1`, the generated branch adds a line at 343, while OTP has no intervening line between the selector and the failure. The 21 unmatched additions require further alignment of branch lowering or line emission. OTP accepted all 1,007 differing image pairs.

In `dialyzer_dep:traverse/4`, the part 1250 selector uses the same 22 atom keys as the release. The function has 951 instructions including labels and line instructions, down from 973, with 799 in OTP. Its encoded length can still differ because instructions encode different operands and dispatch tables.

- [Stack location probe](check-atom-case-line.ps1)
- [Probe result](probe.txt)
- [No-branch-call probe](no-branch-call-probe.txt)
- [Release failure edge](asn1ct-failure.txt)
- [Dialyzer selector comparison](dialyzer-traverse.txt)
- [Full corpus](otp-corpus.json)
- [Corpus delta](delta.json)
- [Failure location comparison](failure-lines.txt)
- [OTP loader](loader.txt)
