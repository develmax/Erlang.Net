# Root-only final cached replay

No build, original VM, dependency installation or network is performed. Run only after the final production source and its five Main DLLs are stable. Supply the final independently reviewed source guard file and its explicit SHA; all source guards and live DLLs are checked at both ends. Old 499 hashes must not be reused after a HAMT patch.

For the two additional original baselines:

```powershell
python -B artifacts/part372-retained-recipes1/capture-cached.py --output artifacts/erlang372-final-baselines1 --only erlang371 ordsets371 --source-bindings FINAL_REVIEWED_GUARDS_JSON --source-bindings-sha256 FINAL_REVIEWED_GUARDS_SHA
```

This captures 751 values/3755 profiles (4506 typed observations) and 436/2180 (2616). Existing cached own observers and their sources/dependencies are frozen byte-exactly, copied to a fresh directory and overlaid only with final Main Compiler/Runtime/Terms/Serialization/Differential DLLs. Exact source/DLL closure, owned process receipts and independent-stage outcomes are retained. Running this launcher does execute the own SDK; this preparation did not execute it.

The same launcher supports the six earlier retained stages (`erlang-old`, `erlang-new`, four `binary-*`). Those captures must be refreshed after a source patch that affects their dependencies. A previous green log is not current evidence.

Nine queue/proplists cohorts use the existing separately frozen `artifacts/binary369-retained-replay-proposal1/replay.py --output FRESH_OUTPUT`. They are nine stages, not three: queue-all/core/long plus proplist-base/extra/strict/binding/iterator/raw. Use the separately prepared 372 comparison source-binding successor; old 496 guard bindings must fail closed against final changed sources. Native-only and own-topology diagnostics remain visible under the unchanged declared protocol.
