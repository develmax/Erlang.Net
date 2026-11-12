# Part815 Complete HTML Observer release image

The compiler emits the nested record checks, shared URL prefix dispatch, ordered query matching and conversion, and the distinct ordinary-link and logging paths. All 278 instructions of handle_event/2 and every chunk match the pinned Windows OTP 29.1.1 release. The complete 18,184-byte image has the same SHA-256 hash. The permanent regression now compares the whole image. [Instruction and chunk comparison](comparison.txt), [script](compare.eval) and [hash](oracle.txt).

Both images produced identical results, messages and errors in eleven link cases on pinned OTP with temporary GUI stubs. [Behavior script](behavior.eval) and [results](behavior.txt).

The next differing module is dtls_connection_sup. The start_child paths have different instruction counts and the release uses the SSL version macro and kernel include directory. [Initial comparison](next-dtls.txt).

The corpus tool was built with `dotnet build tools/Validation/Erlang.BeamCorpus -m:1 --no-restore -v:q -o artifacts/beam-backend/part815-tool`. The test runner was built with `dotnet build tests/Erlang.Tests -m:1 --no-restore -v:q -o artifacts/beam-backend/part815-tests`.

The 1,366-file corpus reported 70 Equal, 1 Different, 195 SourceRejected and 1,100 Unsupported. cdv_html_wx is the only status change. All four focused tests passed. The full runtime suite and canonical readiness regeneration were outside this run. [Corpus report](otp-corpus.json) and [focused report](focused.json).
