# Part 1431 — Reuse a dead argument slot for construction before case

The general emitter can consume the first call result directly from x0 when the next binding builds a tuple or record from prepared operands. Liveness of the remaining expressions identifies an existing Y slot that can hold the constructed value. The first result is saved afterward when the continuation also reads it.

The OTP oracle checks that the constructed tuple uses the dead slot as both input and destination and reads the call result from x0. Both binaries pass the positive and negative case branches and retain the original call result when needed. The C# probe frames shrink from five slots to three and four. OTP uses two slots in both cases. The .NET execution checks use the same compiler assembly as the corpus.

In asn1ct:compile_proc/3, the C# frame shrinks from ten to eight slots, against three in OTP. The constructor now writes directly to the old Options slot. The selector and tuple fields still spill to the stack. outfile/3 instead needs to keep the result of its first case in x0 while testing Ext.

- [Fixture oracle](check-case-tuple-slot.ps1), [before](previous-probe.txt), and [after](probe.txt)
- [compile_proc/3](compile-proc.txt), [outfile/3](outfile-entry.txt), and [focused tests](focused.json)

The final Windows OTP 29.1.1 run generates all 1,366 images with zero source rejections and compiler errors. There are 382 Equal and 984 Different. ELDAPv3 is Equal from the C# ASN.1 generator. OTP prepares all 984 differing images for loading.

Two release images change. asn1ct:compile_proc/3 reaches eight slots and httpd_request_handler:continue_init/7 retains nineteen slots with fewer instructions. The Code size gap drops by fourteen bytes. The whole-image and normalized-function equality counts remain stable. Three targeted .NET tests pass.

- [Corpus](otp-corpus.json), [frame delta](frame-delta.txt), [instruction comparison](normalized-functions.txt), [loader and Code sizes](loader-and-code.txt), and [compiler identity](compiler-hash.txt)
