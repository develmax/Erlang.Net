# Part827 SSL client session cache image

The C# compiler emits the tuple directly in the argument register for the external tail call. It also emits protected calls with a saved argument or a constructed match specification, the exact key comparison in lookup, and the calls that derive the ETS table name and flags.

The full 13,616-byte image matches the Windows OTP 29.1.1 release. The behavior probe ran both images on pinned OTP and checked cache initialization, insert and replace, lookup, select, fold, delete and termination, including exception and deleted-table fallbacks. [Image hash](oracle.txt), [probe](behavior.eval) and [results](behavior.txt).

The corpus tool was built with `dotnet build tools/Validation/Erlang.BeamCorpus -m:1 --no-restore -v:q -o artifacts/beam-backend/part827-tool`. The test runner was built with `dotnet build tests/Erlang.Tests -m:1 --no-restore -v:q -o artifacts/beam-backend/part827-tests`.

The 1,366-file corpus reported 90 Equal, 195 SourceRejected and 1,081 Unsupported. Only ssl_client_session_cache_db changed status. [Corpus report](otp-corpus.json).

The initial focused run found that the isolated release fixture lacked ssl_handshake.hrl. The test fixture now embeds and extracts the pinned header. The repeat passed all four focused tests. The full runtime suite and canonical readiness regeneration were outside this run. [Initial focused report](focused-initial.json) and [repeat report](focused.json).

The next target is the filter closure and nested version conversion in dtls_v1:suites/1. [Pinned disassembly](next-dtls-suites.txt).
