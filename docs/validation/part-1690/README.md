# Part 1690

The structural list emitter frees the key register on branches reached through select_val. In the comparison chain it keeps the key live for subsequent tests. The reference is the release disassembly and switch emission in OTP 29.1.1 beam_ssa_codegen.

OTP VM verified matched branches, mismatched shapes, the fallback, nil and improper tail errors. Three focused tests passed. The fixture still differs from OTP, which extracts the common payload once before selecting the branch. [Fixture](beam_shared_cons_tag.erl) · [Disassembly](disassembly.txt) · [Tests](focused.txt) · [VM probe](outcomes.txt).

All 1,366 sources were accepted and compiled. 398 images were equal and 968 differed. ELDAPv3 remained equal. Three images changed with identical Code sizes, and the OTP loader accepted all 968 differing images. The select tables investigated in eldap, atomics, ets and xmerl_xpath_pred have matching key sets in the release. Their remaining differences include payload projection, branch ordering and other surrounding instructions. [Corpus](otp-corpus.json) · [Loader and sizes](loader-and-code.txt) · [Select tables](select-tables.txt).

Full runtime tests and readiness regeneration were outside this run.
