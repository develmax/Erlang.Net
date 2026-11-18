# Part 1827 — Reuse tuple projections across one failure edge

The C# compiler removes a repeated tuple-field read when a forward
failure label has exactly one predecessor and the same projection
sequence executes immediately before the predicate. No projection
may overwrite a source used by that sequence. If a heap check occurs
before the repeated sequence, every X destination must survive it.
The existing compiler control-flow reader provides the predecessor
proof.

Pinned OTP `beam_ssa_opt:ssa_opt_cse` propagates expressions along
successor edges, intersects available expressions at joins, and
invalidates a speculative result on its failure edge. This change
covers an already evaluated projection with stable source and
destination registers. It reuses the current C# optimizer and BCL
types.

The permanent positive fixture now checks that two preserved roots
feed the cons allocation directly. The negative fixture selects a
different record field in the fallback. Under pinned ERTS the release,
previous C# and candidate C# fixtures each passed the seven behavior
checks. Both real `diameter_callback:handle_answer/4` branches passed
on all three images. The compiler's whole-module focused regression
passed.

The 45-module representative run retained nine Equal and 36 Different.
ERTS prepared all 31 changed representative images, with one more
normalized function match. The full uncached run used four isolated
processes and took 452 seconds. All 1,366 modules retained their
statuses of 400 Equal, 966 Different, zero Unsupported and zero
SourceRejected. There were zero gained or lost whole-file matches.

The candidate changed 413 images and 1,221 normalized functions.
`diameter_callback:handle_answer/4` and `mnesia_log:strip_snmp/1`
now match after normalization. No normalized matches were lost.

ERTS prepared 411 changed images through the batch loader.
The release and both generated images of two modules declare
`on_load` and require the ordinary load path. All three images
passed both the certificate-list read in `pubkey_os_cacerts` and
a compression round trip in `zstd`.

The release still differs in 966 files. Further work includes
combined stack and heap allocation and call-result scheduling before
trimming. The pre-change scan found 1,474 adjacent spills and loads
in 409 functions across 220 modules that use trim. A candidate must
prove that reordering reduces a frame and preserves the retained
argument values. Full regression is tracked through the required
hosted workflows.

[Summary](results.json) · [Corpus](otp-corpus.json) ·
[Changed images](changed-images.json) · [Normalized delta](normalized-delta.terms) ·
[Loader](loader.log) · [Fixtures](fixture-ert-results.log) ·
[On-load execution](on-load-execution.log) · [Next diagnostic](scan-spill-before-trim.terms).
