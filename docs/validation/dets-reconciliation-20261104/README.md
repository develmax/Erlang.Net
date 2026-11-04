# DETS current-main and platform checkpoint

Main parent:42820e0c7. Both review findings were checked against current code and
fixed with a permanent production-engine regression. [Review result](review-regressions.json).
Windows standalone host: [managed57](windows-managed.json), [original57](windows-original.json).
The ordinary build passes with0 warnings/errors. This is an intermediate
publication checkpoint; Linux/macOS execution, final focused replays and source
plan pins remain active work, not claimed passes. The dedicated platform workflow
runs only the two file/DETS probes. No full regression or merge is authorized.

[Earlier local journal](local-journal.md) retains entries whose numbers collided
with main; main's own Part591/592 history was preserved. No review thread was
closed. See the subsequent results in this directory for the final matrix.
