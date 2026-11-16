# Part 1560 — Keep tail argument BIF results in registers after calls

The compiler reuses its non-allocating BIF operand planner after a linear prefix of calls when each call needs only the preceding result or constants. The prefix reserves a frame with zero Y slots. The suffix places BIF results directly into the tail arguments' registers where available. The planner preserves values still needed by later operands. No dependency was added.

In test_server_ctrl:html_convert_modules/3, the two get/1 results now go directly to X0 and X1. The function matches the OTP instructions after label normalization. Five fixture outcomes matched on the OTP VM. Three selected compiler tests passed. [Instructions](html_convert_modules.txt) · [Fixture](beam_bif_tail_sequence.erl) · [Outcomes](fixture.txt) · [Tests](focused.json).

All 1,366 sources were accepted and generated. The byte comparison found 390 Equal and 976 Different. ELDAPv3 matched from the C# ASN.1 generator. Only test_server_ctrl changed. One function gained a complete instruction match after label normalization, and none lost one. The OTP loader accepted all 976 differing images. The full runtime suite and canonical readiness were not rerun. [Corpus](otp-corpus.json) · [Instruction comparison](normalized-functions.txt) · [Loader](loader-and-code.txt).
