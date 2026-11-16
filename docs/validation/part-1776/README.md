# Part 1776

Trim straight-line stack frames between calls when the last references to older slots have passed. Relocate still-live initialized values into dead slots when the move and initialization costs allow a larger trim. Rewrite later Y operands and the final frame release for the new layout.

The installed ex_slider:do_init/1 emits the same six trim sizes as OTP and ends at two slots. The fixture emits both reference trims. The previous compiler, current compiler and OTP passed all eleven fixture assertions.

All 1,366 Windows OTP 29.1.1 sources were accepted and emitted BEAM images. 399 files matched byte for byte, including ELDAPv3 generated from ASN.1 by C#. OTP accepted both images in all 967 differing pairs.

Compared 233 changed images. 7 normalized instruction matches were gained and 0 were lost. Absolute Code-size differences changed by 2362 bytes in total.

All 11 focused cases passed and the preceding compiler failed the new frame regression. The compiler block passed 3722 of 3830. The same 108 cases failed on part 1775.

Combine heap checks for adjacent construction without crossing a call, a garbage-collecting operation or a branch. Part 1775 contains 853 modules with excess split checks.

[Corpus](otp-corpus.json) · [Status changes](status-changes.txt) · [Loader and Code sizes](loader-and-code.txt) · [Normalized instructions](normalized-functions.txt) · [First instruction differences](first.txt) · [Fixture](beam_trims_between_calls.erl) · [Fixture execution](fixture-behavior.txt) · [Release function instructions](ex-slider-instructions.txt) · [Focused tests](focused.txt) · [Previous compiler regression](negative.txt) · [Compiler block](compiler-block-summary.txt) · [Split heap-check rank](rank-split-construction-heap.txt)
