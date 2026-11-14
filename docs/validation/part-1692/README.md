# Part 1692

The structural list emitter writes branch blocks for distinct select_val keys in descending term order, with ascending keys in the selection table. Clause planning and fallback order follow the original pattern groups. The release fixture and OTP 29.1.1 beam_core_to_ssa group_value and beam_ssa_codegen linearize were inspected. No dependency was added.

The control fixture now has the same relative selected block order as OTP. The OTP VM verified both selected paths, rejection of a nested shape, the fallback, nil and original improper-tail error arguments. Three focused tests passed. The fixture still differs in the outer dispatch and register layout. [Fixture](beam_shared_cons_payload.erl) · [Disassembly](disassembly.txt) · [Tests](focused.txt) · [VM probe](outcomes.txt).

All 1,366 sources were accepted and compiled. 398 images were equal and 968 differed. ELDAPv3 remained equal. An initial comparison used the new image set as its baseline. The corrected comparison against Part 1691 found three changed images, the same Code sizes and no changes in normalized function matches. The OTP loader accepted all 968 differing images. [Corpus](otp-corpus.json) · [Code comparison](loader-and-code.txt) · [Function comparison](normalized-functions.txt).

Full runtime tests and readiness regeneration were outside this run.
