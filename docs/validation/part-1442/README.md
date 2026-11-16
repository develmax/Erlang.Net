# Part 1442 — Keep guarded case selectors in X registers

The case register path now accepts guards made of supported type tests, direct comparisons and short-circuit branches that preserve the selector. The guard operands are already available as literals or stack bindings, except that a fresh variable matching the whole selector can be read directly from x0. That variable is saved after the guard accepts. The flat unguarded-arm fast path remains limited to clauses without a guard. Deferred operands remain in the ordinary case path because evaluating them may overwrite x0.

The fixture checks a repeated tag with different guards, strict comparison, disjunction, a list selector, an unchanged fallback payload and the original tuple in a case_clause exception. Both OTP and C# emit the list guard on an X register and read x0 for case_end. The C# tuple field is still saved in a Y register before its guard, and the fixture still has a larger frame.

The implementation reuses Guard, GuardComparisonOpcode, the existing operand writers and the tagged case emitter. Pinned OTP disassembly supplies the register and failure reference.

- [Fixture instructions and execution](probe.txt), [focused .NET tests](focused.json), and [compiler hashes](compiler-hash.txt)

The Windows OTP 29.1.1 run generated all 1,366 release images. It reports 382 Equal, including ELDAPv3 from the C# ASN.1 generator, and 984 Different. Both runtimes accepted the 984 differing images. The change reaches 448 images and 1,188 functions. Twenty frame sizes reach OTP and no measured frame moves further away. The aggregate absolute Code size gap fell by 1,933 bytes. Whole-image and normalized function equality did not change.

The immediate tuple spill scan fell from 619 to 605 images. The remote-call producer group fell from 332 to 321. In asn1ct:scan_pass/1 the tagged error test and list guard now read x0 and the frame falls from four slots to three.

The record update scan finds 2,175 functions in 389 images that emit copy and lack a matching OTP reuse form with the same arity and field indices. The pinned beam_ssa_type:simplify_update_record rules distinguish single-field updates, disjoint field types and multi-field updates.

- [Corpus](otp-corpus.json), [delta](summary.txt), [release guards](release-guards.txt), [register scan](tagged-registers-after.txt), [selector producers](selector-sources-after.txt), and [record update scan](record-updates-after.txt)
