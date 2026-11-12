# Part818 Complete DTLS listener supervisor image

The compiler emits the nested tuple tail argument in register_listener/3, the constant result after ETS setup in init/1, and the protected lookup in lookup_listener/2. The emitted try ends before dispatching on the lookup result. This preserves the distinction between a caught lookup exception and exceptions raised by an of branch.

The complete 3,696-byte image matches the pinned Windows OTP 29.1.1 release. The permanent regression compares the entire file. [Image hash](oracle.txt).

Both images produced the same eight results on real ETS in pinned OTP. Cases include an absent table, active and dead processes, removal of a stale registration, and malformed entries. [Behavior script](behavior.eval) and [results](behavior.txt).

The next DTLS supervisor gap is the list built from child specification calls in dtls_server_sup and dtls_sup. [Pinned disassembly](next-supervisors.txt).

The corpus tool was built with `dotnet build tools/Validation/Erlang.BeamCorpus -m:1 --no-restore -v:q -o artifacts/beam-backend/part818-tool`. The test runner was built with `dotnet build tests/Erlang.Tests -m:1 --no-restore -v:q -o artifacts/beam-backend/part818-tests`.

The 1,366-file corpus reported 74 Equal, 195 SourceRejected and 1,097 Unsupported. Only dtls_listener_sup changed status. All four focused tests passed. The full runtime suite and canonical readiness regeneration were outside this run. [Corpus report](otp-corpus.json) and [focused report](focused.json).
