# Part828 DTLS version and cipher suite image

The C# compiler allocates the noncapturing predicate closure in a stack slot, emits the local and remote conversions for the second argument, and emits the remote tail call. The lifted predicate forwards a converted remote result to a local function. The compiler also emits the shared tuple prefix check and integer dispatch for version conversion and the arithmetic and boolean operations applied to remote results.

The full 4,944-byte dtls_v1 image matches Windows OTP 29.1.1. The behavior probe compares all five suite filters for both supported versions, the version conversions, the invalid-version stack, the seeded cookie timeout and the HMAC output. It verifies the cookie secret length for both images. [Image hash](oracle.txt), [probe](behavior.eval) and [results](behavior.txt).

The corpus tool was built with `dotnet build tools/Validation/Erlang.BeamCorpus -m:1 --no-restore -v:q -o artifacts/beam-backend/part828-tool`. The test runner was built with `dotnet build tests/Erlang.Tests -m:1 --no-restore -v:q -o artifacts/beam-backend/part828-tests`.

The 1,366-file corpus reported 91 Equal, 195 SourceRejected and 1,080 Unsupported. Only dtls_v1 changed status. All four focused tests passed. The full runtime suite and canonical readiness regeneration were outside this run. [Corpus report](otp-corpus.json) and [focused report](focused.json).

The next target is dtls_socket:send/3. Its nested tuple parameter supplies arguments for a dynamic remote tail call. [Pinned disassembly](next-dtls-send.txt).
