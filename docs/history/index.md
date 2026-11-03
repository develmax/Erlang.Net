# Documentation history

These ordered snapshots preserve the complete previous entry documents, including failures, pending checks, superseded instructions and old paths. They describe their recorded revisions; use the current entry pages for navigation and the active changelog for new work.

- [Progress history](progress/index.md)
- [Next steps and handoffs](next-steps/index.md)
- [Historical agent instructions and checkpoints](agents/index.md)
- [Compatibility checkpoints](compatibility/index.md)
- [Architecture decisions and historical acceptance records](decisions/index.md)
- [Current progress](../PROGRESS.md)
- [Current executable queue](../NEXT_STEPS.md)

Volumes are approximately 20 KiB, split at paragraph boundaries. Explicit historical heading anchors preserve links into the split documents. Relative links are rebased to retain their targets; pre-existing missing evidence remains recorded rather than fabricated.

The [preservation manifest](manifest.json) contains ordered chunk hashes, original snapshot lengths and hashes, and reversible link substitutions. Run `node tools/Validation/check-documentation-layout.cjs` from the repository root to reconstruct all three snapshots in memory, verify license bytes and check local links. The report is saved under `artifacts/documentation-reorganization/verification.json`.

Archived bodies are immutable. Put corrections and subsequent work in the active [changelog](../../CHANGELOG.md).
