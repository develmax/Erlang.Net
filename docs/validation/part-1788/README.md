# Part 1788

Preserve a zero-slot frame for a trapping tail BIF. Add a frame when an earlier emitter produced a frameless call to one of those BIFs.

Use the six MFA entries in pinned OTP 29.1.1 beam_ssa_pre_codegen is_trap_bif. Reuse the existing import table and tail-call encoding. No dependency was added.

All 1,366 Windows OTP 29.1.1 modules were admitted and generated. 400 matched byte for byte, including ELDAPv3 generated from ASN.1 by C#. OTP accepted both images in all 966 differing pairs.

Compared 17 changed images. Gained 12 and lost 0 normalized instruction matches. The sum of absolute Code size differences changed by 76 bytes.

All 11 focused tests passed. The two fixture functions match the OTP instructions after label normalization. Each compiler passed seven runtime assertions. All nine normalized function matches lost in part 1787 were restored. The broader compiler block was not run for this intermediate build.

Eliminate entry argument spills proven unread by the existing register lifetime analysis.

[Corpus](otp-corpus.json) · [Status changes](status-changes.txt) · [Loader and Code sizes](loader-and-code.txt) · [Normalized instructions](normalized-functions.txt) · [First differences](first.txt) · [Focused tests](focused.txt) · [Fixture results](behavior.txt)
