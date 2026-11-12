# Part819 DTLS server and root supervisor images

The compiler recognizes a child list built directly in the result or bound earlier in the body. Reachability analysis uses the same shape to discover the local child specification functions. The emitter preserves source call order and OTP register and frame layout. Both modules use the pinned SSL release profile.

The complete dtls_server_sup and dtls_sup images match the pinned Windows OTP 29.1.1 release byte for byte. Both are included in the permanent full-image regression. [Image hashes](oracle.txt).

Both versions return the same specifications and reject the same invalid arguments. Starting the full supervisor tree on pinned OTP yields the same registered child IDs, module names and hierarchy. [Behavior script](behavior.eval) and [results](behavior.txt).

The corpus tool was built with `dotnet build tools/Validation/Erlang.BeamCorpus -m:1 --no-restore -v:q -o artifacts/beam-backend/part819-tool`. The test runner was built with `dotnet build tests/Erlang.Tests -m:1 --no-restore -v:q -o artifacts/beam-backend/part819-tests`.

The 1,366-file corpus reported 76 Equal, 3 Different, 195 SourceRejected and 1,092 Unsupported. Both DTLS supervisors advanced to Equal. ssl_sup, tls_dist_server_sup and tls_dist_sup now emit images that differ. All four focused tests passed. The full runtime suite and canonical readiness regeneration were outside this run. [Corpus report](otp-corpus.json) and [focused report](focused.json).

The separate ssl_sup probe identifies differences only in CInf, Dbgi and Docs. [Recorded release profile](next-ssl-profile.txt).
