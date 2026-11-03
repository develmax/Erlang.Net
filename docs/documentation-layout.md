# Documentation layout

The 2026-11-03 maintenance split separates current navigation and project instructions from historical checkpoints.

| Entry document | Previous size | Current purpose | Full history |
| --- | ---: | --- | --- |
| [PROGRESS.md](PROGRESS.md) | 426,501 bytes | Navigation to readiness, plans and the changelog | [22 volumes](history/progress/index.md) |
| [NEXT_STEPS.md](NEXT_STEPS.md) | 293,891 bytes | Executable queue and planning rules | [15 volumes](history/next-steps/index.md) |
| [AGENTS.md](../AGENTS.md) | 322,534 bytes | Durable architecture, development and delivery rules | [17 volumes](history/agents/index.md) |

The original ordering, narratives, failed attempts, incomplete gates and superseded rules remain in the archive. The preservation manifest supports reconstruction of all three original snapshots, reversing link substitutions. Historical root-relative mistakes in AGENTS links were corrected against existing docs targets; the manifest retains the old destinations for reconstruction. Closed changelog volumes and validation snapshots were not rewritten.

Use the existing remaining-work index and active changelog for later work: the old entry documents started at Part485, while the maintained sources contain later checkpoints. Do not treat old publication instructions or historical test counts as current authorization or results.

## License locations

Four third-party license/notice files moved from docs/ to [licenses/](../licenses/README.md). The owner-local draft moved from the repository root to the same directory and is explicitly ignored. Upstream file notices remain next to their assets. Project inputs now read the new locations; NuGet license filenames and package destinations remain unchanged.

## Maintenance and verification

Keep entry pages short and link to the owning plan or report. Write new checkpoint narratives only in the active changelog volume. Archive snapshots are immutable; add corrections in the changelog. Git text conversion is disabled for archive snapshots and license texts to preserve byte identities.

Run from the repository root:

```powershell
node tools/Validation/check-documentation-layout.cjs
```

This reconstructs the preserved snapshots, checks SHA-256 hashes and sizes, validates license bytes and verifies local paths and Markdown anchors. The report is written to artifacts/documentation-reorganization/verification.json.

Validation performed for this maintenance: all three snapshots reconstructed, 1,284 local links checked with zero unresolved targets, 192 old AGENTS link destinations repaired, and all 44 changed license packaging inputs resolve. Local Release packing with --no-build --no-restore succeeded for Erlang.Otp.Stdlib.Beam and Erlang.Net.Tool (the tool required -m:1 after the sandboxed parallel invocation failed). All five license entries across those two packages match the moved texts byte-for-byte; the local draft is absent. These package checks use existing build outputs and do not establish a new runtime build or regression result. No packages were published.
