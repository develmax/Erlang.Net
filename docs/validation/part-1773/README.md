# Part 1773

Fold fresh constant bindings in straight-line sequences. Calls and record construction retain their evaluation order. The compiler keeps ordinary matching for names that are already bound.

The installed ex_slider:do_init/1 moved from 12 frame slots to 9. OTP uses 8. The independent fixture moved from 7 slots to 4, and OTP uses 3. The previous compiler, current compiler and OTP passed all eight fixture checks, including effects and a repeated match.

All 1,366 Windows OTP 29.1.1 sources were accepted and emitted BEAM images. 399 files matched byte for byte, including ELDAPv3 generated from ASN.1 by C#. OTP accepted both images in all 967 differing pairs.

Compared 40 changed images. 5 normalized instruction matches were gained and 0 were lost. The reduction in absolute Code-size difference was 575 bytes.

All 11 focused cases passed and the preceding compiler failed the new frame regression. The compiler block passed 3721 of 3830 with the same 109 failed cases as part 1772, including the original string Jaro timeout.

Keep an arithmetic operand in X0 when its producer is followed by a GC BIF and no other operand evaluation overwrites it. The part 1771 report identified excess call-result spills before GC BIFs in 1,023 functions across 297 modules.

[Corpus](otp-corpus.json) · [Status changes](status-changes.txt) · [Loader and Code sizes](loader-and-code.txt) · [Normalized instructions](normalized-functions.txt) · [First instruction differences](first.txt) · [Fixture](beam_straight_constants.erl) · [Fixture execution](fixture-behavior.txt) · [Release function instructions](ex-slider-instructions.txt) · [Focused tests](focused.txt) · [Previous compiler regression](negative.txt) · [Compiler block](compiler-block-summary.txt)
