# Explicit gen_statem alternatives and semantic constant ownership

The user requested a complete C# port of pinned OTP-29.1.1 `gen_statem`, retaining
the original Erlang implementation and explicit backend selection, then requested
removal of magic strings/numbers and a PR. This authorization is specific to this
module. It does not change the policy for other pure OTP modules.

Retain the original source and BEAM byte-for-byte. The opt-in managed library uses
the existing runtime timers, receive, aliases, monitors, process registration and
source-wrapper infrastructure. Both alternatives call the selected original
`gen`, `proc_lib`, `sys`, logging and formatting helper closure. No implicit backend
fallback or live state migration is introduced. Long-lived C# state crosses the
opaque source-wrapper boundary; borrowed-heap or performance parity is not claimed.

Own names by their role: function names, module names, process protocol atoms,
status keys/labels and format directives have separate catalogs. Equal literals
in different roles are not merged. Observable error frames retain explicit pinned
source coordinates and private function arities. Never derive one clause's line
from another clause's line. The original sys record layout has named positions.
Validation paths, provenance fields, reflection names, boot protocol, operation
deadlines and corpus dimensions have their own owners. Structural tuple patterns,
public MFA signatures, array indexing of API arguments and arithmetic identities
remain local where their meaning is explicit.

The original Erlang observers are contract fixtures: their deliberately invalid
terms and boundary values stay literal and their source lines stay stable. The
runtime BEAM badfun correction retains the failed application and suspended caller
as distinct frames even when both frames have identical source coordinates. Full
stack equality must not remove either frame to make an observation pass.

Reuse and license decisions are in [dependency decisions](../dependency-decisions.md).
The [gen_statem publication receipt](../validation/gen-statem-port-20261104/README.md) records finite focused
acceptance. It does not certify universal OTP compatibility, metadata identity,
resource/platform parity or the complete upstream suite. No generated global
readiness/API classification is promoted and no package is published.
