# Part 1624 — Plan inline record field calls in entry branches

The entry planner handles both a call bound in the clause body and a call written directly as a record replacement. It uses the same checked tuple shapes and branch-local frame for both forms. Simple input registers and tuple projections feed the call using an order that preserves unread inputs. Pinned OTP Core record lowering and release disassembly provide the reference. No dependency was added.

The permanent beam_inline_record_branch fixture matches OTP byte for byte. The OTP VM confirms the successful update, call side effect and fallback branches. Three focused checks passed. [SHA-256](fixture-sha256.txt) · [Disassembly](disassembly.txt) · [Outcomes](outcomes.txt) · [Tests](focused.txt).

The corpus accepted and generated all 1,366 sources. 393 were Equal and 973 Different. ELDAPv3 was Equal. The generated release images were byte-identical to part 1623. The new parity is demonstrated by the permanent fixture. The OTP loader accepted all 973 differing images. The full runtime suite and readiness regeneration were outside this run. [Corpus](otp-corpus.json) · [Code sizes and loader](loader-and-code.txt) · [Normalized comparison](normalized-functions.txt).

The next work is to share entry tests across record-update branches and keep their frames local. [Baseline frame ranking](../part-1623/baseline-first-differences.txt).
