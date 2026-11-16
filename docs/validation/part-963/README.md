# Part 963 variable-width binary generators

Reuse sequential bit-segment reads for generators with widths from variables. Widths can come from an outer variable or an earlier segment. Strict generators preserve the row start for incomplete input and badmatch. The current variable-width path handles integer and binary segments.

The fixture matches OTP values and strict failure. Its bytes still differ. All three focused checks passed. [Previous probe](probe-before.json), [current probe](probe.json), [reference values](reference.txt) and [focused checks](focused.json).

The full Windows corpus has 223 Equal, 1124 Different, 12 Unsupported and 7 SourceRejected. Beam_disasm, diameter_types and beam_lib now emit BEAM. ELDAPv3 remains Equal. [Corpus](otp-corpus.json).
