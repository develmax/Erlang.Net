# Part748 SSL documentation example client connection

The compiler lowers the supported tagged client call sequence into a two-slot frame, defers tuple projections until needed and trims the frame after preparing the connect arguments. It retains the socket across certificate and data reads, constructs singleton call arguments in registers and emits separate badmatch branches with their source locations. Other sequence shapes use existing lowering.

The reference is Windows OTP-29.1.1 ad05823719d77c8faee87348ea39513d4e2f99c5. All 68 init_connect/1 instructions and 57 mk_opts/1 instructions match after label normalization. The OTP probe used SSL and certificate stubs to compare one successful client scenario and three badmatch failures, including source lines and arguments. It used no network sockets. Existing register and call infrastructure is reused. Dependencies and licenses are unchanged. [Oracle output](image-oracle.txt).

Built the corpus runner with `dotnet build tools/Validation/Erlang.BeamCorpus -m:1 --no-restore -v:q -o artifacts/beam-backend/part748-tool`. Its DLL ran with the installed OTP root from artifacts/library376-official-compiler/official-admission.json and output artifacts/beam-backend/otp-corpus-part748.json.

The remaining instruction difference is start/0, which has 191 instructions in the C# image and 118 in the release. It also changes shared image tables and the subsequent label numbers.

The corpus checked 1,366 files and reported 36 Equal, 1 Different, 488 SourceRejected and 841 Unsupported. The runner exited with status 1 because differences remain. Every module retained its previous status. [Corpus report](otp-corpus.json).

Built the harness with `dotnet build tests/Erlang.Tests -m:1 --no-restore -v:q -o artifacts/beam-backend/part748-tests`. All three selected compiler checks passed. The full runtime harness and canonical readiness regeneration remain pending. The readiness scope retains the immutable Part561 comparison report. [Focused report](focused.json).
