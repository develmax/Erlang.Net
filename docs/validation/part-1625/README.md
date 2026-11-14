# Part 1625 — Preserve tail calls through wildcard matches

The function and tail emitters unwrap wildcard matches when the match returns the value of its right-hand side. A terminal call can then use the existing register and frame-aware tail paths. Pinned OTP Core wildcard lowering and release disassembly provide the reference. No dependency was added.

The permanent beam_wildcard_tail fixture matches OTP byte for byte for direct and nested wildcard matches. OTP VM results confirm return values, call side effects, and the absence of the tail caller in the failure stack. Three focused checks passed. [SHA-256](fixture-sha256.txt) · [Disassembly](disassembly.txt) · [Outcomes](outcomes.txt) · [Tests](focused.txt).

The corpus accepted and generated all 1,366 sources. 393 were Equal and 973 Different. ELDAPv3 was Equal. The change affected 9 images with a signed reduction of 110 bytes in the aggregate Code size gap. The instruction comparison after label normalization recorded 4 new matches and 0 losses. The OTP loader accepted all 973 differing images. The first corpus attempt saved images but failed to serialize the report after a .NET assembly load error. A full retry under explicitly selected .NET 10.0.15 produced this report. The full runtime suite and readiness regeneration were outside this run. [Corpus](otp-corpus.json) · [Code sizes and loader](loader-and-code.txt) · [Normalized comparison](normalized-functions.txt).

Continue measuring entry frame sizes and delayed projections. [Baseline frame ranking](../part-1623/baseline-first-differences.txt).
