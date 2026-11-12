# Part 1441 — Keep call-argument case selectors in X registers

The general emitter now uses the register case path for fresh selectors even when its caller does not reuse branch slots. The path still requires guard-free supported patterns, no exported branch bindings or tuple destinations, and no receive scratch registers. Arguments evaluated before the case remain protected by the existing call argument plan.

The fixture puts a case in the middle argument of a call. The preceding argument, selector and following argument each write a marker. OTP and C# produce the same marker sequence and values for both match and fallback. The tag test reads an X register. The C# frame and final call sequence still differ from OTP.

The implementation reuses the existing C# register pattern emitter and call operand scheduling. The pinned Windows OTP image supplies the instruction reference.

- [Fixture instructions and execution](probe.txt), [focused .NET tests](focused.json), and [compiler hashes](compiler-hash.txt)

The first full run found an IndexOutOfRangeException in nteventlog. Its register-list entry uses actual Y slots above the next temporary slot counter. The case arm planner now sizes its saved-argument array from the highest bound physical slot. Direct recompilation then produced a BEAM image, and the corrected compiler was used for the second full run. [First run](first-pass.json) and [direct recheck](nteventlog-recheck.txt).

The corrected Windows OTP 29.1.1 run generated all 1,366 release images. It reports 382 Equal, including ELDAPv3 from the C# ASN.1 generator, and 984 Different. Both runtimes accepted the 984 differing images. The change reaches 457 images and 1,483 functions. Eleven frame sizes reach the OTP value, and no measured frame moves further away. The aggregate absolute Code size gap fell by 9,982 bytes. Whole-image and normalized function equality did not change.

The immediate selector or nested-field spill scan fell from 649 to 619 images. In wxDisplay:new/1 both wx_env tag tests now run immediately after get and the frame fell from four to two slots. OTP allocates per branch and still uses a different argument register for the first lookup. The largest remaining selector-producer group is remote calls, in 332 images and 743 functions.

- [Final corpus](otp-corpus.json), [delta](summary.txt), [release instructions](release-selectors.txt), and [remaining selector sources](selector-sources-after.txt)
