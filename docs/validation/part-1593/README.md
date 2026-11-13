# Part 1593 — Skip unused tuple fields in entry clause frames

The separate entry frame planner omits a field of an input tuple when its binding occurs only once in the clause patterns and the existing reference analysis finds no use in the guard, the body or another pattern's operands. Tuple shape checks and repeated bindings still apply. No dependency was added.

The fixture agrees with OTP 29.1.1 on five outcomes, including guard rejection and matching and mismatching repeated bindings. The full fixture still differs because the grouped tuple arm uses a separate lowering path. Three focused tests passed. [Fixture](beam_unused_entry_tuple_fields.erl) · [Disassembly](fixture-disassembly.txt) · [Outcomes](outcomes.txt) · [Tests](focused.txt).

In erl_abstract_code:debug_info/4 the compiler no longer reads the unused fields of the first two clauses. OTP still uses atom dispatch and different register assignments for the remaining fields. [Release disassembly](debug-info.txt).

All 1,366 sources were accepted and generated. There were 391 Equal and 975 Different. ELDAPv3 and wx_misc remain Equal. Fifty-five release images changed and four more functions match after label normalization, with no lost matches. The Code size gap decreased in 54 images and increased by four bytes in ex_webview. OTP accepted all 975 differing images for loading. Full runtime and canonical readiness were outside this run. [Corpus](otp-corpus.json) · [Function comparison](normalized-functions.txt) · [Loader and Code](loader-and-code.txt).
