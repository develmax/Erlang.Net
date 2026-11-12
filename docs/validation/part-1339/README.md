# Part 1339 — Nested cons tests before allocation

The entry planner now checks nested unary cons patterns and exact equality for literal or repeated tuple fields. It keeps remainder guards in registers and branches to the next clause when a guard operand is invalid. Independent argument patterns keep their binding order.

The focused comparison exercises equal and unequal bindings, guard failure on zero or a noninteger divisor, improper input and nested fallback. OTP and C# allocate one slot per result branch in the nested fixture. The complete instruction sequences still differ. In bs_seq_match_fixup/1, OTP shares the first tuple tag check and dispatches on the second tuple's arity.

The full Windows OTP 29.1.1 corpus generated all 1,366 images. The result remains 364 Equal, 1,002 Different and zero SourceRejected. Twenty-one images changed. The sum of their absolute Code size gaps fell by 1,047 bytes. No decoded function gained or lost an instruction match after label normalization. OTP accepted all differing images. ELDAPv3 and error_handler remain Equal.

The source shape audit read all 1,002 differing release modules. It found 41 functions in 35 modules with repeated pattern bindings in cons result clauses. Four modules have the narrower leading shared-tag shape.

- [Focused check](check-nested-cons-call.ps1)
- [Focused output](probe.txt)
- [Branch regression](branch-regression.txt)
- [Cons regression](cons-regression.txt)
- [Release instruction comparison](beam-ssa-codegen.txt)
- [Corpus report](otp-corpus.json)
- [Changed images](changed-images.txt)
- [Code size differences](code-delta.json)
- [Source shape ranking](nested-cons-ranking.txt)
- [OTP loader](loader.txt)
