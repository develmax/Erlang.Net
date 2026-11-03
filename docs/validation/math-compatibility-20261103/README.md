# Math compatibility checkpoint — 2026-11-03

The managed and original BEAM alternatives retain the same 26 public math APIs.
The original Erlang source and BEAM assets are unchanged. The pinned reference
is OTP 29.1.1, source commit `ad05823719d77c8faee87348ea39513d4e2f99c5`.

## Corrected difference

The official Windows OTP build uses the `!HAVE_LOG2` branch in
`erts/emulator/beam/erl_math.c`: `log(x) / 0.6931471805599453`.
`System.Math.Log2` is observably different. For example, the input immediately
above 1 produces float bits `3cb71547652b82fe` in OTP and
`3cb71547652b82fd` in the previous managed implementation.

The runtime now follows the pinned Windows expression. Other platforms retain
their direct log2 path. Both backend choices intentionally share the corrected
native primitive; original BEAM pi/tau bodies remain original code.

## Executed evidence

- `validated-managed.json` and `validated-beam.json`: each passes **502,772**
  complete numeric/type/error comparisons with exact float payloads and no
  tolerance. Each additionally matches all 248 whole values/errors and all five
  stack profiles (1,240 stack observations). Actual selected backend, image and
  loaded DLL identities are recorded. No silent fallback or old generation is
  admitted; all 24 native primitives retain the selected ownership.
- `isolated-affected.json`: **45/45 passed** using the actual merged source,
  with a clean test-project build. Includes all 26 direct MFAs, public startup
  selection, original-image native captures, pi/tau ownership and the new
  Windows log2 regression. The preceding merged-owner failure remains in
  `ownership-merged-red.*`; its assertion incorrectly treated pure pi/tau as
  native captures. The native capture count stays 24.
- `complete-red.json`: replaying the previous locally built runtime on the
  same complete oracle reproduces **2,785 log2 differences**, with no other
  numerical difference. This is baseline-binary evidence, not current-head
  acceptance. The runtime SHA identifies the executed baseline.
- `negative-controls.json` and `negative-bit-comparison.json`: one changed
  expected float bit gives exactly one mismatch and exit 1. A changed file with
  its original receipt is refused before comparison. Invalid input now exits
  normally with status 2 (`negative-final-identity.exit`). The first uncaught
  negative-input exception required stopping its specifically identified failed
  test process; the CLI now catches and reports such failures without a Windows
  error-reporting hang.
- `oracle/`: original native observations and their source/version/file receipt.
  `original.etf.gz` preserves the complete 40 MB corpus without editing it;
  decompress to `original.etf` before replay. The local native source was the
  existing admitted official Windows archive. Metadata captures exact OTP
  version and architecture. The receipt's expected source commit is not by
  itself proof of a source-built oracle.
- `source-bindings.json`: exact owned implementation/validator inputs. The
  captured shared source was an uncommitted integration tree based on
  `92ea9036d`; this evidence is not attributed to a future publication SHA.

Acquisition: `pwsh -File tools/capture-math-compatibility.ps1 -ErlangExecutable <pinned-erl> -Output <directory>`.
Replay: `dotnet run --project tools/Validation/Erlang.MathCompatibility -- <managed|beam> <directory> <report.json>`.

## Acceptance still required

The workflow `.github/workflows/math-compatibility.yml` is prepared for genuine
Windows/Linux/macOS comparisons of both alternatives, with the original
reference built or archive-admitted independently. Prepared YAML is not a CI
pass. Exact publication-head CI, current review, complete project regression,
and a verified merge into main are separate remaining delivery gates.

The current math ledger is not promoted to universal 100% compatibility using
only these Windows results. Numerical/argument/error/stack evidence and the
source audit are separate from physical VM layout, allocation limits and
scheduler/performance equivalence. No timing or performance claim is made.
