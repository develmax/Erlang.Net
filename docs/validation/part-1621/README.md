# Part 1621 — Elide wildcard match result slots

Wildcard matches now emit the right-hand side without saving the result to a Y register solely for the wildcard. In a sequence, the emitter follows the same continuation path as for the right-hand side alone. In a value context, the match still returns its right-hand side. The pinned OTP v3_core.erl wildcard handling and release disassembly provide the reference. No dependency was added.

The beam_discarded_match probe verifies ignored case results, an unmatched selector, and a returned wildcard match. OTP VM results and side effects agree. The probe still differs in frame size, dispatch and tail calls. Three focused tests passed. [Fixture](beam_discarded_match.erl) · [Disassembly](disassembly.txt) · [Outcomes](outcomes.txt) · [Tests](focused.txt).

The corpus accepted and generated all 1,366 sources. 393 were Equal and 973 Different. ELDAPv3 was Equal. The change affected 247 images with a signed reduction of 5270 bytes in the aggregate Code size gap. The instruction comparison after label normalization recorded 2 new matches and 0 losses. The OTP loader accepted all 973 differing images. The full runtime suite and readiness regeneration were outside this run. [Corpus](otp-corpus.json) · [Code sizes and loader](loader-and-code.txt) · [Normalized comparison](normalized-functions.txt) · [Release instruction comparison](logger-handler-watcher-handle-info.txt).

The next differences in logger_handler_watcher:handle_info/2 include entry dispatch and per-arm frame allocation.
