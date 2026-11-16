# Part738 HTTP acceptor supervisor argument order and atom table

The register-list emitter puts incoming list fields in the high stack slots. Linear sequences reuse the independent move order used by the existing tail-call emitter. The first external call can use an incoming X register directly when argument moves cannot overwrite that register and all arguments are simple operands.

The reference is Windows OTP-29.1.1 ad05823719d77c8faee87348ea39513d4e2f99c5. Argument ordering was compared with setup_args and order_moves in the pinned beam_ssa_codegen source. Existing move and frame slot logic is reused. Dependencies and licenses are unchanged.

The first 26 disassembled instructions in child_spec/1 match, through its first external call. AtU8 now matches. Code and Attr still differ. OTP verified both callback branches and malformed list rejection. The permanent comparison now includes AtU8 and the 253-byte Code prefix through the first external call. [Oracle output](image-oracle.txt).

Built the corpus runner with `dotnet build tools/Validation/Erlang.BeamCorpus -m:1 --no-restore -v:q -o artifacts/beam-backend/part738-final-tool`. Its DLL ran with the installed OTP root from artifacts/library376-official-compiler/official-admission.json and output artifacts/beam-backend/otp-corpus-part738.json.

The next differences are forwarding the first call result into id/3, slot reuse between later calls, branch-local clearing and label order.

The corpus checked 1,366 files and reported 29 Equal, 8 Different, 488 SourceRejected and 841 Unsupported. The runner exited with status 1 because differences remain. Every module retained its previous status. [Corpus report](otp-corpus.json).

Built the harness with `dotnet build tests/Erlang.Tests -m:1 --no-restore -v:q -o artifacts/beam-backend/part738-tests`. All three selected compiler checks passed. The full runtime harness and canonical readiness regeneration remain pending. The readiness scope retains the immutable Part561 comparison report. [Focused report](focused.json).
