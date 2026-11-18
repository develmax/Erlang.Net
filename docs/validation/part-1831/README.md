# Part 1831 — Branch-local frames and call argument lifetime

This local package uses Windows OTP 29.1.1 source
`ad05823719d77c8faee87348ea39513d4e2f99c5` and compiler base
`3ea4bc0f40e6c76c44582e735c38acd0d2008464`. The reference release and
pinned inputs are recorded in [Part 1823](../part-1823/README.md).

## Changes

- Keep GC BIF success and local failure liveness separate. The failure
  path keeps arguments used by the guard fallback.
- Move a branch-local heap test across a proven in-place list projection.
  Carry record shape proofs through direct private calls and reuse those
  proofs at compatible interior checks.
- Emit atom tail dispatch after existing forwarding rules. Use branch
  frames for selected record and list cases with ordinary calls. Reuse the
  leaf live-root proof to remove a frame from an isolated external tail.
- Admit closure-free call sequences to existing spill reuse. Preserve
  a saved X0 argument through safe Y-slot cleanup. Promote terminal
  temporaries whose lifetime ends before another call.
- Reserve implicit line-file index zero for the module basename.

The source references are `beam_ssa_pre_codegen` for frame placement and
live intervals, `beam_ssa_codegen` for heap and call argument scheduling,
`beam_core_to_ssa` for atom dispatch, `beam_validator` for initialized GC roots,
and `beam_asm` for line files. The C# compiler reuses existing register,
control flow, heap and source-location analysis. All emitted values come
from Erlang source and build parameters. Reference bytes are used for
comparison and execution controls. No dependency was added.

## Focused evidence

The current build passed the three focused harness selections. A
previous selection of all 400 equal modules preserved their equality.
In the final three-module focused retake, `diameter_callback` and
`ex_notebook` gained whole-file equality and `ELDAPv3` retained it.
The first integration attempt changed `ELDAPv3` because the new emission
path took precedence over the already-canonical forwarding dispatch.
Moving the new path after that dispatch recovered the expected bytes.

The independent ERTS controls cover the callback fast, fallback and
exception behavior and the nested record branch with fake remote
dependencies. Six nested-record controls agree with pinned OTP. The wx
GUI itself was not exercised.

## Full Windows corpus

The uncached four-process comparison compiled all 1,366 release inputs
in 437.7 seconds. The output contains 402 Equal, 964 Different, zero
Unsupported and zero SourceRejected. The two focused byte gains were
the only changes in equality status. The comparison found 467 changed
images against Part 1830.

The normalized function comparison recorded 29 new matches and two
losses in modules whose complete images remain different. The losses
are `erl_epmd:register_node/3` and `string:strip/3`. Both use an earlier
dispatch form that the new leading-atom path displaced. Subsequent local
work checks the precedence correction and the all-atom selector guard.
Comparing the sizes of equally numerous frames found 861 narrower and
10 wider total frame-size gaps. These are diagnostic counts.
[Function comparison](normalized-delta.terms) · [Section comparison](section-summary.json).

ERTS admitted the 466 ordinary images for batch preparation.
`crypto` required an ordinary load because it has an on-load entry.
The reference, previous and candidate images produced the same SHA-256
result after loading.

[Corpus](otp-corpus.json) · [Timing](timing.json) ·
[Changed statuses](status-changes.json) ·
[Changed images](changed-images.json) · [ERTS loading](loader.log) ·
[Crypto execution](crypto-execution.log) ·
[Focused tests](focused.json) · [Nested record execution](nested-execution.log).

The package uses .NET SDK 10.0.400 and runtime 10.0.11 on Windows x64.
The existing corpus tool ran with the exact current Debug compiler DLL.
The source and executable hashes accompany the unchanged release input
manifests.
[Source manifest](source-hashes.json) · [Executable hashes](hashes.json).

The full C# regression ran for 3,528.1 seconds and passed 9,741 of
9,748 cases. The original Jaro resource check, original dirty-query
corpus and alternating BEAM trace timed out. All three passed in
standalone runs on the exact candidate test binary. The Jaro
standalone timeout repeated with the explicit DOTNET_ROOT and PATH
overrides and passed when the absolute SDK path was used without
those overrides. Four halt tests failed because the SDK probe had
not been built at the standard path. After the standard build, the
four same-binary halt retakes passed.

The raw full report remains failed. Individual retakes are separate
evidence and canonical readiness was not regenerated from this run.
[Full report](full-regression.json.gz) · [Full console](full-regression.log.gz) ·
[Timing](full-regression-timing.json) · [Halt retake](halt-recovery.json).

The repository-wide layout check
reported one file. The two affected indentation locations in
GeneralEmitter have identical text in the base revision and candidate.
The original layout result is retained.
