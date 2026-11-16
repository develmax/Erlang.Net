# Part 1587 — Forward entry call results through protected calls

When the last expression is a catch around a simple call, keep the preceding call result in X0 until the protected call's arguments are ready. Reuse the existing register move scheduler and catch entry and exit instructions. Clear the consumed stack arguments before the protected call. The first call still runs outside the catch. No dependency was added.

The control fixture has identical disassembly for run/1 and agrees with OTP 29.1.1 for normal return, a throw from inside the catch and a throw before the catch. Its complete module still differs in Code and Attr. The three focused tests passed, including a managed regression for the exception boundary. The initial parallel test build hit a shared obj file lock. The build and tests passed when retried after the corpus tool build completed. [Fixture](beam_forwarded_catch_probe.erl) · [Disassembly](fixture-disassembly.txt) · [Outcomes](outcomes.txt) · [Tests](focused.txt).

The two released megaco_stats functions get_stats/1 and reset_stats/1 now use two stack slots and the same normalized instructions as OTP. [get_stats](get-stats.txt) · [reset_stats](reset-stats.txt).

All 1,366 release sources were accepted and generated. There were 390 Equal and 976 Different. ELDAPv3 was Equal from the C# ASN.1 generator. Only megaco_stats changed, with two newly matching normalized functions and no losses. Its Code chunk remains 2,299 bytes, versus 1,888 bytes in OTP. OTP accepted all 976 differing images for loading. The full runtime suite and canonical readiness were outside this run. [Corpus](otp-corpus.json) · [Function comparison](normalized-functions.txt) · [Loader and Code](loader-and-code.txt).
