# Part 1846 — Preserve a call result as the root of a map update

The C# call-result rooting pass now handles an immediate nonfailing
`put_map_assoc` whose destination is X0 and original live prefix is empty.
It verifies that the saved Y value has one reachable use and that the
map has no competing X inputs. The rewrite leaves the value in X0 and
extends the live prefix to one. Later reads, different destinations,
and branches where failure remains observable retain their saves.

Pinned OTP 29.1.1 computes the live prefix in `beam_ssa_codegen.erl`.
`beam_validator:verify_put_map` checks that prefix and the initialized Y
slots before pruning the X registers and assigning the destination.
The C# implementation reuses the existing proof and compact operand
patcher. Field and live-count edits preserve operand suffixes and are
applied in physical order. No dependency or public MFA was added.

## Scoped results

The one-worker comparison recompiles 107 distinct pinned release
modules, including the 66 observed by projecting the pass over saved
Part 1845 outputs. It takes 342.65 seconds of comparison time alongside
the linked runtime test. It has 9 Equal, 98 Different, 0 Unsupported
and 0 SourceRejected. All nine prior exact files remain. Sixty-six
generated images change. Pinned ERTS prepares 65 and ordinarily loads
the on-load module pubkey_os_cacerts. Its repeated clear calls and
export list match the earlier candidate and release.

Four fully normalized functions gain equality and none lose it among
87 changed function bodies. They are
`edoc_layout_chunks:doc_content/2`, `rpc:init/1`,
`edlin_key:key_map/0` and `peer:start_link/0`. No complete raw BEAM
section gains or loses equality. The isolated four-module frame
observation agrees with the reference widths. The fixture compares
eight values on pinned ERTS across all three builds and includes a
later-read negative. Its permanent test detects the missed root on the
prior frozen candidate. The integrated focused test run passes 26 cases.

The first normalization command omitted the evaluator identity argument.
Its raw error is preserved. The corrected run uses the SHA-256 of the
evaluator, reuses the diagnostic cache, and does not change the compiler
or corpus data. The isolated staging path correction is recorded in
[the independent experiment](isolated-README.md).

[Source and compiler identities](source-freeze.json) ·
[Corpus](cohort.json) · [Timing](cohort.json.timings.json) ·
[Changed images](delta.json) · [Normalized functions](normalized-delta.terms) ·
[Sections](section-delta.json) · [Loader](loader.terms) ·
[On-load behavior](pubkey_os_cacerts-2.log) · [Fixture behavior](isolated-after-execution.log) ·
[Focused tests](focused.json) · [Initial diagnostic error](normalized-initial.stderr.log).
