# Part822 SSL distribution admin supervisor image

The computed map emitter accepts zero-argument static remote calls. The emitter places the result of ssl_admin_sup:manager_opts/0 into the argument list of the manager child specification. Local child-list emission retains its existing restrictions and the module uses the pinned SSL release profile.

The complete 2,960-byte ssl_dist_admin_sup image matches Windows OTP 29.1.1 byte for byte. It is part of the permanent release regression. [Image hash](oracle.txt).

Both images return identical supervisor specifications with default and explicitly configured session options and raise the same invalid argument error on pinned OTP. [Behavior script](behavior.eval) and [results](behavior.txt).

The next SSL gaps include the computed names passed through the listener tracker and session ticket supervisor wrappers. [Pinned disassembly](next-supervisors.txt).

The corpus tool was built with `dotnet build tools/Validation/Erlang.BeamCorpus -m:1 --no-restore -v:q -o artifacts/beam-backend/part822-tool`. The test runner was built with `dotnet build tests/Erlang.Tests -m:1 --no-restore -v:q -o artifacts/beam-backend/part822-tests`.

The 1,366-file corpus reported 84 Equal, 195 SourceRejected and 1,087 Unsupported. Only ssl_dist_admin_sup changed status. All four focused tests passed. The full runtime suite and canonical readiness regeneration were outside this run. [Corpus report](otp-corpus.json) and [focused report](focused.json).
