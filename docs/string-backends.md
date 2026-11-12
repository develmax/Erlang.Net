# string backends

Both implementations expose all **71 exports** of pinned OTP 29.1.1
(`ad05823719d77c8faee87348ea39513d4e2f99c5`) under the same `string:F/A` API.
The manual C# implementation and the unchanged original Erlang source/BEAM are
retained separately. Both execute on this project's .NET runtime.

The current [Part562 source audit](validation/part-562/README.md) covers all71
APIs and119 public source clauses, with a [per-API review table](validation/part-562/api-audit.md).
Both final backends match37145 observations each, including17103 complete
exception stacks, plus8522 upstream-inclusive cases and138 depth-profile
observations. Full regression passes9649/9649. The67 new permanent rows cover
converted patterns, lazy malformed suffixes, actual native search/membership,
long numbers and indexed Jaro matching. Exact source and assembly identities,
retained failures and the separate large-input timing boundary are in the evidence.
Original Erlang/BEAM and the C# alternative remain separately selectable; default
registration remains one native primitive. No universal percentage is inferred.

## Selection and integration

Use the startup selector or an `ErlangLibrary` manifest. The complete working
recipe is [HelloOtpString](../examples/HelloOtpString/README.md):

```powershell
dotnet run --project examples/HelloOtpString -p:StringBackend=Managed
dotnet run --project examples/HelloOtpString -p:StringBackend=Beam
```

The C# choice is `RuntimeModuleSelection.Managed("string", dependencies)`;
the original choice is `RuntimeModuleSelection.Beam("string", image, dependencies)`.
Select `erlang` and `lists`, and explicitly supply the pinned `unicode` and
`unicode_util` images. The example declares every dependency and verifies image
SHA256 values. Original numeric parsing uses the explicitly selected
`RuntimeNativePrimitives.InternalScalars413` provider. Its real bindings appear
in the runtime's native-provider receipt.

`RuntimeLibraryManifest.Load`, the MSBuild library task and runtime startup all
support managed dependency declarations. Missing selections and invalid images
fail without backend fallback. Startup receipts retain the backend, actual image,
BEAM input digest, assembly MVIDs and resolved dependency backends. There is no
runtime directory scan or live backend switch.

The default registry remains unchanged: it contains the shared native
`string:list_to_float/1` primitive. Consequently the default readiness table still
counts **1** string registration. The explicitly selected C# backend exposes
**71**, and original BEAM exposes the same 71 plus its two `module_info` exports.
Default API counts must not be confused with selectable implementation coverage.

Unicode segmentation, case mapping and normalization use the original pinned
`unicode_util`/`unicode` dependencies in either configuration. C# implements the
string algorithms and preserves chardata/list/binary structure, numeric tails,
source helper calls and exception locations. It does not invoke an Erlang VM or
original `string` as a fallback. `Rune` is used for UTF-8 decoding, not as a
replacement for OTP Unicode tables.

## Validation

```powershell
pwsh -NoProfile -File tools/validate-string-original.ps1 `
  -Output artifacts/string-check-unique -IncludeUpstreamSuite -DepthProfiles
```

Use a fresh directory. The command checks the admitted official OTP provider and
its execution inputs before and after acquisition. It compares both selected
backends using the same original Erlang observer, including complete values,
exception classes/reasons, full stacks and thirteen binary-backing observations.
Every worker is monitored and joined before its result is accepted.

The unchanged upstream `string_SUITE.erl` is retained with its Apache-2.0 notice
and pinned source digest. All 44 functional testcase functions run in original
OTP and contribute their 2112 unique exported calls to the differential corpus.
Performance measurement and Common Test doctest infrastructure are separate.
Additional cases cover every export, malformed arguments and UTF-8, mixed and
improper chardata, normalization, case expansion, numeric remainders, large
lexeme indexes and non-byte-aligned bitstrings. Backtrace depths 0, 1, 2, 3, 8 and
32 are compared separately.

VM-generated compiled-pattern reference IDs naturally differ. For these cases
the observer queries each actual live resource with `binary:match/2`; the
comparison requires matching tags, query results and a one-to-one resource
binding before comparing the entire stack. Raw outcomes remain intact. Removed
frames, absent bindings, modified queries and invalid resources are rejected by
negative controls. Arbitrary references are never ignored or normalized.

Current reports and exact acceptance scope are in the
[Part560 evidence](validation/part-560/string/README.md). The earlier
[incomplete checkpoint](validation/string-port-20261103/README.md) remains
immutable, including its original failing stack comparisons. Finite differential
coverage is recorded as such; it is not a proof for every possible input,
physical ERTS resource policy or operating system. Global project regression and
publication gates are reported separately from string contract comparisons.
