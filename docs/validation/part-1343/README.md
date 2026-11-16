# Part 1343 — Call chains inside clause frames

The general compiler reads nested calls once and uses the resulting plan in both unconditional functions and guarded clause bodies. The tuple conversions are ordinary external calls. An unguarded cons branch can save the tail directly during extraction, reducing the argument moves before the first call.

The focused check matches the entire walk/2 instruction sequence after label normalization. It also exercises nested list and tuple values, a list in the second argument, improper input and a failed tuple conversion. The mixed cons, computed head and repeated binding regressions passed.

The Windows OTP 29.1.1 corpus generated all 1,366 images with 364 Equal, 1,002 Different and zero SourceRejected. Eighty-five images changed and narrowed their Code size gaps by 2,504 bytes in total. Twenty functions gained matching decoded instructions and no prior matches were lost. qlc_pt:restore_locations/2 matches after label normalization. The OTP loader accepted all differing images. ELDAPv3 and error_handler remain Equal.

- [Focused check](check-entry-call-chain.ps1)
- [Focused output](probe.txt)
- [Mixed cons regression](mixed-regression.txt)
- [Computed head regression](computed-regression.txt)
- [Repeated binding regression](repeated-regression.txt)
- [Release restore_locations instructions](qlc-restore-locations.txt)
- [Corpus report](otp-corpus.json)
- [Changed images](changed-images.txt)
- [Code size differences](code-delta.json)
- [Instruction matches](normalized-functions.txt)
- [OTP loader](loader.txt)
- [Conversion call ranking](conversion-call-audit.txt)
