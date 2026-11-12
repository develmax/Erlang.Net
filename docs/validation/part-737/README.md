# Part737 HTTP acceptor supervisor stack lifetime and constructor lowering

The register-list path now recognizes a sequence of fresh variable bindings whose expressions do not introduce nested bindings. It substitutes pure constants, preserves call evaluation order and reuses slots after their final read. For a tuple containing a list of bound values and one call result, it reserves heap space once and keeps the call result and list tail in X registers. Other shapes retain existing lowering.

The reference is Windows OTP-29.1.1 ad05823719d77c8faee87348ea39513d4e2f99c5. Existing expression traversal, constant normalization and BEAM constructors are reused. Dependencies and licenses are unchanged.

The generated child_spec/1 now has nine stack slots, matching OTP, and 93 disassembled instructions compared with 94 in OTP. Both callback branches and malformed lists were verified by loading the generated image in OTP. AtU8, Code and Attr still differ. The atom ordering difference is visible in the option lookups: socket_type precedes ip_comm in the generated table, while the release orders the default first. [Oracle output](image-oracle.txt).

Built the corpus runner with `dotnet build tools/Validation/Erlang.BeamCorpus -m:1 --no-restore -v:q -o artifacts/beam-backend/part737-final-tool`. Its DLL ran with the installed OTP root from artifacts/library376-official-compiler/official-admission.json and output artifacts/beam-backend/otp-corpus-part737.json.

The corpus checked 1,366 files and reported 29 Equal, 8 Different, 488 SourceRejected and 841 Unsupported. The runner exited with status 1 because differences remain. Every module retained its previous status. [Corpus report](otp-corpus.json).

Built the harness with `dotnet build tests/Erlang.Tests -m:1 --no-restore -v:q -o artifacts/beam-backend/part737-tests`. The permanent Code prefix comparison now includes the frame allocation. All three selected checks passed. The full runtime harness and canonical readiness regeneration remain pending. The readiness scope retains the immutable Part561 comparison report. [Focused report](focused.json).
