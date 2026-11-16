# Part747 SSL documentation example option builder and metadata

The compiler emits a three-path option-list constructor using a shared directory, constant suffix lists and one final heap reservation. It preserves the order of the remote calls. The typed atom-or-cons selector is emitted only for an unexported function with known literal callers and no local function reference.

The reference is Windows OTP-29.1.1 ad05823719d77c8faee87348ea39513d4e2f99c5. The SSL example profile restores the release source path and compile options. The debug writer preserves a remote fun reference in its original abstract form. CInf, Dbgi and Type match byte for byte. Both images omit the Docs chunk. Dependencies and licenses are unchanged.

All 57 instructions of mk_opts/1 match after normalizing the 13-label offset introduced by earlier functions. OTP compared the seven client options by intercepting ssl:connect/3 with a test stub. The probe stops before establishing a connection. The full file still differs in eight sections. [Oracle output](image-oracle.txt).

Built the corpus runner with `dotnet build tools/Validation/Erlang.BeamCorpus -m:1 --no-restore -v:q -o artifacts/beam-backend/part747-tool`. Its DLL ran with the installed OTP root from artifacts/library376-official-compiler/official-admission.json and output artifacts/beam-backend/otp-corpus-part747.json.

The remaining differences are in init_connect/1 and start/0 and the tables they affect. The current init_connect/1 has 109 instructions versus 68 in the release. The current start/0 has 191 instructions versus 118.

The corpus checked 1,366 files and reported 36 Equal, 1 Different, 488 SourceRejected and 841 Unsupported. The runner exited with status 1 because differences remain. Every module retained its previous status. [Corpus report](otp-corpus.json).

Built the harness with `dotnet build tests/Erlang.Tests -m:1 --no-restore -v:q -o artifacts/beam-backend/part747-tests`. All three selected compiler checks passed. The full runtime harness and canonical readiness regeneration remain pending. The readiness scope retains the immutable Part561 comparison report. [Focused report](focused.json).
