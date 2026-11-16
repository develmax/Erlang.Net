# Part821 SSL connection supervisor release images

The existing child list writer now handles a singleton computed specification. It emits a zero-slot call frame and retains OTP's nil check, local call and nested tuple layout. The two supervisors use the pinned SSL release profile.

The full ssl_connection_sup and ssl_dist_connection_sup images match the pinned Windows OTP 29.1.1 release and are part of the permanent regression. [Image hashes](oracle.txt).

Both images for each module produce the same flags, child specifications and function_clause for invalid arguments on pinned OTP. [Behavior script](behavior.eval) and [results](behavior.txt).

The next SSL gap is the computed map containing the manager options in ssl_dist_admin_sup. [Pinned disassembly](next-admin.txt).

The corpus tool was built with `dotnet build tools/Validation/Erlang.BeamCorpus -m:1 --no-restore -v:q -o artifacts/beam-backend/part821-tool`. The test runner was built with `dotnet build tests/Erlang.Tests -m:1 --no-restore -v:q -o artifacts/beam-backend/part821-tests`.

The 1,366-file corpus reported 83 Equal, 195 SourceRejected and 1,088 Unsupported. Only the two targeted connection supervisors changed status. All four focused tests passed. The full runtime suite and canonical readiness regeneration were outside this run. [Corpus report](otp-corpus.json) and [focused report](focused.json).
