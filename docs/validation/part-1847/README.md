# Part 1847 — Remove unused stack frames from direct external tails

A leaf function with a direct external tail may use X registers for
its temporary values. The C# allocator now considers this form
alongside local tails. It rewrites `call_ext_last` to `call_ext_only`
when liveness, interference and the initialized root prefix prove that
no frame is needed. The referenced import must be a static external
target and its name and arity must pass the existing trapping-BIF gate.

Pinned `beam_ssa_pre_codegen:need_frame/1` allows a simple static
external tail. Its `is_trap_bif/3` keeps frames on the trapping
erlang BIFs. Dynamic remote calls and fun applications have separate
frame requirements. The permanent fixture has direct external and
ordinary return arms. The existing entry-tail cases cover trapping
controls. No dependencies or runtime APIs were added.

The saved Part 1845 image audit decodes the Code, AtU8 and ImpT chunks,
and uses the literal count from LitT. It projects the C# pass both with
and without the external-tail rule. Of 960 Different images, 264 have
a newly applicable rewrite. No offline Code stream becomes wholly
equal. This projection is diagnostic. The separate source compilation
uses production inputs and build profiles.

The first projection decoded full ETF literals even though the pass
uses only their count. It encountered 90 failures while opening such
tables. The corrected projection preserves those diagnostics and
completes with zero decoder failures. The first cohort command used
module names. The selector requires installation-relative BEAM paths.
The corrected input list uses those exact paths.

All 27 integrated focused cases pass. The permanent fixture is exactly
equal to pinned OTP. Seven outcomes on the original, preceding and
current compiler images agree in ERTS, covering two calls to the
external target, ordinary cases and invalid inputs. The linked release
comparison is separate from these fixture checks.

[Source and compiler identities](source-freeze.json) ·
[Projection summary](projection-summary.json) ·
[Focused tests](focused.json) · [Fixture execution](current-execution.log).

## Release cohort

The source comparison covers 305 distinct release paths in 1,367.35
seconds with one worker. It reports 9 Equal, 296 Different, 0 Unsupported
and 0 SourceRejected, retaining all earlier byte matches. Compared with
the full Part 1845 corpus, 272 generated files and 521 normalized
functions change. Forty-one functions gain equality and zero lose it.
Four of these gains were already verified in Part 1846. No complete
raw chunk gains or loses equality.

Pinned ERTS prepares 270 generated images. The two on-load files
pubkey_os_cacerts and zstd use ordinary loading. The certificate cache
calls and the compression and decompression observations agree with
the preceding candidate and release. The completed selection covers 230 of the 264 observed saved-image
candidates. The other 34 remain listed for the linked full recompile.

The diagnostic finds 22 comparable allocations closer to OTP and one
further away. The first affected frozen compiler for ct_doctest is
Part 1846. Its new root forwarding competes with an earlier slot
simplification. The stored four-way disassembly retains the observed
four-to-five-slot change as an open issue.

[Corpus](tail-cohort.json) · [Runtime](tail-cohort-receipt.json) ·
[Section comparison](section-delta.json) · [Normalized summary](normalized-summary.terms) ·
[Load](loader.terms) · [Zstd execution](zstd-2.log) ·
[Certificate cache execution](pubkey_os_cacerts-2.log) ·
[Earlier frame](doctest-1845.terms) · [Changed frame](doctest-1847.terms).
