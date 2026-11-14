# Part 1622 — Elide dead call result bindings

The sequence emitter drops the binding of a fresh variable assigned from a static call when the remaining expressions never read it. Existing lexical capture analysis checks guards, patterns, and nested functions. The call still executes once and its exceptions propagate. Pinned OTP Core variable use and release disassembly provide the reference. No dependency was added.

The permanent beam_dead_call_binding fixture matches OTP byte for byte. A separate VM probe checks a live closure capture, repeated-variable matching, and call side effects. Three focused checks passed. [SHA-256](fixture-sha256.txt) · [Disassembly](disassembly.txt) · [Scope probe](scope-probe.erl) · [Outcomes](outcomes.txt) · [Tests](focused.txt).

The corpus accepted and generated all 1,366 sources. 393 were Equal and 973 Different. ELDAPv3 was Equal. The change affected 11 images with a signed reduction of 83 bytes in the aggregate Code size gap. The instruction comparison after label normalization recorded 0 new matches and 0 losses. The OTP loader accepted all 973 differing images. The full runtime suite and readiness regeneration were outside this run. [Corpus](otp-corpus.json) · [Code sizes and loader](loader-and-code.txt) · [Normalized comparison](normalized-functions.txt).

The next frame work is to quantify where entry argument spills can move to individual branches.
