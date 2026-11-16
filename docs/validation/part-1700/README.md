# Part 1700

When the singleton-result cleanup turns a local entry block into a tail call, the writer can remove an empty allocation followed only by register moves and the call location. It emits call_only and rebuilds the Line table from surviving instructions. The rule follows the local tail call line handling in OTP 29.1.1 beam_ssa_codegen. No dependency was added.

All 1,366 Windows OTP 29.1.1 sources were accepted and compiled. 398 images matched byte for byte, including ELDAPv3 from the C# ASN.1 generator. All 968 differing images passed the OTP loader. The sole changed image was logger_disk_log_h.beam. Its terminate/3 matches OTP after label normalization and its Code gap decreased by seven bytes. [Corpus](otp-corpus.json) · [Function comparison](normalized-functions.txt) · [Loader and Code](loader-and-code.txt).

The OTP VM verified the fixture's value and side effect. All its disassembled functions match OTP and its Line chunk matches byte for byte. Atom ordering and the AtU8, Code, ImpT, LocT and Attr chunks still differ because generating the redundant move interned the return atom before later code. Three focused tests passed. Full runtime tests and readiness regeneration were outside this run. [Fixture](beam_frameless_singleton_call.erl) · [Disassembly](fixture-check.txt) · [Chunks](fixture-chunks.txt) · [Tables](fixture-tables.txt) · [Tests](focused.txt).
