# Part 1588 — Reuse call-chain allocation after entry guards

A single-clause function can use the existing straight-line call planner when all guard tests have already been emitted at entry. Failures still branch to function_info with the incoming registers. The call planner uses its last-use analysis to save only arguments needed after the first call. Partially lowered guards, entry patterns requiring further matching and lifted closures keep their existing paths. No dependency was added.

The deterministic 656-byte fixture matches OTP 29.1.1. It allocates one Y slot after both guard tests, holds the first and second call results in X0 and ends in a local tail call. Three OTP VM outcomes agree, covering success and failure of each guard. The focused managed tests passed. [Fixture](beam_guarded_call_chain.erl) · [Disassembly](fixture-disassembly.txt) · [SHA-256](fixture-sha256.txt) · [Outcomes](outcomes.txt) · [Tests](focused.txt).

The release function httpc:cancel_request/2 matches the OTP instruction sequence after label normalization. It uses one stack slot and forwards the profile result into the external tail call. [Release disassembly](httpc-cancel-request.txt).

All 1,366 release sources were accepted and generated. There were 390 Equal and 976 Different. ELDAPv3 was Equal from the C# ASN.1 generator. Thirty-eight release images changed, with 33 newly matching normalized functions and no lost matches. The Code size gap decreased in 30 modules and increased by three bytes in wx_misc. OTP accepted all 976 differing images for loading. The full runtime suite and canonical readiness were outside this run. [Corpus](otp-corpus.json) · [Function comparison](normalized-functions.txt) · [Loader and Code](loader-and-code.txt).
