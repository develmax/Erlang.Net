# Part 1774

Keep the result of a call or inner arithmetic expression in X0 when the other operand is a constant or a bound variable and the GC BIF can read X0. Preserve the operand order of noncommutative operations and use OTP's variable-before-literal order for commutative operators. Mark X0 live through the GC BIF and annotate a proven integer result.

All six bor instructions and the Type chunk in ex_slider:do_init/1 now match OTP. The C# frame uses 9 slots and the release uses 8. The independent fixture moved from four frame slots to three, matching the reference frame size. The previous compiler, current compiler and OTP passed all eight fixture checks.

All 1,366 Windows OTP 29.1.1 sources were accepted and emitted BEAM images. 399 files matched byte for byte, including ELDAPv3 generated from ASN.1 by C#. OTP accepted both images in all 967 differing pairs.

Compared 394 changed images. 6 normalized instruction matches were gained and 0 were lost. Absolute Code-size differences changed by -10898 bytes in total.

All 11 focused cases passed and the preceding compiler failed the new frame regression. The compiler block passed 3722 of 3830. The same 108 cases failed on part 1773. The original string Jaro case passed in this run.

Schedule the last independent Y-to-X argument load ahead of the result spill when this lets the frame allocator reuse the released slot. In ex_slider:do_init/1 the stored fourth slider result currently overlaps the first slider until the first call to wxSizer:add/3.

[Corpus](otp-corpus.json) · [Status changes](status-changes.txt) · [Loader and Code sizes](loader-and-code.txt) · [Normalized instructions](normalized-functions.txt) · [First instruction differences](first.txt) · [Fixture](beam_straight_constants.erl) · [Fixture execution](fixture-behavior.txt) · [Release function instructions](ex-slider-instructions.txt) · [Focused tests](focused.txt) · [Previous compiler regression](negative.txt) · [Compiler block](compiler-block-summary.txt)
