# Part 1850 — Preserve constructor order, call-result roots and Megaco build paths

The linked compiler integrates three independently verified changes.
The constructor emitter reuses its scheduled argument constructions
for pure single-value returns. Direct fields keep their order.
An append prefix lowered through an earlier Core let retains that
evaluation position. Pinned Core and SSA dumps identify these two
constructor layouts.

A nonfailing GC BIF that writes X0 can feed the next allocating
instruction through its X prefix when the saved Y copy has a single
use. The existing C# liveness and operand proof applies only after
verifying the producer and the consumer. The repeated-read control
keeps its saved value.

The release profile records own-library header paths under the
Megaco application directory added by the pinned megaco.mk code-path
option. It uses the separate flex build root where that profile
requires it. Explicit header mappings remain authoritative. The
metadata still derives from the input and build flags.

| Isolated origin | Equal | Different | Unsupported | SourceRejected | Measured gain |
| --- | ---: | ---: | ---: | ---: | --- |
| Ordered sibling construction | 4 | 10 | 0 | 0 | Three exact normalized function bodies |
| Adjacent GC BIF result | 4 | 5 | 0 | 0 | Pinned frame width in diameter_session:time32/1 and instruction equality when ignoring its unresolved number operand type |
| Megaco library include root | 13 | 46 | 0 | 0 | Thirty-four exact Dbgi sections |

The isolated scopes retain all earlier exact files and do not establish
combined release acceptance. The sibling cohort reports no normalized
losses. The Megaco image comparison finds 36 Dbgi-only changes, no
normalized changes and no section regression. ERTS admits all 36 and
runs module_info on each. The BIF comparison retains exact runtime
observations including ETS behavior, wide arithmetic and badarith.
Some BIF shape controls still have additional instructions.

All 31 selected integrated tests pass. The standard linked full
comparison and regression against the final cumulative source remain
pending. The independent audit preserves its original broader partial
selection and exact later scope. No external dependency or public
runtime MFA was added.

[Integrated test report](focused.json) · [Exact source and assemblies](source-freeze.json) ·
[Sibling proof](isolated-constructor-README.md) · [BIF proof](isolated-bif-README.md) ·
[Megaco proof](isolated-megaco-README.md) · [Megaco raw delta](isolated-megaco-delta.json).
