# Pattern scope and closure binding

## Status and current interpretation

Preserve source-defined evaluation and binding scope. The original record is an architectural constraint; additional syntax and runtime acceptance must be established by current source and evidence.

The owning current rules are [AGENTS.md](../../AGENTS.md); current acceptance is recorded in the [changelog](../../CHANGELOG.md) and [remaining work](../remaining-work/INDEX.md).

## Context and original decision

13. Map pattern keys evaluate against the scope preceding the whole pattern, separate from newly bound value patterns. Closures preserve captured key scope while their value-pattern variables shadow outer variables. Optional process context reaches nested patterns for guard BIFs such as self.

## Consequences and evidence

Read these original statements in their historical checkpoint context. Preserve their limits instead of inferring current completion. [Complete source record](../history/decisions/009.md) and [all decision history](../history/decisions/index.md).
