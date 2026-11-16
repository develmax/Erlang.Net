# Part 1174 — measure entry frame initialization

The previous corpus ranks first opcode differences across all differing images. In 773 images at least one function has a reference X-to-Y move after an allocation, while the C# output allocates a larger frame and starts with init_yregs. The cohort counts overlap.

The candidate moved argument copies ahead of initialization in the general emitter. The candidate still produced 356 Equal, 1,007 Different and 3 SourceRejected out of 1,366. All previously exact modules remained exact. OTP prepared all 1,007 differing images for loading and three focused tests passed.

The candidate changed 997 differing images without making an additional image equal. The Code length gap widened in 554 images and narrowed in zero. For example, in asn1ct:print_structured_errors/1 the candidate inserted a reload of the argument from Y0 into X0 before copying it to Y1. The change was reverted. The current compiler checkpoint is Part 1173.

Next, isolate which values must survive the first safepoint and which slot writes can remain in X registers. Frame size and register placement are part of the same comparison.

- [Baseline](../part-1173/otp-corpus.json)
- [Candidate corpus](candidate-corpus.json)
- [Candidate delta](delta.json)
- [Candidate focused tests](focused.json)
- [Candidate loader](loader.txt)
- [Candidate ranking](ranking.txt)
- [Baseline move cohort](move-cohort.txt)
- [Candidate leading opcode comparison](prefix.txt)
