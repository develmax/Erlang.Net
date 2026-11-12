# Part 1444 — Reuse checked record shapes

The general emitter records the tag and arity of an alias after matching a tagged tuple. A record access or update can use the saved operand directly when the alias and its Y slot still match that proof. Case arms inherit only the incoming proofs and restore them at the join. Shadowed variables lose the proof. Unproven sources keep their existing tag test and badrecord path.

The fixture checks both an entry alias and a case alias. The entry emits one tagged test, then reads and updates the record without another check. The fallback and the shadowed list-generator variable still report badrecord for an invalid value. Record updates preserve the replacement argument order and load the source only if it is needed in x0.

The implementation reuses the existing tuple-pattern checks, saved variable bindings and typed BEAM tuple operands. The reference is the pinned Windows OTP 29.1.1 release disassembly.

- [Fixture instructions and execution](probe.txt), [focused .NET tests](focused.json), and [compiler hashes](compiler-hash.txt)

The full Windows OTP 29.1.1 run generated all 1,366 release images. It reports 382 Equal, including ELDAPv3 from the C# ASN.1 generator, and 984 Different. Both runtimes accepted the 984 differing images. The change reaches 327 images and 1,629 functions. The aggregate absolute Code size gap fell by 84,058 bytes. Whole-image equality and normalized function equality did not change.

The record check scan fell from 3,839 functions in 469 images to 3,099 functions in 424 images. In asn1ct:scan_pass/1 the emitter removes both explicit badrecord blocks. The remaining scan includes both mod_head functions, where the first checked field access can supply the proof for later accesses. Three remaining modules also appear in the near-image ranking.

- [Corpus](otp-corpus.json), [delta](summary.txt), [release instructions](release-records.txt), [record check scan](record-checks-after.txt), and [near-image overlap](near-record-overlap.txt)
