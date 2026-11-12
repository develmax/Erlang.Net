# Part816 Complete DTLS connection supervisor release image

The compiler emits the list and tuple checks in start_child/1 and start_child_dist/1 and forwards the extracted options and original argument list. For start_child/3 it emits the map lookup and default, the supervisor call, the options message and the badmap path. The existing SSL release profile supplies the version definition and include path.

The complete 3,548-byte image matches the pinned Windows OTP 29.1.1 release byte for byte. The permanent regression uses the pinned source and release assets. [Image hash](oracle.txt).

Both images ran against the real supervisor on pinned OTP with a temporary child stub. Default and custom spawn options, the start error, badmap and the malformed wrapper list produced identical outcomes. [Behavior script](behavior.eval) and [results](behavior.txt).

The next SSL emission gap is the same computed argument list in the two session cache supervisor wrappers. [Pinned disassembly](next-session-cache.txt).

The corpus tool was built with `dotnet build tools/Validation/Erlang.BeamCorpus -m:1 --no-restore -v:q -o artifacts/beam-backend/part816-tool`. The test runner was built with `dotnet build tests/Erlang.Tests -m:1 --no-restore -v:q -o artifacts/beam-backend/part816-tests`.

The 1,366-file corpus reported 71 Equal, 195 SourceRejected and 1,100 Unsupported. dtls_connection_sup is the only status change. All four focused tests passed. The full runtime suite and canonical readiness regeneration were outside this run. [Corpus report](otp-corpus.json) and [focused report](focused.json).
