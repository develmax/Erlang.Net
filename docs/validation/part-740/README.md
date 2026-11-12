# Part740 Byte-identical HTTP acceptor supervisor image

The linear binding emitter can transfer the last-read argument to its X register before reusing the Y slot for the previous result. The branch emitter orders the alternative and join labels, clears dead Y slots before the branch-local call and returns the final tuple directly using its value in X0. Branch cleanup applies only to the direct branch result and retains values needed by either the remaining constructor or the continuation.

The reference is Windows OTP-29.1.1 ad05823719d77c8faee87348ea39513d4e2f99c5. The generated httpd_acceptor_sup.beam matches all 3,992 bytes, all chunks and all disassembled functions of the installed release. OTP also verified both callback branches and malformed list rejection. The existing vendored source, include and image now participate in the whole-file release regression. Existing register, branch, constructor and comparison infrastructure is reused. Dependencies and licenses are unchanged. [Oracle output](image-oracle.txt).

The corpus runner was built with `dotnet build tools/Validation/Erlang.BeamCorpus -m:1 --no-restore -v:q -o artifacts/beam-backend/part740-verified-tool`. Its DLL ran with the installed OTP root from artifacts/library376-official-compiler/official-admission.json and output artifacts/beam-backend/otp-corpus-part740.json.

The corpus checked 1,366 files and reported 30 Equal, 7 Different, 488 SourceRejected and 841 Unsupported. The runner exited with status 1 because differences remain. httpd_acceptor_sup was the only status change. ELDAPv3 remained Equal. [Corpus report](otp-corpus.json).

Built the harness with `dotnet build tests/Erlang.Tests -m:1 --no-restore -v:q -o artifacts/beam-backend/part740-tests`. All three selected compiler checks passed. The full runtime harness and canonical readiness regeneration remain pending. The readiness scope retains the immutable Part561 comparison report. [Focused report](focused.json).
