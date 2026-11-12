# Part520 math port checkpoint

Date: 2026-11-03. Baseline: OTP29.1.1, commit ad05823719d77c8faee87348ea39513d4e2f99c5.

Managed math registration grows from 1 to 26 exports (+25). The unchanged original Erlang source and BEAM remain selectable. The 24 native operations intentionally use the same runtime primitive implementation in both choices; pi/tau execute as managed constants or as original BEAM bodies. No fallback or live migration was introduced.

## Validation

- `affected.json` / `affected.log`: 44/44 affected groups, including 26 direct MFA cases, managed/original whole results and stacks, backend ownership and strict selection controls.
- Original observer: 248 cases, plus 5 stack profiles, yielding 1,488 complete outcomes per backend. Whole ETF equality preserves float payloads and sign of zero.
- `provenance.json`: exact oracle identities and fresh/retained result SHA256 comparisons. The installed release execution files were checked against the retained official release manifest.
- `original-values.etf`, `original-stacks.etf`: fresh official OTP output, byte-identical to the retained expected files. Values SHA256 5CAF138C04D4490B7FA2E2E6C10BFA9F5AC0D13ECB5482F7D076C5146B5FD5F8; stacks SHA256 CA512DDD350A0332E2D4517BEB32336FC173D441EEAF34F480C853D067852BEF.
- Reproduce with `pwsh -NoProfile -File tools/validate-math-backends-original.ps1`; the script validates the pinned release and actual execution file digests before running the observer.
- `build.log`: test-project build succeeds with 0 warnings and 0 errors. The first build attempts failed on a temporary inaccessible catalog reference and existing ambiguous StringModule references; the implementation now uses its own math signatures and the test names are unambiguous. Earlier build logs remain under artifacts/math-build.log and artifacts/math-port.
- The complete project regression was launched independently. Its final results and final-source focused acceptance are recorded below when available. A passing affected run must not replace complete regression evidence.

## Acceptance limits

No math command is promoted to 100% compatibility. Cross-platform CRT/libm and complete numerical/source contracts remain unverified. The previous canonical readiness table remains a historical snapshot until a current complete passing regression and exact source-bound readiness generation are available. The explicit previous comparison snapshot remains `docs/validation/part-485/native-catalog/previous-tests.json` from `module-readiness-scope.json`.

The shared checkout already contains extensive unfinished runtime/native work. This checkpoint preserves it. No commit, push, merge, package publication or original OTP checkout modification is claimed.

## Complete regression result

`regression.json` records **9,077 passed and 154 failed out of 9,231 tests**.
All 44 math acceptance groups pass inside this complete run too. The failing
groups are outside that selection; failures include subprocess/port `eacces`,
native and driver checks, BEAM controls, and readiness documentation. Their
causes are not waived or asserted to be pre-existing without baseline proof.
See `full-regression-summary.json` and the complete `regression.log`.

The readiness command was attempted with the actual complete report:

```text
dotnet artifacts/math-port/final-bin/Erlang.Tool.dll readiness docs artifacts/math-port/regression.json artifacts/math-port/readiness
```

It exits 1: `Readiness requires a passing report and matching pinned inventory baseline`.
No partial report was substituted and no canonical readiness publication is
claimed. The remaining full validation script, publication, numerical audit and
cross-platform acceptance remain unrun gates.

Final-source acceptance: `final-build.log` succeeds (0 warnings, 0 errors), and `final-affected.json`/`final-affected.log` pass 44/44. This includes the public `ProcessRuntime.CreateAsync` managed choice. An isolated output directory was used so that this final test-only adjustment did not replace the assemblies used by the already-running complete regression. Runtime implementation is identical across those builds.
