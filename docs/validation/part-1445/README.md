# Part 1445 — Carry successful record checks into later operands

The emitter carries a successful field-access proof into later arguments and sequential expressions, and from a case selector into the case arms. A tagged pattern on a saved variable proves that variable's shape for the matching arm. Proofs refer to the original binding and slot. The operand and case scopes restore the incoming facts. Conditional evaluations do not contribute a fact outside the path where they succeeded.

The fixture compares repeated accesses in a call, a record access used as the case selector, and a saved selector matched by a tagged pattern. It also verifies that an unproven value still raises badrecord after a branch that may skip its first access. The pinned OTP images supply the instruction reference.

- [Fixture instructions and execution](probe.txt), [focused .NET tests](focused.json), and [compiler hashes](compiler-hash.txt)

The full Windows OTP 29.1.1 run generated all 1,366 images. It reports 382 Equal, including ELDAPv3 from the C# ASN.1 generator, and 984 Different. Both runtimes accepted all 984 differing images. The change reaches 302 images and 1,220 functions. The aggregate absolute Code size gap fell by 72,007 bytes. Whole-image and normalized function equality did not change.

The record scan dropped from 3,099 functions in 424 images to 2,579 functions in 391 images. mod_head:do/1 dropped from eight badrecord instructions to one, matching OTP's count. do_head/1 dropped from eight to two, while OTP has one. Both functions still differ in frame allocation and operand moves.

The plain tuple spill scan finds 2,879 functions in 560 images, including mod_head:do/1. Those functions move a tuple to Y immediately before shape and arity tests, with a corresponding X-register test in OTP.

- [Corpus](otp-corpus.json), [delta](summary.txt), [release instructions](release-records.txt), [record scan](record-checks-after.txt), and [plain tuple scan](plain-tuple-spills-after.txt)
