# Validation archives and publication

Source code and permanent test fixtures are versioned together. The fixtures
contain the source/BEAM identities and complete captured outcomes used by
ordinary tests without an installed reference VM.

Large raw development snapshots under `docs/validation/*/snapshot` and ignored
`artifacts/` directories remain local. Published completion records and binding
inventories describe those retained snapshots; they do not imply that every
`PreservedAt` archive path is hosted in Git. Historical `NoPublication` fields
record the state at capture time and are not rewritten after later publication.

Fresh GitHub Actions results must be associated with the actual published
commit. A local native/original run, source audit, targeted test report and
headless CI run remain distinct evidence; none substitutes for the others.

The separately requested `LICENSE.local.txt` remains outside publication.
