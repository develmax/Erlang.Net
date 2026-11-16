# Part 1440 — Keep tagged case tests in X registers

The register case emitter accepts supported nested tagged patterns and fresh selectors from expression emission. A nested tuple uses a temporary X register separate from its parent. The pattern still saves variables that can outlive the check and reads the unmodified selector on rejection. Saved-selector case arms can also use temporary X registers for nested tag tests when branch slots are reusable. The fast path for flat tuple arms remains limited to flat patterns because nested patterns can bind names used by later fields.

The OTP fixture checks three nested tagged tests on X registers, a match, rejection of a mismatched bound field, and rejection of a repeated field across nesting levels. Both rejections return the original selector. The rest of the fixture covers the previous guard and tuple tests.

The implementation reuses the general register case emitter and BEAM tuple checks. The OTP reference uses X-register tests for the same tags and arities. The C# result still uses a stack frame and makes additional moves for live bindings.

- [Fixture instructions and execution](probe.txt), [focused .NET tests](focused.json), and [compiler hashes](compiler-hash.txt)

The Windows OTP 29.1.1 run generated all 1,366 release images. It reports 382 Equal, including ELDAPv3 from the C# ASN.1 generator, and 984 Different. Both runtimes accepted all 984 differing images. The change reaches 402 images and 1,251 functions. Frame size reaches the OTP value in 67 functions and moves further from it in two, megaco_messenger_misc:encode_transaction/2 and edlin_context:read_operator1/1. The aggregate absolute Code size gap fell by 2,263 bytes. Whole-image and normalized function equality did not change.

The immediate tuple spill scan dropped from 3,946 functions in 686 images to 3,527 functions in 649 images. Selector moves remain in 540 images and nested-field spills in 412. wxDisplay:new/1 still spills the results of both environment lookups before matching.

- [Corpus](otp-corpus.json), [delta](summary.txt), [register scan](tagged-registers-after.txt), and [release instructions](release-patterns.txt)
