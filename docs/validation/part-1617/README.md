# Part 1617 — Trim saved call frames before terminal matches

Terminal literal and tagged call matches now use the saved-call sequence planner. It reserves Y slots for values that survive an intervening call, trims expired slots before the final call, and matches the result in X0. OTP 29.1.1 beam_ssa_pre_codegen.erl and beam_trim.erl supplied the reference. No dependency was added.

The permanent beam_saved_terminal_match fixture covers one and two saved parameters, a saved parameter mixed with an immediate result, and a saved call result. OTP and C# produced byte-identical images and the same VM outcomes, side effect, and badmatch lines. Three focused checks passed. [SHA-256](fixture-sha256.txt) · [Saved inputs](disassembly.txt) · [Saved result](saved-result-disassembly.txt) · [Outcomes](outcomes.txt) · [Tests](focused.txt).

The corpus accepted and generated all 1,366 sources. 393 were Equal and 973 Different. ELDAPv3 was Equal. The change affected 8 images with a signed reduction of 305 bytes in the aggregate Code size gap. The instruction comparison after label normalization recorded 8 new matches and 0 losses. The OTP loader accepted all 973 differing images. The full runtime suite and readiness regeneration were outside this run. [Corpus](otp-corpus.json) · [Code sizes and loader](loader-and-code.txt) · [Normalized comparison](normalized-functions.txt) · [Release disassembly](asn1ct_func-generate.txt).

The next work is to carry Y-register liveness through branch joins. [Frame ranking](../part-1616/baseline-frame-rank.txt).
