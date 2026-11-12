# Part 1446 — Test plain case tuples in X registers

The register case emitter accepts plain tuple patterns, including nested tuples, and keeps the original selector available for rejection. It groups disjoint arities, and it uses a tag table only if every clause in the arity group has an atom tag. Mixed groups keep their source order. A nested tuple test uses its own scratch register.

The fixture compares a plain nested branch that overlaps a tagged clause of the same arity, a different arity, a nested mismatch, and an unchanged fallback payload. Both compilers emit an arity table for the fixture. The C# emitter still saves fields for generic branch bodies.

The implementation reuses the existing register pattern emitter and arity dispatch. The reference is pinned Windows OTP 29.1.1.

- [Fixture instructions and execution](probe.txt), [focused .NET tests](focused.json), and [compiler hashes](compiler-hash.txt)

The full Windows OTP 29.1.1 run generated all 1,366 images. It reports 382 Equal, including ELDAPv3 from the C# ASN.1 generator, and 984 Different. Both runtimes accepted the 984 differing images. The change reaches 531 images and 2,037 functions. Thirty-four stack frames now match OTP and no measured frame moves further away. The aggregate absolute Code size gap fell by 18,424 bytes. Whole-image and normalized function equality did not change.

The plain tuple spill scan dropped from 2,879 functions in 560 images to 2,680 functions in 545 images. In mod_head:do/1 the first status tuple uses x0 for both shape and arity. The C# emitter still extracts all three unused fields. The separate unused-field scan finds 1,890 functions in 485 images where a field is written to a Y slot with no other use in the function.

- [Corpus](otp-corpus.json), [delta](summary.txt), [release instructions](release-tuples.txt), [remaining plain spills](plain-tuple-spills-after.txt), [selector producers](plain-tuple-producers.txt), and [unused fields](unused-tuple-fields-after.txt)
