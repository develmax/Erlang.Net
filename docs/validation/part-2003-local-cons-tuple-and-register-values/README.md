# Part 2003 local cons tuple decisions and outgoing register values

Windows OTP 29.1.1 ad05823719d77c8faee87348ea39513d4e2f99c5. The [uncached 276-file scope](status.json) took 523.564 s. Analysis took 343.882 s. This scope is 0 Equal, 276 Different, 0 Unsupported and 0 SourceRejected. All 276 changed images prepared in OTP. The [normalized comparison](normalized.terms) records 935 changed bodies, 39 new observed equal bodies and zero lost equal bodies. The accumulated byte ledger remains 539 Equal and 827 Different.

The shared cons entry now partitions disjoint tagged tuple heads by arity and tag before leaf frames. A nested mismatch, guard rejection or generic fallback keeps the original head and tail. Duplicate tags retain ordered matching. The [source cohort](source-cons-candidates.txt) has 35 functions across 30 Different modules. The measured beam_utils parent body still differs while its lifted callback remains normalized equal.

A collecting BIF can write directly to its outgoing X argument when the intervening effects and call arity prove that both the copy destination and temporary are safe. The existing GC roots, frame shape and exception placement are carried through the rewrite. The [saved-image model](source-register-candidates.txt) selected 258 modules. Its five predicted normalized gains were observed in ct_proper_ext, ct_quickcheck_ext and dbg_wx_mon_win. Those observations concern function bodies.

The compiler substitutes literal binary operands when the source binding dominates their use. It reuses Erlang binding analysis and preserves bound matches and anonymous function scope. The [source witnesses](source-binary-facts.tsv) include beam_asm, inet_gethost_native and dets_v9. Full file equality remains pending.

All 29 [focused managed checks](focused.stdout) passed, including the three new permanent tests. The [paired fixture](paired-generated.terms) matched 16 values, errors and effects. Loading the separately generated released beam_utils and beam_asm modules into OTP produced the same deterministic test BEAM and runtime results as the reference modules. See [released behavior hashes](released-behavior.json). [Twelve compiler and test inputs](source-transfer-2166.json) were integrated. The original [compiler diagnostics](initial-compiler-diagnostics.log) are preserved.

Full uncached corpus acceptance, whole-package regression and readiness regeneration remain pending. Publication checks are independent of this report.
