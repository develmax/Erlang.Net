# Part 1341 — Computed cons head and tail calls

The C# planner evaluates a call in a list head before the tail call. It saves the original values needed by the tail or result tuple, reuses dead argument slots and trims the stack before the second call. The simple cons entry extracts the head to its call register and the tail directly to its saved slot.

The focused test matches three decoded instruction sequences after label normalization. It verifies call order through process state, head exception short-circuiting, retained tuple operands, improper input and a 5,000-element list. Three prior cons regression fixtures passed.

The Windows OTP 29.1.1 corpus generated all 1,366 files with 364 Equal, 1,002 Different and zero SourceRejected. Thirty-two images changed. Twenty functions gained matching decoded instructions after label normalization and no prior matches were lost. Thirty-one images narrowed their Code size gaps. ct_logs widened by two bytes because its new smaller branch frames repeat the cons tests. The aggregate absolute Code gap fell by 994 bytes. OTP accepted all differing images. ELDAPv3 and error_handler remain Equal.

- [Focused check](check-computed-cons-head.ps1)
- [Focused output](probe.txt)
- [Repeated binding regression](repeated-regression.txt)
- [Nested cons regression](nested-regression.txt)
- [Branch regression](branch-regression.txt)
- [ASN.1 call schedule comparison](asn1-enc-cse.txt)
- [ct_logs comparison](ct-logs-changes.txt)
- [Corpus report](otp-corpus.json)
- [Changed images](changed-images.txt)
- [Code size differences](code-delta.json)
- [Instruction matches](normalized-functions.txt)
- [OTP loader](loader.txt)
