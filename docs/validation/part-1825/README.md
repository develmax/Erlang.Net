# Part 1825 — Dispatch closed atom clauses with mixed results

A closed unary atom dispatch now groups clauses whose bodies are literals
or tail calls accepted by the ordinary call resolver. The dispatch sorts
atom keys by exact term order. Equal literal results share a return block
and that block writes the literal if any incoming key differs from it.
Ordinary external tail calls keep their own line-bearing blocks.

The change extends the current C# closed-atom lowering. The pinned
reference emits one `select_val` for the distinct literal branches.
OTP `beam_ssa_opt:ssa_opt_sw` also preserves multi-value switches unless
their structure permits an explicit simplification. No dependency was
added.

Two fixture functions cover different body kinds, a return of the input
atom and multiple keys that share a return value. An additional function
exercises guard fallback. All eight behavior checks passed on the original
OTP fixture and both C# fixtures. The candidate's whole-module regression
passed.

The no-cache four-process run used the same pinned inputs and the new
module-selection entry point. It covered all 1,366 release images in 363
seconds. It retained 400 Equal and 966 Different, with zero Unsupported,
zero SourceRejected and zero changed equality statuses.

ERTS admitted all six changed images. The normalized comparison gained
seven function matches and lost zero. Five are functions generated from
SSH flag conversions. The others are in `erl_features` and
`tls_gen_connection_1_3`.

[Full corpus](otp-corpus.json) · [Summary](results.json) ·
[Changed images](changed-images.json) · [Loader](loader.log) ·
[Normalized comparisons](normalized-delta.terms).

The compiler block passed 3,835 of 3,836 cases. The original Jaro resource case timed out during the parallel corpus run. The same test also timed out on the prior compiler under concurrent load. The isolated retake passed on the candidate binaries. Both reports are retained. Both workflows passed on the hosted compiler revision. The joined report passed all 9,746 cases and was used to regenerate readiness. Validation of the documentation revision is tracked on the pull request. [Hosted receipt](hosted-receipt.json).
