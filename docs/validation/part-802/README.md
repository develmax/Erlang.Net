# Part802 Tagged arguments in error tail calls

The C# compiler now builds an atom-tagged tuple from incoming argument registers and emits the tail call to erlang:error/1. It reuses the tuple writer used by literal-dispatch fallbacks and resolves unqualified calls through the existing import rules. The pinned OTP 29.1.1 disassembly supplied the heap and register layout.

The 720-byte fixture includes the upstream handle_call/3 and handle_cast/2 bodies with their Apache-2.0 notice. A third function uses reordered and repeated parameters with an unused parameter between them. The complete deterministic fixture matches OTP byte for byte. [Oracle script](oracle.eval) and [byte comparison](oracle.txt).

The complete cdv_detail_wx module now reaches create_menus/1. OTP folds the menu list and the create_menu record into one literal, then passes it alongside the original menu bar and new_window.

The corpus tool was built with `dotnet build tools/Validation/Erlang.BeamCorpus -m:1 --no-restore -v:q -o artifacts/beam-backend/part802-tool` and run as `dotnet artifacts/beam-backend/part802-tool/Erlang.BeamCorpus.dll <OTP root> artifacts/beam-backend/otp-corpus-part802.json`.

The test runner was built with `dotnet build tests/Erlang.Tests -m:1 --no-restore -v:q -o artifacts/beam-backend/part802-tests` and run with the four exact cases listed in the focused report.

The corpus reported 68 Equal, 195 SourceRejected and 1,103 Unsupported across 1,366 release images. ELDAPv3 remains Equal. The tool returned 1 because unmatched cases remain. [Corpus report](otp-corpus.json).

All four focused tests passed. The full runtime suite and canonical readiness regeneration were outside this run. [Focused report](focused.json).
