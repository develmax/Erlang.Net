# Part 1439 — Fuse general tagged tuple pattern tests

The general pattern emitter emits the existing is_tagged_tuple instruction when the tuple's shape has not yet been tested and the first member is a literal atom. It skips reloading the proven tag. Previously checked shapes and literals retain their existing path, and the emitter keeps a required receive result extraction. Remaining fields use the same pattern checks and failure label.

The fixture compares nested tagged tuples with OTP. It checks two fused instructions, successful projection, both tag failures, wrong arity, a non-tuple, and a bound-field mismatch. The fallback returns the original selector. The C# fixture still uses a stack frame, and OTP tests the selector and nested field in X registers.

The implementation reuses EmitRecordTest for the BEAM encoding. The pinned OTP beam_ssa_opt:is_tagged_tuple pass fuses the same shape, size and tag checks when they share a failure edge.

- [Fixture instructions and execution](probe.txt), [focused .NET tests](focused.json), and [compiler hashes](compiler-hash.txt)

The Windows OTP 29.1.1 run generated all 1,366 release images. It reports 382 Equal, including ELDAPv3 generated from ASN.1 in C#, and 984 Different. Both runtimes accepted all 984 differing images. The change reaches 904 images and 11,859 functions. Three additional functions match after label normalization. The aggregate absolute Code size gap fell by 556,549 bytes. Frame size reaches the OTP value in 38 functions and moves closer in 9,280. Two functions move further from OTP when the generic emitter drops a slot. These are beam_ssa_opt:ssa_opt_sink/1 and megaco_config:handle_stop_user/1.

The split-pattern scan dropped from 7,856 functions across 848 images to ten functions across nine images. In wxDisplay:new/1 all three tagged tests now use the fused instruction. In mod_head:do/1 the C# emitter now has ten tagged tests and OTP has one. The split tests previously hidden behind shape and arity instructions account for two of the new fused tests.

The follow-up scan finds 3,946 functions in 686 images where C# moves or projects a tuple to a Y register immediately before testing it and OTP has a matching test on an X register. The scan counts 576 images for selector moves and 469 for nested fields, with overlap.

- [Corpus](otp-corpus.json), [delta](summary.txt), [normalized functions](normalized-functions.txt), and [release instructions](release-patterns.txt)
- [Remaining split tests](tagged-tuple-tests-after.txt), [register scan](tagged-registers-after.txt), and [near-image ranking](near-images-after.txt)
