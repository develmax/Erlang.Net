# Part 1623 — Allocate record update frames after entry dispatch

The tuple entry planner can separate a record update branch from a simple tuple-return fallback. It checks both input shapes before allocating a frame, extracts the call arguments into their registers, and saves the checked record through the call. The update and outer tuple use one heap check and the call result remains in X0. OTP 29.1.1 frame placement and release disassembly provide the reference. No dependency was added.

The permanent beam_record_branch_frame fixture matches OTP byte for byte. The OTP VM confirms the record update, the call side effect, and both fallback cases. Three focused checks passed on the final build. [SHA-256](fixture-sha256.txt) · [Disassembly](disassembly.txt) · [Outcomes](outcomes.txt) · [Final tests](focused-final.txt). An earlier broader rule made two existing checks fail. The final entry shape keeps those earlier code paths. [Earlier checks](focused.txt).

The corpus accepted and generated all 1,366 sources. 393 were Equal and 973 Different. ELDAPv3 was Equal. The change affected 4 images with a signed reduction of 209 bytes in the aggregate Code size gap. The instruction comparison after label normalization recorded 0 new matches and 0 losses. The OTP loader accepted all 973 differing images. The full runtime suite and readiness regeneration were outside this run. [Corpus](otp-corpus.json) · [Code sizes and loader](loader-and-code.txt) · [Normalized comparison](normalized-functions.txt).

The baseline rank found excess frames with equal live counts in 876 modules. Extend planning to further compound branch bodies. [Baseline ranking](baseline-first-differences.txt).
