# Part 1620 — Reserve heap once for wrapped record updates

The general expression emitter combines the heap reservations for a checked record update and a tuple that ends in that update with a constant prefix. When the last replacement value is a call, it passes X0 directly to update_record. The pinned OTP 29.1.1 beam_ssa_codegen heap scheduling and release disassembly provide the reference. No dependency was added.

The beam_wrapped_record_update probe exercises two case branches and a call result. OTP VM results, the call side effect, and case_clause agree. The probe still differs in allocations, dispatch and frame exits. Three focused regression checks passed. [Fixture](beam_wrapped_record_update.erl) · [Case disassembly](disassembly.txt) · [Call result disassembly](single-disassembly.txt) · [Outcomes](outcomes.txt) · [Tests](focused.txt).

The corpus accepted and generated all 1,366 sources. 393 were Equal and 973 Different. ELDAPv3 was Equal. The change affected 142 images with a signed reduction of 2003 bytes in the aggregate Code size gap. The instruction comparison after label normalization recorded 0 new matches and 0 losses. The OTP loader accepted all 973 differing images. The full runtime suite and readiness regeneration were outside this run. [Corpus](otp-corpus.json) · [Code sizes and loader](loader-and-code.txt) · [Normalized comparison](normalized-functions.txt) · [Release instruction comparison](logger-handler-watcher-handle-info.txt).

The remaining logger_handler_watcher:handle_info/2 differences include entry dispatch, per-arm frames, and the ignored nested case result.
