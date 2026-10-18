# Part372 controlled evidence publication plan

Preparation only writes this artifact folder. It never copies to Main/docs, edits Part371, builds, runs subjects or uses the network. `copy-plan.json` is an exact source/destination/hash list for the parent to review and copy. It preserves failed native CI, failed pre-fix tests, intermediate captures/differences and corrected captures as distinct evidence, with no implicit final-success credit.

Regenerate after final source guards/captures/regression/package outputs settle:

```powershell
python -B artifacts/part372-evidence-copy-proposal1/prepare.py --add-prefix artifacts/FINAL_NEW_OUTPUT_DIRECTORY
```

Only controlled artifact prefixes are allowed. The validation checkout, compiled DLL/bin/obj/cache trees, private/single license files, original pinned authority copies, producer snapshot duplicates and cached own observer source copies are excluded. Raw original ZIP and unpacked ETF/BEAM/metadata are retained: these are evidence, never executed by the passive replay. Old Part371 evidence and the already committed Part372 branch-cleanup archive are referenced and protected by a complete byte inventory, not copied again.

`producer-packet6.patch` is a byte-preserving full-index Git diff from published Part371230de4 to actual native producer76c034. The allowed changed paths are limited to three new collector tools, the dispatched workflow and line-ending attributes. `producer-commit.raw` preserves the exact commit object, verifies its Git object SHA, and records its sole published-base parent and expected final tree. This reconstructs the exact producer Git object using the existing base commit instead of copying the whole repository or original OTP source tree. The committed cleanup bundle covers the previous21 producer branches and does not itself contain this new372 producer. The original OTP pin/source inventory and producer Git SHA receipts remain in raw native/admission metadata.

Immediately before copying, verify every source file still has the recorded size/hash and every existing destination is absent or byte-identical. Never overwrite a conflicting destination. Verify all `protected-existing-evidence.json` rows again after copying. The root must add final evidence and mark the publication complete; current `ReadyForFinalCopy=false` is intentional while final work continues.

For later replay, restore evidence to its original `artifacts/...` paths from the manifest in a disposable checkout; keep the published Part371 base available, then apply the producer patch in a separate validation worktree or restore the archived producer Git ref. Passive audits/comparisons consume the retained bytes and owned receipts without executing downloaded VM/BEAM code. SDK observer reruns require rebuilding their source separately because DLL/cache publication is deliberately excluded.
