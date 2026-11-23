# Part 1840 — Encode oversized integers and retain singleton-result calls

In `beam_asm:encode_arg` the OTP 29.1.1 compiler uses the literal table
when the magnitude of an integer is at least 2^128. The C# assembler
now applies the same threshold to literal operands and rewritten
integer comparisons. It reuses the ordinary ETF literal table.

Pinned `beam_ssa_type:opt_local_return` exposes a local function's
singleton success type to its consumer. The C# compiler now replaces
all-known tuple fields with their literal result, including expanded
ordinary records. It emits the original calls in input order and
preserves errors, effects and bare tail calls. The emitter applies
this rule to a direct zero-arity body and to closed unary atom clauses.

The 30-module integrated comparison took 25.86 seconds including
setup. It reports 6 Equal, 24 Different, zero Unsupported and zero
SourceRejected. SSL's `ssl_dh_groups` gained whole-file equality. No
match was lost. The related `beam_asm` and `ssl_handshake` files
also changed. ERTS prepared all three changed files. Seven new
normalized function matches occur in `ssl_dh_groups`. The two affected
functions in the other images still differ.

The deterministic singleton fixture is byte-identical to the OTP
fixture. Both executions return the same eight observations, including
function_clause and a process dictionary effect from a retained call.
The deterministic signed threshold and deduplication fixture also
matches OTP exactly. Twenty regression selections passed from the
9,769-case catalog, including the repository's whole-module checks.

The isolated prototype first used an invalid C# pattern and retained
the raw compiler log. A subsequent prototype revealed a missing
branch call's line instruction; tracking physical branch order
retains it. The fixed two-pass candidate was built with zero warnings
and errors. The linked full corpus and regression are pending.

[Corpus](integrated-cohort.json) · [Timings](integrated-cohort.json.timings.json) ·
[Image deltas](integrated-delta.json) · [ERTS preparation](integrated-loader.terms) ·
[Normalized functions](integrated-normalized.terms) ·
[OTP fixture](reference.terms) · [Fixture execution](sealed-fixture-execution.log) ·
[Focused tests](integrated-focused.json) · [Identities](identity.json).

