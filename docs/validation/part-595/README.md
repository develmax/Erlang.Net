# Part595 whole BEAM emission

The C# compiler produces a complete deterministic BEAM image for modules whose declared functions are exported, have arity zero, and return an atom or a signed 32-bit integer constant. The supported source attributes are `module` and `export`. The source basename must equal the module name. The output profile is OTP 29.1.1 compiler 10.0.6 with `deterministic` and default debug settings.

The four fixture pairs in `tests/Erlang.Tests/Resources/Beam` are `beam_empty`, `beam_probe`, `beam_same_line`, and `tiny`. They cover the implicit metadata functions, multiple source lines, functions on the same line, and signed integer boundaries. The original BEAMs were captured with the Part376 admitted OTP provider using `erlc +deterministic -o <output> <input.erl>`. The C# output matched each full binary and loaded in the original OTP VM, where the exported functions returned the expected values.

`dotnet build tests/Erlang.Tests -m:1 --no-restore -v:q` passed with zero warnings or errors. The [focused harness report](focused.json) covers `compiler/beam/whole-module-matches-otp` and `compiler/beam/compact-operands-match-otp`.

Build the corpus gate with `dotnet build tools/Validation/Erlang.BeamCorpus -m:1`. Run it with `dotnet run --project tools/Validation/Erlang.BeamCorpus --no-build -- <otp-installation> artifacts/beam-backend/otp-corpus.json`. The gate compares against the actual BEAM files shipped in the installation, records a status for every image, and exits nonzero unless all images match.

| Installed Windows OTP 29.1.1 result | Count |
| --- | ---: |
| Source rejected by the C# parser | 1,364 |
| Parsed, outside the implemented emission profile | 1 |
| Missing source in the installation | 1 |
| Generated and equal | 0 |
| Total | 1,366 |

The generated ASN.1 module `ELDAPv3` has a shipped BEAM but no matching Erlang source in the installation. This corpus includes shipped example BEAMs and measures the installed Windows release, with the released bytes as-is. Reproducing release bytes also requires the original build inputs and per-module options. Full regression and canonical readiness regeneration remain outstanding. [Per-module report](otp-corpus.json).
