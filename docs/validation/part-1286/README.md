# Part 1286 — Fall through from the final case arm to the tail call

When the boolean failure block is deferred, the last emitted case arm falls through into the tail call. The other arm still jumps to the shared continuation.

The probe checks that no jump targets the immediately following label. It also verifies both successful results and the failure location. The gl:init_nif/0 frame still uses three slots, compared with two in OTP.

The full corpus generated all 1,366 images with 359 Equal, 1,007 Different and zero source rejections. ELDAPv3 remains Equal. OTP accepted all differing pairs. The change affected gl and typer_core. The Code size gap decreased by three bytes for typer_core and increased by two for gl, although the removed jump matches the OTP instruction sequence.

- [Full corpus](otp-corpus.json)
- [Corpus delta](delta.json)
- [OTP loader](loader.txt)
- [Focused execution](probe.txt)
- [Focused command](check-case-fallthrough.ps1)
- [gl instruction comparison](gl-init.txt)
