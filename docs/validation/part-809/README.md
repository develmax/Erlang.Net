# Part809 HTML page construction and record update

The compiler emits the HTML window initialization with the OTP stack schedule and builds the result record using declared defaults. For the record update, it checks the local producer's returned record shape and proves the replacement is an atom by inspecting guarded local call sites. These facts permit the typed update_record instruction and omission of redundant tuple tests.

The deterministic 1,144-byte fixture contains all three upstream init functions and preserves the Apache-2.0 notice. It matches OTP 29.1.1 byte for byte. With temporary GUI stubs on pinned OTP, both images returned the same records and made the same calls in order for both delayed and immediate initialization. [Oracle script](oracle.eval), [byte comparison](oracle.txt), [behavior script](behavior.eval) and [behavior results](behavior.txt).

The complete cdv_html_wx module now reaches handle_info/2. That function dispatches on active state and delayed callback presence, then updates the state. [Pinned disassembly](next-handle-info.txt).

The corpus tool was built with `dotnet build tools/Validation/Erlang.BeamCorpus -m:1 --no-restore -v:q -o artifacts/beam-backend/part809-tool` and run as `dotnet artifacts/beam-backend/part809-tool/Erlang.BeamCorpus.dll <OTP root> artifacts/beam-backend/otp-corpus-part809.json`.

The test runner was built with `dotnet build tests/Erlang.Tests -m:1 --no-restore -v:q -o artifacts/beam-backend/part809-tests` and run with the exact cases listed in the focused report.

The full 1,366-file corpus reported 69 Equal, 195 SourceRejected and 1,102 Unsupported. No status changed. ELDAPv3 and cdv_detail_wx remain Equal. The cdv_html_wx diagnostic advanced from init/2 to handle_info/2. [Corpus report](otp-corpus.json).

All four focused tests passed. Full runtime suite execution and canonical readiness regeneration were outside this run. [Focused report](focused.json).
