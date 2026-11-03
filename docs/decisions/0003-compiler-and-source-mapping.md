# Compiler pipeline and source mapping

## Status and current interpretation

The original compiler design records an incremental implementation, not the current completion status. Consult current compiler and backend documentation for delivered capabilities and remaining scope.

The owning current rules are [AGENTS.md](../../AGENTS.md); current acceptance is recorded in the [changelog](../../CHANGELOG.md) and [remaining work](../remaining-work/INDEX.md).

## Context and original decision

5. Use dedicated Erlang lexer/parser/analyzer; borrow Roslyn only for C# lexical context. No regex language rewriting and no Roslyn fork.

6. Emit checked AST construction in generated C# as the first compilation representation. Keep direct lowering and optimized IR as explicit future compiler tasks.

7. Adjust the mandatory sample to an async process callback. Suspending receive must not block a C# public synchronous method or silently change its signature.

8. Supply #line plus original line-count preservation now; full expression columns and debugging require a later source-map representation.

## Consequences and evidence

Read these original statements in their historical checkpoint context. Preserve their limits instead of inferring current completion. [Complete source record](../history/decisions/009.md) and [all decision history](../history/decisions/index.md).
