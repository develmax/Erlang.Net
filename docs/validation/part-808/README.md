# Part808 Guarded HTML page arguments

The C# compiler lowers the two-element list dispatch in cdv_html_wx:init/1. It emits the atom and function guards, branches on the tuple arity and tag, and schedules the local tail call arguments as OTP does. The zero-arity fun call retains the guarded fun type and reconstructs the recursive argument in OTP order.

The deterministic 840-byte fixture retains the Apache-2.0 notice and provides local stubs for init/2 and init/4. It matches OTP 29.1.1 byte for byte. Both images produce the same outcomes on pinned OTP for all six successful paths, an invalid list and a function with the wrong arity. [Oracle script](oracle.eval), [byte comparison](oracle.txt), [behavior script](behavior.eval) and [results](behavior.txt).

The complete cdv_html_wx module now reaches init/2. Its OTP image uses update_record with a typed tuple result and an atom-typed replacement value. [Disassembly](next-init.txt).

The corpus tool was built with `dotnet build tools/Validation/Erlang.BeamCorpus -m:1 --no-restore -v:q -o artifacts/beam-backend/part808-tool` and run as `dotnet artifacts/beam-backend/part808-tool/Erlang.BeamCorpus.dll <OTP root> artifacts/beam-backend/otp-corpus-part808.json`.

The test runner was built with `dotnet build tests/Erlang.Tests -m:1 --no-restore -v:q -o artifacts/beam-backend/part808-tests` and run with the exact cases listed in the focused report.

The full 1,366-file corpus reported 69 Equal, 195 SourceRejected and 1,102 Unsupported. No status changed. ELDAPv3 and cdv_detail_wx remain Equal. The cdv_html_wx diagnostic advanced to init/2. [Corpus report](otp-corpus.json).

All four focused tests passed. Full runtime suite execution and canonical readiness regeneration were outside this run. [Focused report](focused.json).
