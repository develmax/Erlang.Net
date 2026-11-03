# Pinned math compatibility

This validator compares the actual selected managed and original BEAM backends
against a freshly executed OTP 29.1.1 oracle. The oracle source is
`Original/math_acceptance.erl`; the full-stack observer remains beside the
existing library dependency tests.

```powershell
dotnet build tools/Validation/Erlang.MathCompatibility -m:1
pwsh -File tools/capture-math-compatibility.ps1 -ErlangExecutable <pinned-erl> -Output artifacts/math-compatibility
dotnet run --project tools/Validation/Erlang.MathCompatibility --no-build -- managed artifacts/math-compatibility artifacts/math-compatibility/managed.json
dotnet run --project tools/Validation/Erlang.MathCompatibility --no-build -- beam artifacts/math-compatibility artifacts/math-compatibility/beam.json
```

Acquisition checks the installed exact version. CI additionally verifies either
the official Windows release archive and every extracted member, or the exact
Unix source commit before building. The capture receipt's expected source commit
does not replace that independent admission.

Each backend must expose precisely the 26 native declarations and compare
502,772 numeric/type/error cases using complete ETF equality, including every
float bit. Inputs cover finite binary64 exponent/sign patterns, subnormals,
domain neighbors, large integers at rounding midpoints and overflow, all
non-number term categories, and binary argument-error precedence. A fixed
generator supplies additional inputs independently of the C# implementation.

The original observer additionally checks 248 complete values/errors and five
stack-depth profiles. Reports retain exact failures rather than numeric
tolerances, plus the actual loaded assembly and original image identities.
Both backend runs execute even if the first comparison fails. Exit code 1
means a mismatch; malformed or incomplete input is refused.

The workflow runs on Windows, Linux and macOS. Prepared workflow files and
Windows-only passing comparisons do not establish that Unix CI has passed.
No finite corpus alone establishes universal numerical compatibility; complete
source/host primitive audit and genuine platform receipts remain necessary.
