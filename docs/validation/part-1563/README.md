# Part 1563 — Order argument reloads around a reused stack slot

The saved-call emitter reads the overwritten slot and the operand for the X-register swap before saving X0. Other stack loads follow the save. The existing move scheduler orders each group. OTP 29.1.1 beam_block.erl sorts contiguous moves by Y register. No dependency was added.

The instructions in observer_html_lib:expandable_term/4 match after label normalization. Four slot-reuse fixture outcomes and three focused compiler tests passed. [Instructions](expandable_term.txt) · [Fixture](../part-1562/beam_reuse_dying_call_slot.erl) · [Outcomes](fixture.txt) · [Tests](focused.json).

All 1,366 sources were accepted and generated. The byte comparison found 390 Equal and 976 Different. ELDAPv3 matched from C# ASN.1 generation. Only observer_html_lib changed. One function gained an instruction match and none lost one. OTP accepted all 976 differing images for loading. The full runtime suite and canonical readiness were not rerun. [Corpus](otp-corpus.json) · [Function comparison](normalized-functions.txt) · [Loader](loader-and-code.txt).
