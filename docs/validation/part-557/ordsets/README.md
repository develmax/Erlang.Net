# Part557: ordsets C# verification

Local, baseline-bound checkpoint on 2026-11-03. The isolated build predates the
concurrent merge of `origin/main`; these results do not validate the merged tree.

## Actual results

| Check | Evidence | Result |
| --- | --- | --- |
| Isolated test-project build | [build log](build-accepted.log) | Zero warnings/errors |
| Final collections/dependency regression | [870-group report](final-affected.json) | 870/870 passed, partial project selection |
| Managed original comparison | [managed receipt](Managed.json) | 12,247 outcomes, all 21 MFAs, whole ETF matches |
| Original BEAM on .NET comparison | [BEAM receipt](Beam.json) | Same 12,247 outcomes, all 21 MFAs, original image/source ownership checked |
| Fresh original repeat | [repeat receipt](oracle-repeat.json) | 6,427 provider files verified before/after; full oracle reproduced byte for byte |
| Earlier broad baseline | [baseline report](affected.json) | 1,889/1,898 passed: eight port `eacces` failures and one BEAM loader refusal control, before production correction |
| Failure reproduction | [raw managed output](before-fix-managed.etf), [first differences](before-fix-differences.txt), [failed report](final.json) | Missing native reverse helper source frames reproduced; original backend also hit the initial host helper's ten-second deadline |
| First corrected run | [report](fix-first.json) | Both full corpora pass; final regression additionally tests the final conditional native-binding correction |

The permanent test now owns a bounded actor with a 75-second corpus deadline,
matching the existing original-BEAM corpus convention. It awaits actor completion
and disposes the runtime. No production deadline changes or weaker expected
values are involved.

The initially smaller corpus also passed 6,543 and 10,413 observations. Those
earlier passes did not cover the malformed multi-set path that subsequently
failed. Only the final 12,247-row corpus is the retained acceptance corpus.

## Reproduction

```powershell
dotnet build tests/Erlang.Tests/Erlang.Tests.csproj --artifacts-path artifacts/ordsets-verification/final-build -p:NuGetAudit=false --ignore-failed-sources -m:1
dotnet artifacts/ordsets-verification/final-build/bin/Erlang.Tests/debug/Erlang.Tests.dll --case runtime/ordsets/managed-whole-otp-corpus --case runtime/ordsets/original-whole-otp-corpus --report artifacts/ordsets-verification/recheck.json
py -3 tools/Validation/capture-ordsets-compatibility.py --check
```

The full 870-case selection is recorded in `final-affected.json`. It was run
in batches of at most 64 exact cases to stay within the Windows command-line
limit; every batch exited zero. The report remains explicitly partial and is
not admitted as canonical readiness evidence.

## Source and compatibility review

[Source review](source-review.json) binds the three production files and pinned
reverse call PCs. [Validated binaries](validated-binaries.json) records the
actual isolated assembly/configuration hashes. The `source/` directory preserves
the reviewed production and test source for this checkpoint.

The source trampoline now preserves native reverse's current helper frame in
addition to pending non-tail continuations. Sorting/merge values, comparison,
numeric representative choice, callbacks and dynamic target resolution are
unchanged. The new oracle also records callback effects and validates them as
part of the complete outcome, without projection or normalization.

No whole-module promotion, performance identity, cross-platform acceptance,
package publication or external CI is claimed. Registration/full deltas are
0/0; the observed ordsets ledger remains 21 present, 6 verified, 15 pending.

## Integration gates

The shared checkout began a separate upstream merge during documentation
finalization. `CHANGELOG.md`, the canonical MFA ledger and progress/next-step
documents had unresolved conflicts. Do not overwrite those files with this
checkpoint's old snapshot. Apply the separately saved proposed source-guard and
evidence update after resolving the canonical ledger, regenerate API documents,
then run merged-tree regression/readiness. Keep the prior immutable readiness
comparison snapshot unchanged until a complete green report is available.

Full shared term ordering/equality and dependent `lists` helper closure remain
required before changing the fifteen pending classifications. See
[the module acceptance scope](../../../ORDSETS_COMPATIBILITY.md).
