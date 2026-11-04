# String OTP integration - 2026-11-04

The C# alternative and original Erlang/BEAM retain all71 exports of pinned
OTP29.1.1. Startup/MSBuild selection is integrated. Original sources/images,
main's native-caller/async changes and unchanged allocation limits are retained.
No backend default or compatibility classification is changed.

Implementation and local validation are complete. The final published head's
review, required CI and merge state are tracked in
[PR105](https://github.com/develmax/Erlang.Net/pull/105).

## Current measured acceptance

- [Full regression](full-tests.json.gz):9706 unique tests pass,0 failures,
  2010.215 summed harness seconds. This is an actual uninterrupted complete run,
  not composed from focused receipts. No OOM or stack-overflow process crash.
- [Input integrity](input-integrity.json):14198 source/test/tool/example inputs
  and60 assembly/configuration files remain unchanged across retained checks;
  [exact identities](source-and-assembly-freeze.json.gz) preserve the scope.
- [Both backend comparisons](final-otp-comparison.json) independently match8522
  complete outcomes each:6364 values and2158 exceptions/full stacks, covering
  all71 exports and binary backing. Raw original/actual ETF, hashes, upstream
  acquisition and dependency identities remain in
  [the comparison archive](final-otp-comparison.tar.gz).
- Six depths0,1,2,3,8,32 independently match23 observations each (22 exceptions
  and one lazy malformed-suffix value), per backend. The unchanged upstream
  suite executes44 testcase functions and supplies2112 unique exported calls.
- [All71 API table](final-api-audit.md) binds119 public source clauses to the
  [current handler catalog](final-source-catalog.json) and measured outcomes.
- [Fourteen cooperation tests](cooperation14-tests.json) pass through actual
  process_info(reductions). Old-assembly negative controls reproduce all14
  omissions in their applicable preceding assemblies:4 length/reverse,2 slice,
  and8 raw/equality/empty-fragment cases. No runtime timeout is raised.
- Both MSBuild examples pass with equal results:
  [Managed](example-managed.json), [Beam](example-beam.json).
- [Build](final-build.log):0 warnings/errors. Canonical readiness is regenerated
  from9706 actual tests against the exact immutable previous9687 main unit report.
  [Readiness admission](readiness-admission.json) retains all733 command records,
  registered733/21modules, delta0; the default string row remains the native
  list_to_float/1, separate from71 opt-in exports.

See [final gates](final-gates.json), [raw test log](full-test-log.txt.gz) and
[local SDK/runtime](local-dotnet-info.txt). Summed test durations are not a
performance comparison or universal input/platform/resource proof. Classifications
remain unchanged. Current implementation identity is801d77a20945dc6801bde2654c22f89d173de0bc.
Accepted main106 changes CI/helpers/source-plan metadata only: the production
src/tests/build/examples trees remain identical. The original local run retains
its source/input identity; new combined-head CI is required for the infrastructure.
See [reconciliation proof](main106-reconciliation/functional-tree-identity.json).

## Preserved history

The [historical API table](historical-api-audit.md),
[historical summary](historical-audit-summary.json) and
[original local receipt archive](historical-string-receipts.tar.gz) retain the
2026-11-03 audit:37145 observations per backend and9649 full tests. These belong
to their original source/assembly identities, rather than current main acceptance.
Local receipt labels560/561/562 do not replace existing main journal parts.

Pre-cooperation comparisons are separately labelled. Superseded partial runs,
including the isolated setup's18 failures and the invalid focused-runner option,
remain in [development receipts](superseded-development-runs.tar.gz); they are not
acceptance. Full standard-path probe builds and the admitted provider profile
resolve that setup; unchanged port checks and provider smoke pass. Source-plan
predecessors and current source-only seals remain in source-plan-refresh/,
without fabricating native execution or promoting any contracts. The observed
repeated-search allocation regression is repaired within the unchanged2GiB
limit by operation-local successful compiled-pattern reuse.
