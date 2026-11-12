# Part 1338 — Branch-local frames for cons results

The register entry planner now handles branches returning a cons over a call. It preserves the head operands across the call, then reserves heap space for the result. When the head needs one tuple field that the call does not use, the planner preserves the tuple and reads the field after the call. The existing two-clause cons and empty-list path retains its instruction order.

The focused fixture compares normalized instructions against OTP and executes tagged, guarded and fallback branches, duplicate tuple fields and improper inputs. The prior cons regression fixture also passed. An intermediate build displaced existing instruction matches in edoc_lib:unique/1 and ex1:end_of_table/1. The final build restored both module hashes.

The Windows OTP 29.1.1 corpus generated all 1,366 images with 364 Equal, 1,002 Different and zero SourceRejected. Twenty-five images changed. Their aggregate absolute Code size gap fell by 1,025 bytes. Seven release functions gained matching decoded instructions after label normalization. The OTP loader accepted all differing images. Both ELDAPv3 and error_handler remain Equal.

- [Focused check](check-branch-cons-call.ps1)
- [Focused output](probe.txt)
- [Existing cons regression check](cons-regression.txt)
- [Corpus report](otp-corpus.json)
- [Changed images](changed-images.txt)
- [Code size differences](code-delta.json)
- [Instruction matches](normalized-functions.txt)
- [OTP loader](loader.txt)
