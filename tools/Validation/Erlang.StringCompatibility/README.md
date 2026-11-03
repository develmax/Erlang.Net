# string differential validation

Run `pwsh -NoProfile -File tools/validate-string-original.ps1 -Output artifacts/string-check-unique`
from the repository root. The admitted original provider is a development oracle,
not a production dependency. The script refuses an existing output directory.

The fixture acquires inputs/expected complete outcomes from official OTP 29.1.1.
The C# executable runs the same observer BEAM on .NET with either the manually
ported string module or the original string image. The worker protocol uses
spawn_monitor and waits for both its actual result and normal DOWN.

Each report records all 71 signatures, exercised signatures, value matches,
exception reason matches, full stack matches, and separate differences. Raw ETF
files preserve every complete input/outcome. Thirteen additional binary-backing
observations include the 64-byte slice boundary. The executable exits nonzero for
any value, reason, full-stack or backing mismatch. Do not call a value-only pass full
compatibility. See [current scope](../../../docs/string-backends.md).

An explicit `-RunnerDll <path>` on the PowerShell entry point uses a retained
validator without rebuilding the checkout. This is useful during an unrelated
integration conflict; it is not proof that the current combined source builds.
