# Part812 Complete HTML Observer image emission

The C# compiler now emits the send expression in handle_event/2 and checks record tags before access or update in expand/3. It also emits the two-item list passed by start_link/2 and the logging return in handle_call/3. The update evaluates replacement expressions before the record expression, matching OTP record expansion order.

The complete cdv_html_wx image is 5,336 bytes, compared with 18,184 bytes in the release. The generated and release images both load on pinned OTP. Nine link-handling scenarios produced identical values, errors and messages with temporary GUI stubs. They cover all three URL prefixes, fallback, a registered observer, new and existing windows, and malformed input. [Behavior script](behavior.eval) and [results](behavior.txt).

The full image differs in twelve chunk payloads. The instruction counts differ in handle_event/2, expand/3 and cdv_wx_set_status/2. The release build profile is still outstanding. [Chunk and instruction comparison](comparison.txt).

The corpus tool was built with `dotnet build tools/Validation/Erlang.BeamCorpus -m:1 --no-restore -v:q -o artifacts/beam-backend/part812-tool`. The test runner was built with `dotnet build tests/Erlang.Tests -m:1 --no-restore -v:q -o artifacts/beam-backend/part812-tests`.

The full 1,366-file corpus reported 69 Equal, 2 Different, 195 SourceRejected and 1,100 Unsupported. cdv_html_wx and dtls_connection_sup advanced from Unsupported to Different. ELDAPv3 and cdv_detail_wx remain Equal. Behavior of the newly emitted DTLS module was outside this comparison. [Corpus report](otp-corpus.json).

All four focused tests passed. Full runtime suite execution and canonical readiness regeneration were outside this run. [Focused report](focused.json).
