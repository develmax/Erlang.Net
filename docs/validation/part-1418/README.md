# Part 1418 — Emit dynamic tail calls after tuple dispatch

The grouped tuple planner now emits apply_last for dynamic remote calls in a tagged-result and atom case. It keeps only the needed entry fields in the frame, projects the tagged payload into an X register, and emits case_clause after the outer fallback. Compatible groups returning tuples use register projections and return without a frame. The existing argument constructor planner emits the ordinary tail call for constructed exits.

The C# compiler rebuilds xmerl_xpath_lib.beam from the installed Erlang source byte for byte. primary_expr/2 now matches the release instructions exactly, with a three-slot frame and two apply_last instructions. The deterministic test fixture matches OTP as a complete image. Both images were executed through the two dynamic branches, both tuple returns, the constructed exits and case_clause. The previous compiler differs on the fixture.

The full Windows OTP 29.1.1 run generates all 1,366 images with zero source rejections. There are 381 Equal and 985 Different. xmerl_xpath_lib is the sole transition to Equal and ELDAPv3 remains Equal from C#-generated Erlang. OTP prepares all 985 differing images for loading. The Code chunk of xmerl_xpath_lib drops from 379 to the reference 318 bytes.

The next near-parity module is xmerl_b64Bin_scan. The captured instructions show two differing function bodies, scan/2 and scan_token/1. They provide reference cases for list head dispatch with disjunctive guards and a tail call with a cons cell built from a tuple result. The section audit finds Code differences in all 985 outstanding modules.

- [Corpus](otp-corpus.json), [section audit](section-audit.txt), [frame delta](frame-delta.txt) and [loader and Code size](loader-and-code.txt)
- [Exact release comparison](xmerl-release.txt) and [hashes](xmerl-release-hashes.txt)
- [Oracle](check-dynamic-tuple-case.ps1), [probe](probe.txt), [previous compiler](previous-compiler.txt), [fixture hashes](fixture-hashes.txt) and [focused tests](focused.json)
- [Near-parity functions](near-parity-functions.txt) and [Base64 scanner instructions](xmerl-base64-instructions.txt)
