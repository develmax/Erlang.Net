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

Files required by live readiness and compatibility inputs are an exception to
the local-archive policy. Part440 publishes the exact historical Part402
8369-test comparison report and four400/402 JSON authorities. Their original
hashes and outcomes are unchanged. The actual8813-case CI report and compact
clean-checkout suffix receipts are retained under Part440; this does not
publish unrelated raw snapshots or process-memory dump files.
