# Part 1562 — Reuse a saved call slot while preparing X0

The saved-call planner can reuse a dying slot even if the next call loads X0 from a different saved slot. For a single outgoing move of the previous result and no constants, it loads the new X0 value into the other argument register, saves the previous result, and swaps the two registers. More complex move sets use the existing scratch-register path. The change reuses the argument emitter and adds no dependency.

The fixture reduces the frame from three slots to two and matches OTP's register moves. Four execution outcomes matched on the OTP VM. Three focused compiler tests passed. [Before](run-before.txt) · [After](run-after.txt) · [Fixture](beam_reuse_dying_call_slot.erl) · [Outcomes](fixture.txt) · [Tests](focused.json).

All 1,366 sources were accepted and generated. The byte comparison found 390 Equal and 976 Different. ELDAPv3 matched from C# ASN.1 generation. Only observer_html_lib changed. Its expandable_term/4 now uses the same frame size and swap as OTP, with a remaining difference in the order of independent moves. No whole function gained or lost a match. OTP accepted all 976 differing images for loading. The full runtime suite and canonical readiness were not rerun. [Corpus](otp-corpus.json) · [Disassembly](expandable_term.txt) · [Loader](loader-and-code.txt).
