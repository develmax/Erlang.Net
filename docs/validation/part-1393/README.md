# Part 1393 — Dispatch list clauses before allocating the frame

Separate entry frames can now handle an empty-list clause returning a tuple or nil after nonempty-list clauses. A constant function-arity guard can run directly on the incoming X register. When a call-prefix case planner replaces the deferred call-sequence planner, the selected plan performs its own extraction or keeps the ordinary entry extraction.

The permanent regression fixture covers list head and tail values across a call prefix, a tuple result from the empty-list clause and rejection of an invalid callback. The OTP oracle also exercises the recursive fallback and valid and invalid callback arities. Both probe functions move the nonempty-list test before allocation. Other instructions still differ from OTP. The selected compiler cases and the whole-module check pass with the same compiler assembly used by the corpus.

The Windows OTP 29.1.1 corpus generated all 1,366 images. There are 372 Equal, 994 Different, zero Unsupported and zero SourceRejected. ELDAPv3 remains Equal using the C# ASN.1 generator. OTP accepts all 994 differing images for loading.

The change affects 178 release images. The list-entry test now matches OTP in 265 functions across 135 modules that previously allocated first, with no lost entry matches. Two functions gain normalized instruction matches and none lose one. Of 330 changed peak frames, 327 are closer to OTP, including 26 that reach the OTP size. Three lose the OTP peak size. The Code size gap narrows in 159 images and widens in 14, with a net reduction of 7,992 bytes.

The largest first-difference group by module count is now tuple tests before allocation, with 2,187 functions in 558 modules. The nonempty-list group drops from 3,204 functions in 561 modules to 2,938 in 537. The two ASN.1 examples now match the OTP first list test and still differ in extraction and frame layout. The full runtime suite, canonical readiness regeneration and publication remain pending.

- [Full corpus](otp-corpus.json)
- [List entry delta](list-entry-delta.txt)
- [Release disassembly](list-entry-instructions.txt)
- [Frame audit](frame-shapes.txt)
- [List source groups](list-case-branches.txt)
- [Normalized instructions](normalized-functions.txt)
- [Peak stack slots](peak-slots.txt)
- [Code size gaps](code-delta.json)
- [OTP loader check](loader.txt)
- [OTP oracle](check-list-entry-fallback.ps1)
- [Oracle result](probe.txt)
- [Compiler tests](focused.json)
- [Whole-module check](whole-module.txt)
