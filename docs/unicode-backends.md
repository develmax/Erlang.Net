# unicode backends

The user-requested C# alternative implements all **22 exported signatures** in
pinned OTP **29.1.1**, commit `ad05823719d77c8faee87348ea39513d4e2f99c5`.
The unchanged original `unicode.erl` and `unicode.beam` remain under
`src/Otp/Original/Foundation/Erlang.Otp.Stdlib.Beam/Otp/`.
Both alternatives run on the project's .NET runtime. Native `bin_is_7bit/1`,
`characters_to_binary/2` and `characters_to_list/2` remain common BIF primitives.
The complete C# `unicode_util` alternative implements all **20 exported signatures**.
Selecting both managed modules requires no Erlang source, BEAM image, Erlang compiler
or ERTS VM at application startup or execution. Embedded Unicode17.0 data is derived
from the pinned OTP generator; it does not use the host Unicode database.
The original `unicode_util.erl` and `unicode_util.beam` remain unchanged too.

## Explicit selection

```csharp
var selection = new RuntimeLibrarySelection([
    RuntimeModuleSelection.Managed("erlang"),
    RuntimeModuleSelection.Managed("lists"),
    RuntimeModuleSelection.Managed("unicode_util"),
    RuntimeModuleSelection.Managed("unicode", ["lists", "unicode_util"])
]);
await using var runtime = await ProcessRuntime.CreateAsync(
    selection, RuntimeNativePrimitives.InternalScalars413, CancellationToken.None);
```

To use original Erlang, replace the last two entries with:

```csharp
RuntimeModuleSelection.Beam("unicode_util", unicodeUtilityImage, ["lists"]),
RuntimeModuleSelection.Beam("unicode", unicodeImage, ["lists", "unicode_util"])
```

Managed and BEAM choices may also be mixed explicitly. Dependencies must be selected explicitly. Selection does
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

## Managed utility implementation

| Utility exports | C# implementation |
| --- | --- |
| `cp/1` | Iterative OTP `cp/cpl` state machine, three lazy continuation stages, UTF8 decoding and original residual terms |
| `gc/1` | OTP Latin1/control/prepend, Hangul, regional indicator, extended pictograph/ZWJ and Indic states |
| `nfd/1`, `nfc/1`, `nfkd/1`, `nfkc/1` | Pinned recursive-decomposition data, stable CCC ordering, canonical composition, Hangul arithmetic and compatibility resegmentation |
| `uppercase/1`, `lowercase/1`, `titlecase/1`, `casefold/1`, `get_case/1` | Complete pinned mappings preserving integer/list result shapes and lazy tails |
| `lookup/1`, `category/1`, `is_wide/1` | Exact lookup maps, category ranges, width/variation rules and original argument guards |
| `pattern_whitespace/0`, `is_whitespace/1`, `spec_version/0`, three identifier predicates | Pinned literal tables and OTP clause order |

The data extractor accepts only the SHA256-pinned generated source. Boolean property
ranges are coalesced before binary search; the loader rejects overlapping ranges.
The embedded data reproduces byte-for-byte and retains Unicode3.0 and upstream
Apache2.0 notices. Production registration loads C# algorithms and data only.
Work slices use the runtime reduction/cancellation infrastructure; malformed inputs
retain original source frames rather than exposing C# implementation frames.

The [Part588 acceptance receipt](validation/part-588/README.md) records the new
fully managed closure separately from historical mixed-backend observations below.
Its finite-domain table audit covers every codepoint, all eight transition probes
per codepoint, the complete pinned normalization/grapheme conformance files, and
complete malformed-input outcomes including source stacks and binary backing.

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
