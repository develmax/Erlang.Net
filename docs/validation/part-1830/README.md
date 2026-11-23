# Part 1830 — Preserve initialized roots for leaf arithmetic

Binary arithmetic GC BIFs with exit-only failure can use X registers in frameless code. The existing leaf liveness and initialized-root proof now accounts for the arguments, result and live-count operand. The pass preserves the existing layout for guard failure edges. The pinned OTP 29.1.1 validator supplies the reference for checking and pruning GC roots.

A permanent fixture checks the framed guard fallback and the frameless arithmetic result. Seven ERTS controls agreed on the pinned, previous and candidate images. They cover boxed integers, an invalid record, badarith and the guard fallback. Public behavior controls on array, rand and string agreed on all three images.

The uncached run covered all 1,366 Windows release images in 426 seconds. It retained 400 Equal, 966 Different, zero Unsupported and zero SourceRejected. The byte equality status did not change for any module. ERTS admitted 113 of the 114 changed files through batch preparation. The crypto module passed ordinary loading and produced the same SHA-256 result as both controls.

Normalized comparison gained seven exact function matches and lost none. The candidate removed 198 frames that also have no frame in the matching OTP function, plus ten where the generated function key has no matching reference key.

[Corpus](otp-corpus.json) · [Summary](results.json) · [Normalized delta](normalized-delta.terms) · [Frame removals](frame-removals.terms) · [ERTS controls](module-execution.log).

The full local regression passed 9,744 of 9,746 cases in approximately 46 minutes. The interpreter capability case failed at the assertion requiring dispatcher completion after disposal. The alternating original-BEAM trace case timed out. Both passed on the same candidate binary when run separately. The interpreter case also passed on the prior compiler binary in isolation. The raw full run remains failed. Canonical readiness requires a passing full report and was not regenerated for this part.

[Full report](full-regression.json.gz) · [Full console](full-regression.log.gz) · [Interpreter candidate](interpreter-candidate.json) · [Interpreter prior compiler](interpreter-main.json) · [Trace candidate](alternating-candidate.json).
