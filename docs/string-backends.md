# string backends

The manual C# alternative implements all **71 exports** declared by pinned
OTP 29.1.1 (`ad05823719d77c8faee87348ea39513d4e2f99c5`). This includes the
modern grapheme/chardata API, the obsolete list API, and the two internal
number-prefix functions. `StringModule.Exports` is checked against actual
original `string:module_info(exports)` by the differential runner.

**Status: partially compatible; full acceptance is not complete.** Export
presence and passing value comparisons do not establish 100% OTP compatibility.
Full exception stacks remain an explicit failing acceptance gate. No API is
promoted to fully compatible by this port.

The [retained checkpoint](validation/string-port-20261103/README.md) includes
complete original/actual outcomes, code/build inputs, provenance and explicit
failed acceptance gates.

## Explicit alternatives

- C#: `src/Otp/Managed/Erlang.Otp.Stdlib/Text/`. Opt in with
  `StringModule.Register(registry)` inside `runtime.InitializeModulesAsync`.
- Original Erlang: `src/Otp/Original/Foundation/Erlang.Otp.Stdlib.Beam/Otp/string.erl`
  and `string.beam` remain unchanged. Prepare/publish the original image in a
  separate runtime instead of calling `StringModule.Register`.

Both alternatives use the same MFA calls through
`runtime.Modules.Call(context, "string", function, arguments)`. The new C#
implementation is not installed silently into existing runtimes. It is not yet
integrated into `RuntimeModuleSelection.Managed("string")` or the canonical
default registry/readiness counters. Thus the existing default table can still
show the one native `string:list_to_float/1` registration.

The C# version retains the runtime's existing `list_to_float/1` primitive. Its
integer-prefix parser uses `BigInteger` and retains the original tail. Unicode
segmentation, mapping and normalization use explicitly loaded, pinned original
`unicode_util`/`unicode` dependencies executing on the project's C# runtime.
There is no subprocess, official Erlang VM, or generated Erlang AST in the
production C# string implementation. Unicode helpers have not themselves been
manually ported by this change. Missing dependencies are not replaced by BCL
string operations or a hidden call to original `string`.

The comparison runner shows the complete dependency preparation and module
registration recipe. Original `erts_internal` plus the explicit native catalog
are included for its original string profile's numeric primitives.

## Validation

```powershell
pwsh -NoProfile -File tools/validate-string-original.ps1 -Output artifacts/string-check-unique
```

The command requires the existing admitted official OTP 29.1.1 development
provider. It verifies its manifest and selected executable/library/source files
before and after execution, acquires independent ETF outcomes, builds the C#
runner, executes both alternatives, and retains both reports even if one fails.
Use a fresh output directory; previous evidence is never overwritten.

The fixture covers every export and checks complete returned terms, including
binary/list/improper-tail structure, not just flattened visible text. It covers
combining marks, emoji/ZWJ sequences, normalization, case expansion, split/search
directions, empty mixed data, malformed UTF-8, number remainders and old API
argument-validation order. A common original Erlang observer spawns, monitors
and joins each worker before returning its result. Raw exception class, reason
and complete stack are retained without stack-frame filtering.

Reports distinguish value/reason failures from stack differences. The validator
returns a failing exit code for either category. Full-suite regression, broader
Unicode conformance/upstream suites, resource/scheduler/backtrace-depth profiles,
canonical readiness publication and existing startup-selector integration remain
required acceptance work.

The 2026-11-03 expanded comparison matched **5,226/5,226 values and exception
class/reason outcomes** in each alternative: 4,193 returned values and 1,033
exceptions. Complete stacks matched 3/1,033 in C# and 1,020/1,033 for original
Erlang on .NET. The validator correctly returned failure. These counts are
finite-corpus evidence, not a module-wide compatibility percentage.

Thirteen additional binary-backing observations cover slices around the 64-byte
copy boundary, suffix/prefix/search, split/replace/take and trimming. Limited
binary slices retain the original backing through `SliceBits`; re-encoding an
equal text would have lost this observable property. The separate 71-export
smoke plus all backing observations passes on the frozen-runtime build.

The shared checkout entered a concurrent integration merge with conflicts after
the normal projects had built successfully. The later backing check therefore
uses a separately rebuilt stdlib and current observer over retained runtime
assemblies; unrelated stdlib sources are bound to `07da777f6`. Current source
integration, default registration and canonical readiness are not inferred from
that isolated check. `-RunnerDll` selects an explicit previously built validator
without compiling a conflicting checkout. Its hash is retained in provenance.

## Reference and reuse

The retained `string.erl` Git blob is
`33eaf44956fe832a88da5520ce1340bd6c29a0f4`, identical to the pinned tag. The
retained `string.beam` SHA-256 is
`37C66C6A5DF38AE239CA1EEDEDBD2CE2FBC4B8B4F3EB7605E115D0AB73FDA697`, identical
to the admitted official provider.

Use immutable Erlang terms and the existing runtime/serialization components.
`System.Text.Rune` and UTF-8 encoding are used only for scalar encoding/search
bytes; `StringInfo`, culture-sensitive comparisons, BCL trimming and invariant
case conversion are not substitutes for the pinned OTP Unicode rules. No new
package dependency or performance advantage is claimed. Upstream Ericsson/
Erlang contributors' Apache-2.0 attribution and packaged notices are retained;
see [OTP ports](OTP-PORTS.md) and [licenses](../licenses/README.md).
