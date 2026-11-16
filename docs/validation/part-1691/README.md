# Part 1691

The structural list emitter reads a common tuple payload once before a select_val table. It retains the original head if other fields or later clauses need it. This follows the field projection order in OTP 29.1.1 release disassembly and uses the existing tuple projection emitter. The initial arity-three implementation changed only the fixture. Extending it to wider tuples changed a release image.

The OTP VM verified both literal matches, the field following the shared payload, selection failure, nested mismatch, nil and improper tail errors. Three focused compiler tests passed. The fixture still differs from OTP in dispatch layout and register allocation. [Fixture](beam_shared_cons_payload.erl) · [Disassembly](disassembly.txt) · [Tests](focused.txt) · [VM probe](outcomes.txt).

All 1,366 sources were accepted and compiled. 398 images were equal and 968 differed. ELDAPv3 remained equal. The only changed release image, xmerl_xpath_pred, moved 12 bytes closer to OTP in Code size. The OTP loader accepted all 968 differing images. [Corpus](otp-corpus.json) · [Loader and sizes](loader-and-code.txt) · [Function comparison](normalized-functions.txt).

Full runtime tests and readiness regeneration were outside this run.
