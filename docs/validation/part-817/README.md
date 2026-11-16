# Part817 SSL session cache supervisor images

The C# compiler saves the listener across the SSL options call, constructs the argument list and emits the tail call to supervisor:start_child/2. The debug writer now retains default expressions in typed record fields. The SSL profiles record the pinned bootstrap include paths for logger.hrl and public_key.hrl.

The full dtls_server_session_cache_sup and ssl_server_session_cache_sup images match the pinned Windows OTP 29.1.1 release. The permanent regression uses existing pinned module and header assets. [Image hashes](oracle.txt).

Both images for each module ran with a real OTP supervisor and temporary configuration and child stubs. Successful and failing starts returned the same values and forwarded the same computed arguments. [Behavior script](behavior.eval) and [results](behavior.txt).

The corpus tool was built with `dotnet build tools/Validation/Erlang.BeamCorpus -m:1 --no-restore -v:q -o artifacts/beam-backend/part817-tool`. The test runner was built with `dotnet build tests/Erlang.Tests -m:1 --no-restore -v:q -o artifacts/beam-backend/part817-tests`.

The 1,366-file corpus reported 73 Equal, 195 SourceRejected and 1,098 Unsupported. Both session cache supervisors advanced from Unsupported to Equal. All four focused tests passed. The full runtime suite and canonical readiness regeneration were outside this run. [Corpus report](otp-corpus.json) and [focused report](focused.json).

The next DTLS listener gap is the nested tuple argument to ets:insert/2. [Pinned disassembly](next-listener.txt).
