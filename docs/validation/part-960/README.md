# Part 960 extrema and float conversion guards

Emit non-allocating binary BIF instructions for min and max and a GC-aware unary BIF for float. Reuse Erlang auto-import resolution and preserve operand guard failure labels. The oracle fixture verifies term order across types and fallback after a failed float conversion.

All three focused checks passed. The fixture returns the same values as OTP 29.1.1. The emitted bytes still differ. [Reference instructions](reference.S), [previous probe](probe-before.json), [current probe](probe.json) and [focused checks](focused.json).

The full corpus has 223 Equal, 1117 Different, 19 Unsupported and 7 SourceRejected. Supervisor and shell_docs_markdown generate BEAM. Qlc_pt passes float and reaches round. ELDAPv3 remains Equal. [Corpus](otp-corpus.json).
