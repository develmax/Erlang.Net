# math backends

The managed runtime exposes all 26 public declarations in pinned OTP 29.1.1:
`acos`, `acosh`, `asin`, `asinh`, `atan`, `atan2`, `atanh`, `ceil`, `cos`,
`cosh`, `erf`, `erfc`, `exp`, `floor`, `fmod`, `log`, `log2`, `log10`,
`pow`, `sin`, `sinh`, `sqrt`, `tan`, `tanh`, `pi`, and `tau`.

| Module | Registered APIs | Change | Reference declarations | Verified compatible | Needs verification | Known differences |
| --- | ---: | ---: | ---: | ---: | ---: | ---: |
| math | 26 | +25 | 26 | 0 | 26 | 0 |

These are the current math implementation and ledger counts. The canonical
project readiness report requires a separate passing complete regression.

## Selection

Use the existing startup selection API. Create a separate runtime for each choice:

```csharp
await using ProcessRuntime managed = await ProcessRuntime.CreateAsync(
    new RuntimeLibrarySelection([RuntimeModuleSelection.Managed("math")])
);

byte[] originalMath = File.ReadAllBytes("math.beam");
await using ProcessRuntime original = await ProcessRuntime.CreateAsync(
    new RuntimeLibrarySelection([RuntimeModuleSelection.Beam("math", originalMath)])
);
```

Supply the unchanged, pinned original `math.beam` shipped by
`src/Otp/Original/Foundation/Erlang.Otp.Stdlib.Beam/Otp/`. The original `.erl`
and `.beam` remain unchanged. Both choices expose the same MFA calls through
`runtime.Modules.Call(context, "math", function, arguments)`.

`runtime.LibrarySelection.Modules` records the selected backend, exports,
assembly identity, and original-image identity when applicable. Selection is at
startup; it does not silently replace a rejected BEAM image with managed code.

## Implementation and compatibility

OTP's `math.erl` implements `pi/0` and `tau/0` as literal floats. The other
24 functions are native ERTS primitives. Both backends share the runtime's
C# primitive implementation. The original backend executes the original BEAM
bodies and retains their ownership; its native stubs bind explicitly to those
primitives.

The implementation preserves numeric argument conversion, left-to-right
`badarg`/`badarith` precedence, rejection of nonfinite results, accepted
underflow, and signed zero. `erf` and `erfc` use the supported operating system's
CRT/libm through the existing native bindings. No approximation package was added.

Current Windows validation passes 45 affected groups, including direct calls to
all 26 MFAs and an explicit log2 rounding regression. Both backends additionally
match 502,772 freshly acquired numeric/type/error cases, the same 248 whole
values/errors and five stack profiles. Complete ETF comparison checks IEEE
float payloads, including signed zero.

The expanded corpus found 2,785 differences in the previous runtime's Windows
log2 path. Pinned OTP uses `log(x) / 0.6931471805599453` on this official
Windows profile; the runtime now uses that exact expression. Both alternatives
have zero differences on the expanded Windows corpus. The original Erlang and
BEAM assets remain unchanged.

This establishes complete API availability and the recorded differential
corpus, not universal 100% compatibility. Exhaustive numerical/platform audit,
other supported operating systems, and a passing complete project regression
remain acceptance gates. All math ledger rows remain `Needs verification`.

See [the original port checkpoint](validation/part-520/math-port/README.md) and
[the expanded compatibility evidence](validation/math-compatibility-20261103/README.md).
