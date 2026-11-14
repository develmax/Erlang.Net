# Part 1686

Structural tuple projection records the capture order independently of a reused physical register. In init:eval_script/2 the preceding register reuse changed the order of captured tuple fields and introduced four swaps in the lifted functions. Preserving the projection order removes those swaps. The reference is the release disassembly and the X register lifetime analysis in OTP 29.1.1. No dependency was added.

The OTP VM verified the environment order, returned tuple and fallback for a nested-pattern closure compiled in C#. The OTP compiler folds the fixture tuple into one capture. Three focused compiler tests passed. [Fixture](beam_structural_capture_order.erl) · [VM probe](outcomes.txt) · [Disassembly](fixture-disassembly.txt) · [Tests](focused.txt).

All 1,366 sources were accepted and compiled. 398 images were equal and 968 differed. ELDAPv3 remained equal. Four images changed. The init Code section dropped from 24,467 to 24,455 bytes against 18,875 bytes in OTP. The release still has one swap per affected closure, and the normalized function match counts stayed the same. The OTP loader accepted all 968 differing images. [Corpus](otp-corpus.json) · [Code and loader](loader-and-code.txt) · [Init closures](init-closures.txt) · [Function comparison](normalized-functions.txt).

Full runtime tests and readiness regeneration were outside this run.
