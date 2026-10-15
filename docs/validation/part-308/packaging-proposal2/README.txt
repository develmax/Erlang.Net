Actual-bound part308 packaging proposal2. Author performed read-only gates and archival only; root alone mutates docs/index.

Source authority is fixed to sourcefreeze C1A2377E970ECDE5E2AF5C298D4EBC32BD0E45E59FD6F9EFDC2EB57BE6F5492D. composite-authority.json contains explicit evidence paths, root proof allowlist and exact required statuses. No root*308 glob is used. Dispatch/attempt files and the current stage-index guard are excluded; previous307 stage provenance is retained in its detached manifest.

Provenance is compact and complete: one deterministic ZIP holds all 2,189 immutable composite members, including the 2,129-source snapshot. All 1,032 logical311/312/ledger candidate files reconstruct from exact matching ZIP entries or 135 unique content-addressed blobs. Eight hundred eighty logical files reuse ZIP bytes; no duplicate 822/2129 physical baseline source trees are copied. compiled-source-closure.json remains separately archived. verify308-portable-evidence-replay2.py performs a passive full replay and prints its result without rewriting archived guards.

ComparisonTestReport=validation/part-307/tests.json intentionally names the previous baseline for MFA deltas. Current actual tests are separately pinned and copied to part308/tests.json; the registered delta is 505-504=1. Raw505 bytes are preserved. Only process_info/1 and the new indirect load_nif/2 Limits disclosures may differ, with exact root guard Changes linkage. PublicLoadNifRegistered is true; default native-load instruction handling and general native ABI remain false.

Root sequence:
1. Run bind-actual-evidence.py (optional authority path; defaults to composite-authority.json).
2. Run part308-readiness-proposal.py config-actual.json, then the readiness CLI.
3. Rerun bind-actual-evidence.py to pin actual readiness output and copy index.
4. Run part308-finalize-proposal.py config-actual.json exactly once.
5. Run part308-stage-guard-proposal.py config-actual.json for a read-only check; use --apply only when root intends staging.

Stage guard uses git check-attr and records raw/staged hashes and explicit modes. All CSharp and proof bytes remain exact; only declared text source paths may normalize CRLF to LF. Closed0045 is pinned; active0046 remains at most32768 bytes/400 lines. Finalizer archives exact config/index/authority and their hashes. A ready handoff freezes source files: later changes require a new revision and prior notice, never an in-place edit during root execution.
