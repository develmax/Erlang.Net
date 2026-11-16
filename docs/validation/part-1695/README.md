# Part 1695

Stack trimming now crosses tests and BIFs whose failure is either immediate or targets an error block that does not access Y registers. It uses the pure and exit BIF classifications from OTP 29.1.1 erl_bifs and the two cost rules from beam_trim. The pass reuses the existing BEAM reader and writer. The classification table carries the upstream Apache-2.0 notice. No runtime dependency was added. Functions with exception handlers and branches into regular continuations remain outside the pass.

The Windows corpus admitted and compiled 1,366 sources. 398 images matched byte for byte, including ELDAPv3 from the C# ASN.1 generator. 968 differed and all passed the OTP loader. 87 images changed. One additional function, dbg_wx_trace_win:button_area/1, matches normalized OTP disassembly. No previous function match was lost. [Corpus](otp-corpus.json) · [Function comparison](normalized-functions.txt) · [Loader and Code](loader-and-code.txt).

The OTP VM verified the external call and the successful and failing tagged matches. The external function matches OTP disassembly. The matching function still differs in the order of tuple extraction and heap checking. Three focused tests passed after a clean build with zero warnings. [Fixture](../../../tests/Erlang.Tests/Resources/Beam/beam_trim_failure_edges.erl) · [Disassembly](fixture-check.txt) · [Tests](focused.txt).

Missing trim still affects at least one function in 867 differing images. Excess move affects 967. The next comparison will focus on general call argument moves and saved-register scheduling. Full runtime tests and readiness regeneration were outside this run. [Before](opcodes-before.txt) · [After](opcodes-after.txt).
