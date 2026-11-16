# Part 1689

The compiler emits a sorted select_val table when consecutive tagged tuple patterns match distinct atoms or distinct integers in their first field after the tag. A failed guard or nested pattern skips the remaining distinct keys in the group. Repeated keys and mixed types retain ordered comparisons. The implementation reuses the BEAM table writer and follows the literal selection in OTP 29.1.1 beam_ssa_codegen.

The OTP VM verified the new dispatch and fallback paths using the C# image. Three focused tests passed. The fixture still differs from OTP in block and register layout. [Fixture](beam_shared_cons_tag.erl) · [Disassembly](disassembly.txt) · [VM probe](outcomes.txt) · [Tests](focused.txt).

All 1,366 sources were accepted and compiled. 398 images were equal and 968 differed. ELDAPv3 remained equal. Seven images changed and the OTP loader accepted all 968 differing images. The Code size gap shrank for edoc_layout, snmpc and xmerl_xsd, and grew for eldap, atomics, ets and xmerl_xpath_pred. The normalized function match counts stayed the same. [Corpus](otp-corpus.json) · [Code and loader](loader-and-code.txt) · [Function comparison](normalized-functions.txt).

Full runtime tests and readiness regeneration were outside this run.
