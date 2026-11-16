# Part 1340 — Repeated bindings across list arguments

The register entry planner compares later occurrences of variables bound in a list head, tail, or tuple field. A tuple alias keeps the whole head available for the result. A successful exact equality test can satisfy a tail call argument already in the destination register.

The focused fixture exercises exact numeric equality, aliased tuple heads, repeated tails, and fallthrough that returns the original list. Three instruction sequences match OTP after label normalization. The nested, branch, and simple cons fixtures also cover prior lowering paths.

The final Windows OTP 29.1.1 corpus generated all 1,366 files with 364 Equal, 1,002 Different and zero SourceRejected. Thirty-three changed files narrowed their Code size gaps by 1,573 bytes in total. Nine functions gained matching instructions after label normalization. An intermediate build displaced the prior orddict:reverse_pairs/2 match. The final build restored it by retaining the existing two-clause tuple alias plan. OTP accepted all differing images. ELDAPv3 and error_handler remain Equal.

The next audit examines list results containing calls in both the head and tail. Abstract forms from all 1,002 differing release files contain this shape in 287 functions across 157 modules. The count is a source inventory.

- [Focused check](check-repeated-cons-arguments.ps1)
- [Focused output](probe.txt)
- [Nested cons regression](nested-regression.txt)
- [Branch regression](branch-regression.txt)
- [Simple cons regression](cons-regression.txt)
- [List result call ranking](list-result-call-ranking.txt)
- [Corpus report](otp-corpus.json)
- [Changed images](changed-images.txt)
- [Code size differences](code-delta.json)
- [Instruction matches](normalized-functions.txt)
- [Preserved tuple alias path](reverse-pairs.txt)
- [OTP loader](loader.txt)
