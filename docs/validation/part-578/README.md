# Part578 — file literal ownership and scoped PR evidence

Date:2026-11-04. Base:67809531bb2603202937ca0562e9dcf54174e298.
Reference:OTP29.1.1/ad05823719d77c8faee87348ea39513d4e2f99c5.
Literal cleanup is complete; **100% file compatibility is not achieved**.

## Results

| Check | Actual result |
| --- | --- |
| Clean validator/dependency build |0 warnings/errors |
| Test project compilation |0 warnings/errors |
| C# / original BEAM library corpus |408 exact comparisons per backend,0 differences/skips |
| Five connected regression cases |5/5 passing; Partial=true,9712 available cases |
| Literal audit |8 behavior/admission files,0 findings |
| Literal-audit controls |embedded atom and4096 rejected; documentation comment accepted |
| Corpus/source digest controls |both rejected for the intended digest reason; no success report |
| SDK layout |0 files need formatting |
| Full filesystem cycles |raw/normal/RAM/consult/compressed each fail in each backend |

Each complete cycle uses a fresh runtime. These ten failures preserve the existing
native/provider gaps; they are neither omitted nor accepted as expected API
values. In particular ordinary/raw opening reaches missing prim_file:open_nif/2,
and the RAM driver fails. Complete parser/compression/socket effects and native
resources/ownership/source-stack/platform/metadata acceptance remain open.

The main corpus includes69 exports, complete captured exception stacks,
descriptor/copy effects, positional batches, unsigned indirect headers,
monitored process termination, recursive deletion, callback sendfile restoration,
server command payloads and available real Windows read/info/list/getcwd paths.
Validation-only devices exercise protocols and are not production substitutes.
Both hosts reset fixture access time before each observation; returned timestamps
and stacks are compared unchanged. Original source/image are not modified.

## Reproduction

```powershell
dotnet build tools/Validation/Erlang.FileCompatibility -m:1
dotnet build tests/Erlang.Tests -m:1
py -3 tools/Validation/audit-file-literals.py . --self-test
pwsh -File tools/validate-file-original.ps1 -Admission <official-admission.json> -Output artifacts/file-fresh -CaptureOnly
dotnet tools/Validation/Erlang.FileCompatibility/bin/Debug/net10.0/Erlang.FileCompatibility.dll . artifacts/file-fresh Managed
dotnet tools/Validation/Erlang.FileCompatibility/bin/Debug/net10.0/Erlang.FileCompatibility.dll . artifacts/file-fresh Beam
# Each backend, each gate index0 through4; nonzero is currently required by the actual differences:
dotnet tools/Validation/Erlang.FileCompatibility/bin/Debug/net10.0/Erlang.FileCompatibility.dll . artifacts/file-fresh Managed runtime-gates.etf 0
```

The exact regression selection is in `connected-tests.json`. It contains four
prim_file410 host controls and `runtime/library396/file-encoding-private-source-owner`.
The literal audit intentionally permits obvious small tuple/argument indexes and
arithmetic identities; named source/MFA/protocol/layout/configuration values stay
with semantic owners. Independent Erlang fixtures retain concrete reference inputs.

`provenance.json` authenticates captured source/image/corpus identities and the
official provider closure checked before/after capture. The passing reports and
ten `*-runtime-gates-N.json` reports record actual assembly hashes. `source-bindings.json`
binds the task's source inputs; `runner-bindings.json` binds executable dependencies.
Negative-control logs, build/style/audit logs and raw ETF/BEAM bytes are retained
alongside this receipt. Recreated filesystem metadata needs fresh acquisition;
replaying copied ETF bytes at a different fixture location is not equivalent.

Full local regression/global readiness/manual CI dispatch were not run. No
compatibility classification or performance claim is promoted. See the
[backend guide](../../file-backends.md) and [English changelog](../../changelog/0105.md).
PR creation is authorized; merging and NuGet publication are not part of this part.
