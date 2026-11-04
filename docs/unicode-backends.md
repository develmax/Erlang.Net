# unicode backends

The user-requested C# alternative implements all **22 exported signatures** in
pinned OTP **29.1.1**, commit `ad05823719d77c8faee87348ea39513d4e2f99c5`.
The unchanged original `unicode.erl` and `unicode.beam` remain under
`src/Otp/Original/Foundation/Erlang.Otp.Stdlib.Beam/Otp/`.
Both alternatives run on the project's .NET runtime. Native `bin_is_7bit/1`,
`characters_to_binary/2` and `characters_to_list/2` remain common BIF primitives.
The selected pinned `unicode_util` supplies normalization and classification data
to both alternatives; the C# port does not substitute the host Unicode database.

## Explicit selection

```csharp
var selection = new RuntimeLibrarySelection([
    RuntimeModuleSelection.Managed("erlang"),
    RuntimeModuleSelection.Managed("lists"),
    RuntimeModuleSelection.Beam("unicode_util", unicodeUtilityImage, ["lists"]),
    RuntimeModuleSelection.Managed("unicode", ["lists", "unicode_util"])
]);
await using var runtime = await ProcessRuntime.CreateAsync(
    selection, RuntimeNativePrimitives.InternalScalars413, CancellationToken.None);
```

Replace the last entry with
`RuntimeModuleSelection.Beam("unicode", unicodeImage, ["lists", "unicode_util"])`
to use original Erlang. Dependencies must be selected explicitly. Selection does
not load an ERTS VM, change the default registry, silently fall back, or switch a
live module. Startup diagnostics identify backend, exports, native entries,
dependencies, managed assembly MVIDs and original BEAM SHA256 where applicable.

The [example](../examples/HelloOtpUnicode/README.md) uses the same selection from
ordinary MSBuild `ErlangLibrary` entries:

```powershell
dotnet run --project examples/HelloOtpUnicode -p:UnicodeBackend=Managed
dotnet run --project examples/HelloOtpUnicode -p:UnicodeBackend=Beam
```

## Port boundaries

| Exports | Implementation |
| --- | --- |
| `characters_to_binary/1,3`, both `*_int/2` traps, `characters_to_list/1` | C# wrappers plus an iterative port of `ml_map`, input translators, output translators and `cbv` incomplete-prefix rules |
| `characters_to_binary/2`, `characters_to_list/2`, `bin_is_7bit/1` | Shared runtime BIFs; external encodings trap into the selected library |
| `bom_to_encoding/1`, `encoding_to_bom/1` | C# with OTP prefix order, aliases and function-clause failures |
| Eight `characters_to_nf{c,d,kc,kd}_{list,binary}/1` exports | C# streaming loops, pinned `unicode_util` normalization, 200-grapheme binary rows |
| `category/1`, `is_whitespace/1`, `is_id_start/1`, `is_id_continue/1` | C# guards, fast paths and category dispatch, pinned `unicode_util` properties |

The port preserves codepoint integers independently of binary encoding, nested
lists and binary tails, characters spanning binaries, endian defaults, malformed
shape exceptions, range errors, partial prefixes and OTP's residual term shapes.
External decoding intentionally follows OTP `cbv` rather than treating every
.NET decoder failure as the same `error` or `incomplete` result. Error construction
retains original source locations and `erl_stdlib_errors` metadata.

## Focused validation

The [Part563 receipt](validation/unicode-port-20261104/README.md) records 66,458 matching
primary observations per backend, a 490-case permanent subset, 29 matching backing
probes, 19 passing connected regression groups and both working MSBuild selections.
Original values are replayed with exact exception stacks. Four BEAM partitions
reconstruct the original corpus byte-for-byte; long cases run one per command
under the same two-minute command deadline after the retained aggregate timeout.

`tools/validate-unicode-original.ps1 -Output artifacts/a-new-unicode-run` acquires
the corpus through the previously admitted official OTP 29.1.1 provider. It checks
the provider execution inputs before and after acquisition, builds only the
Unicode validator dependency closure and compares both implementations separately.
Reports compare complete returned terms and complete exception class/reason/stack
terms. The observer's identical worker body is executed directly in batches on
.NET; its returned send value transports the complete outcome without stripping
or normalizing stack frames. Runtime-local pid transport is outside the outcome.

All upstream export declarations are checked against the managed registration;
selection diagnostics and missing-dependency refusal are checked independently.
The corpus includes encoding combinations and aliases, scalar boundaries,
one-byte inputs, malformed multibyte prefixes, split binaries, invalid list
structures, normalization errors and identifier/category boundaries.

This is scoped evidence, not a proof of universal equivalence or a refreshed
whole-project readiness report. Full regression is intentionally excluded by the
current user instruction. Exact results and remaining gates are recorded in the
current work-part receipt; no 100% claim follows from finite test counts.
