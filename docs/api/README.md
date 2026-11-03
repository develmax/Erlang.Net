# API documentation maintenance

Generated from the baseline-bound compatibility ledger. Edit the ledger, then regenerate. Rendering this snapshot does not revalidate current implementation bindings, refresh runtime registration counts or grant compatibility credit.

[All modules](../API_COMPATIBILITY.md)

Module pages contain complete command scopes and limits. The erlang module is divided by runtime topic. Tables and full evidence sequences are paginated at paragraph/row boundaries; identical evidence sequences share content-addressed records. Every evidence kind, path, SHA-256, scope and sequence position is retained.

## Regeneration

Run from the repository root:

```powershell
dotnet run --project tools/Build/Erlang.Tool -- api-compatibility docs docs
node tools/Validation/check-api-documentation.cjs
```

The documentation-only command validates ledger structure, baseline and all evidence file hashes. It preserves the ledger snapshot, including recorded source bindings, without checking those bindings against a changing worktree or recalculating runtime registration. The readiness command still validates actual runtime registration and current implementation source bindings before producing readiness reports; its validation is unchanged.

The checker compares every generated command, status, scope, limit and ordered evidence item with the ledger, verifies evidence bytes and checks local paths and anchors. Its report is written to artifacts/api-doc-split/verification.json. Output directories outside docs are supported; evidence links resolve to the source docs directory.

Generated files are listed in api/generated-files.json. Regeneration removes only previously listed, obsolete Markdown pages under api/; it preserves unlisted files. Historical evidence and the ledger are never rewritten by rendering.
