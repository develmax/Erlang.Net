# Part 1333

Construct a result cons after its tail call returns. Keep a variable head in one Y slot and use a zero-slot frame for a constant head. After an empty-list test, return the existing nil in X0. The proper-list proof also recognizes a recursive call used as the tail of the returned cons.

The focused test checks frame sizes, nil tests and execution for private and exported functions. The full Windows corpus generated 1,366 images with 363 Equal, 1,003 Different and zero SourceRejected. The Code size gap narrowed in all 190 changed images, by 1,608 bytes in aggregate. OTP accepted all differing images and ELDAPv3 remained Equal.

The return for an empty list in beam_ssa_codegen:bs_seq_match_fixup/1 now reuses X0. The guarded clauses with two list elements still need branch-specific frame planning. The previous corpus has 1,528 functions in 407 modules with a cons test at entry, a call, a put_list using X0 as the tail, and a smaller OTP frame.

- [Focused check](check-cons-call-result.ps1)
- [Focused output](probe.txt)
- [Release comparison](ssa-list.txt)
- [Full corpus](otp-corpus.json)
- [Changed images](delta.json)
- [OTP loader](loader.txt)
- [Frame ranking](cons-call-frame-ranking.txt)
