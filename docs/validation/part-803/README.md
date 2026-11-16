# Part803 Constant menu record and complete Observer image

The C# compiler expands an ordinary record construction into its tuple, using explicit values and declaration defaults. It folds the menu list to a literal and substitutes constant bindings into the tail call while keeping its incoming menu bar argument. Existing register move scheduling produces the OTP order.

The 700-byte fixture contains the upstream create_menus/1 body and record declaration with the Apache-2.0 notice. OTP 29.1.1 compiled it with `compile:file(Source, [binary,deterministic])`. The complete fixture matches C# output byte for byte. [Oracle script](oracle.eval) and [byte comparison](oracle.txt).

The complete cdv_detail_wx module now emits a 4,876-byte BEAM, compared with 19,908 bytes in the release. AtU8, Code, ImpT, ExpT, LitT, LocT, Attr, CInf, Dbgi, Docs and Line differ. The disassembly shows the first instruction-count difference in display_progress/2 and an absent list comprehension helper for create_pages/3. handle_event/2 and terminate/2 also have different instruction counts. The current emission has no release build profile for this module. [Disassembly summary](disassembly-summary.txt) and [comparison script](compare.eval).

The corpus tool was built with `dotnet build tools/Validation/Erlang.BeamCorpus -m:1 --no-restore -v:q -o artifacts/beam-backend/part803-tool` and run as `dotnet artifacts/beam-backend/part803-tool/Erlang.BeamCorpus.dll <OTP root> artifacts/beam-backend/otp-corpus-part803.json`.

The test runner was built with `dotnet build tests/Erlang.Tests -m:1 --no-restore -v:q -o artifacts/beam-backend/part803-tests` and run with the four exact cases listed in the focused report.

The corpus reported 68 Equal, 1 Different, 195 SourceRejected and 1,102 Unsupported across 1,366 release images. cdv_detail_wx was the only status transition, from Unsupported to Different. ELDAPv3 remains Equal. The tool returned 1 because unmatched cases remain. [Corpus report](otp-corpus.json).

All four focused tests passed. Full runtime suite execution and canonical readiness regeneration were outside this run. [Focused report](focused.json).
