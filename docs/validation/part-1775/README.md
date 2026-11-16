# Part 1775

Move an independent stack-to-register argument load ahead of a call result store when linear allocation proves that the moves can share a slot and reduce the frame. The load and store read distinct registers and neither depends on the result of the other.

The installed ex_slider:do_init/1 moved from nine frame slots to eight, matching OTP. The independent fixture moved from four slots to three, matching OTP. All three executions passed the nine fixture checks.

All 1,366 Windows OTP 29.1.1 sources were accepted and emitted BEAM images. 399 files matched byte for byte, including ELDAPv3 generated from ASN.1 by C#. OTP accepted both images in all 967 differing pairs.

Compared 31 changed images. 1 normalized instruction matches were gained and 0 were lost. Absolute Code-size differences changed by -103 bytes in total.

All 11 focused cases passed and the preceding compiler failed the new frame regression. The compiler block passed 3722 of 3830. The same 108 cases failed on part 1774.

Implement repeated trims based on slot lifetime between calls. The part 1774 comparison found 869 modules with fewer trims than OTP, including 302 modules with straight-line functions missing a trim.

[Corpus](otp-corpus.json) · [Status changes](status-changes.txt) · [Loader and Code sizes](loader-and-code.txt) · [Normalized instructions](normalized-functions.txt) · [First instruction differences](first.txt) · [Fixture](beam_call_result_slot_reuse.erl) · [Fixture execution](fixture-behavior.txt) · [Release function instructions](ex-slider-instructions.txt) · [Focused tests](focused.txt) · [Previous compiler regression](negative.txt) · [Compiler block](compiler-block-summary.txt) · [Frame trim rank](rank-frame-trims.txt) · [Straight-line trim rank](rank-straight-frame-trims.txt)
