# ETF snapshot comparison (local only)

Compare complete, already captured ETF values using the existing reviewed passive decoder. This utility does not run Erlang, BEAM, .NET, native code or CI. It does not prove where a snapshot was produced; supply previously reviewed provenance hashes.

One command from the repository root (choose a **new** output directory each time):

```powershell
python -B tools/Validation/EtfSnapshotCompare/compare.py --manifest artifacts/etf-snapshot-compare-local1/match59-full12.manifest.json --output artifacts/etf-snapshot-compare-local1/next-comparison
```

The recorded example compares all 59 contract rows and all 12 complete caller stacks from original run39043433351 against the final SDK capture. No row selection, stack projection or normalization occurs. Complete decoded values determine the verdict; raw byte equality is also reported, but differing ETF encodings of equal decoded values do not cause failure.

Manifest schema1 has exactly these fields:

```json
{
  "schema_version": 1,
  "baseline": {"tag": "OTP-29.1.1", "commit": "ad05823719d77c8faee87348ea39513d4e2f99c5"},
  "decoder": {"path": "path", "sha256": "reviewed SHA256"},
  "fixture": {"path": "path", "sha256": "reviewed SHA256"},
  "inputs": [{"path": "provenance input", "sha256": "reviewed SHA256"}],
  "comparisons": [{"name": "unique name", "left": {"path": "original.etf", "sha256": "reviewed SHA256"}, "right": {"path": "sdk.etf", "sha256": "reviewed SHA256"}}]
}
```

Replace example placeholders with actual paths and 64 hexadecimal SHA256 values. Paths resolve relative to the manifest directory unless absolute. The decoder must be the repository's `tools/Distribution/Erlang.GuestConnectedSendObservations/passive_etf.py`, SHA256 `A069AD650B3EA1BE6B90DE60117B84ED7A1A6B67128CC41E7ECAEFD2889C1713`. Its verified source bytes are imported directly; no duplicate parser or cached .pyc is used. Its existing accepted-term profile and limits remain in force; unsupported/truncated ETF is invalid input.

Outputs are JSON on stdout and `report.json` in the new output directory. Exact manifest bytes and bounded raw input copies are retained there. Existing inputs are read only; an existing output directory is rejected without writing to it. Exit codes:

- `0`: all complete decoded values match.
- `1`: valid input, at least one complete value differs.
- `2`: invalid/missing/malformed/hash-mismatched input or invalid output path. Invalid inputs take precedence over differences.

Independent comparisons continue after a missing/malformed snapshot. Invalid preparation inputs (baseline, decoder, fixture or provenance) prevent dependent decoding. Limits: manifest256KiB, each bound file16MiB, aggregate64MiB, at most16 comparisons and32 provenance inputs. No source/generated/module registration or shared documentation is changed.

Inventory considered: repeated original53/59/full12 peer hash/ETF comparison adapters; source-freeze/guard hash checks (`source_inputs.py`, source-plan scripts); parse-before-build gates in the proplist host checks. Only snapshot comparison is consolidated here. The other candidates retain their existing owners and implementations.

Local verification: `artifacts/etf-snapshot-compare-local1/test-results.json` records nine controls: final59+12 match, intentional difference, missing snapshot, malformed ETF, snapshot hash mismatch, fixture hash mismatch, wrong OTP baseline, wrong decoder, and no overwrite. The normal example performs eight bound-file checks, four complete decodes and two comparisons: **14 previously hand-wired verification operations, one invocation, zero Python source edits per new pair**. Earlier peer scripts could already bundle checks; no human-minute or CI-time savings have been measured or claimed. This directory is intentionally uncommitted and excluded from part362 staging.
