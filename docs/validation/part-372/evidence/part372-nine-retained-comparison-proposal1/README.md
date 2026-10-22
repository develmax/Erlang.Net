# Nine-cohort passive comparison successor

Prepare the binding only after final source review (including any later HAMT or mutable-node fix):

```powershell
python -B artifacts/part372-nine-retained-comparison-proposal1/prepare-binding.py --guard-review FINAL_REVIEW_JSON --guard-review-sha256 REVIEW_SHA --guards FINAL_GUARDS_JSON --guards-sha256 GUARDS_SHA --output artifacts/part372-nine-retained-comparison-proposal1/final-binding.json
python -B artifacts/part372-nine-retained-comparison-proposal1/compare.py --capture FRESH_NINE_COHORT_CAPTURE --output artifacts/part372-nine-retained-comparison-proposal1/actual1 --source-binding-proof artifacts/part372-nine-retained-comparison-proposal1/final-binding.json --source-binding-proof-sha256 PRINTED_BINDING_SHA
```

The binding preserves all 496 published guard paths and checks every final source hash. It accepts only the two independently reviewed existing compiler deltas in the old queue saved-entry binding. Any additional old binding change fails and requires a separate semantic proof. Final guard count can grow beyond 499, but a supplied reviewed SHA is mandatory: no blind refresh.

The published371 comparison algorithm, full-field policies, named code/reference relations, explicit native-only/own-topology exclusions and corruption controls remain unchanged. Captured Main DLLs must equal final live Main DLLs. No subject, compiler, original VM or network was executed during preparation. Actual acceptance must come from a fresh final replay, not earlier green evidence.
