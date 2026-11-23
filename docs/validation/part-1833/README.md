# Part 1833 — Preserve source facts through call and branch lowering

This local package extends the Part 1832 compiler with 404 complete
Windows OTP 29.1.1 byte matches. Each independent change was compared
with pinned OTP before the combined candidate was built.

| Scope | Focused evidence |
| --- | --- |
| Proper-list pattern tails | Six new operand annotations match OTP in five release images. Arbitrary heads and improper input controls retain their behavior. |
| Nested call argument order | `erl_init:if_loaded/2` now matches normalized instructions. The three outgoing moves in `tftp_engine:info/1` follow OTP. |
| Dead slots before a late call | Four changed images retain normalized matches. Three release functions gain the expected zero-slot continuation. Shared-label, live-Y and pre-existing trim controls keep their layouts. |
| Closed literal bindings | `inet6_sctp:peeloff_opts/2` uses two slots instead of three. `megaco_sup:init/0` uses three instead of four. Shadowing, guards and repeated pattern reads are covered. |
| Sibling list constructors | `megaco_sup:sup_spec/1` now matches normalized instructions. The equivalent Mnesia function drops from seven to five instructions. |
| Atom dispatch | `diameter_tcp:send/3` and `setopts/3` use OTP's order for independent terminal blocks and retain ascending switch tables. |
| Size result types | Ten images change in a 13-module sample. Forty-six new arithmetic operand annotations match OTP. The exact test includes resolved BIFs, a local override and the emitter's SSA option control. |
| Trapping tail heap check | The minimized `exit/2` select fallback uses the same zero-slot heap allocation and execution result as OTP. |

The new corpus timing option is separate from the result report.
`ERLANG_BEAM_CORPUS_TIMINGS=1` writes a sidecar with setup, overall
comparison and individual module durations. In the six-module control
the main report was byte-identical with timing enabled and disabled.

The reference sources are pinned `beam_core_to_ssa`, `beam_ssa_type`,
`beam_call_types`, `beam_ssa_pre_codegen`, `beam_ssa_codegen`,
`beam_trim` and `beam_validator`. C# reuses the current emitter,
scope guards, instruction moves and stack analysis. Timing uses the
.NET BCL stopwatch. No external dependency or public runtime MFA is
added.

An early branch trim variant lost whole-file equality in
`diameter_callback` because it acted before tail folding. Moving the
rewrite to the late pass recovered the byte match in its sample.
The first closed-binding retake produced Unsupported for an entry
case. Canonicalizing the singleton sequence exposed the existing
entry-case path and eliminated that rejection. The first complete
compiler-focused test for the trapping-tail variant timed out while
full corpus workers were active. The same test binary passed after
those workers stopped.

## Combined focused validation

The frozen candidate has 10 Equal and 34 Different on 44 representatives.
It has zero Unsupported and zero SourceRejected. All ten exact matches
were preserved. No new whole-file match appeared in this cohort.
Comparison took 196.88 seconds with two bounded workers.

There are 27 changed images and six new normalized function matches.
No normalized function match was lost. ERTS prepared 25 images in the
batch. `crypto` and `zstd` loaded through their ordinary on-load paths.
SHA-256, Zstandard compression/decompression and the two supervisor
initialization results agree across release, before and candidate images.

The first batch diagnostic used the wrong prepare_loading tuple shape.
The next attempt exposed a carriage-return suffix in Windows TSV paths.
The final driver strips it before decoding the module name. Both
diagnostic failures are retained.

The aggregate compiler test reached its running-time budget with the
three new selections embedded. Its body completed in 43.66 seconds
under a diagnostic timer. The same selections now have their own named
cases. All eleven targeted cases pass on the exact compiler hash.

[Corpus](focused-corpus.json) · [Timings](focused-corpus-timings.json) ·
[Normalized functions](focused-normalized.terms) · [ERTS preparation](focused-loader.log) ·
[Named tests](focused.json) · [Test timeout](unsplit-test-timeout.json) ·
[Hashes and counts](results.json).

## Full release comparison

The four-process uncached run compiled all 1,366 modules in 617.25
seconds. It retains 404 Equal and 962 Different with zero Unsupported
or SourceRejected. All complete-file statuses are unchanged. The
compiler changed 356 images. Two atom tables, two import tables and
three literal tables reached exact equality. No previously equal
section was lost.

The normalized comparison gained 39 functions and lost one.
`ssl_handshake:path_validate/9` reversed two tail argument moves
after folding immediately consumed literal bindings. The pinned
function has the literal error tuple move before the nil move.
The next linked candidate carries a focused correction and a
permanent deterministic fixture for that ordering.

ERTS prepared 354 changed images. `crypto` and `zstd` used ordinary
loading and returned the same SHA-256 and compression roundtrip
results. Per-module timings are preserved for the final package
partition. Repository-wide regression is pending on that candidate.

[Corpus](otp-corpus.json) · [Timing](timing.json) · [Section delta](full-section-delta.json) ·
[Normalized functions](full-normalized-delta.terms) · [ERTS preparation](full-loader.log) ·
[OTP SSL instructions](ssl-path-0.terms) · [Candidate SSL instructions](ssl-path-2.terms).
