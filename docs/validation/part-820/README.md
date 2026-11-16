# Part820 SSL and TLS supervisor release images

The compiler recognizes either order of the constant flags and computed child list bindings. It uses the existing local call and list emitter, preserving the order of the child specification calls. The SSL release profile now covers five additional modules.

The full ssl_sup, tls_sup, tls_server_sup, tls_dist_sup and tls_dist_server_sup images match the pinned Windows OTP 29.1.1 release. All five use the existing pinned assets in the full-image regression. [Image hashes](oracle.txt).

Both versions of each module return identical flags and ordered child specifications and raise function_clause for invalid arguments on pinned OTP. [Behavior script](behavior.eval) and [results](behavior.txt).

The next SSL targets are ssl_connection_sup and the single-child list in ssl_dist_connection_sup. [Pinned disassembly](next-connection.txt).

The corpus tool was built with `dotnet build tools/Validation/Erlang.BeamCorpus -m:1 --no-restore -v:q -o artifacts/beam-backend/part820-tool`. The test runner was built with `dotnet build tests/Erlang.Tests -m:1 --no-restore -v:q -o artifacts/beam-backend/part820-tests`.

The 1,366-file corpus reported 81 Equal, 1 Different, 195 SourceRejected and 1,089 Unsupported. The five targeted supervisors advanced to Equal. ssl_connection_sup advanced to Different and its standalone comparison differs only in CInf, Dbgi and Docs. All four focused tests passed. The full runtime suite and canonical readiness regeneration were outside this run. [Corpus report](otp-corpus.json) and [focused report](focused.json).
