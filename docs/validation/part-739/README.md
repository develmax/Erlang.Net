# Part739 HTTP acceptor supervisor linear call forwarding

The linear binding emitter can pass the previous result in X0 to the third argument of a three-argument local call while saving it for later reads. It stages the first argument in X2 and swaps the registers before the call. For the supported independent external-call moves, it saves the previous result after the first two loads and before overwriting X0. Other argument shapes keep existing lowering.

The reference is Windows OTP-29.1.1 ad05823719d77c8faee87348ea39513d4e2f99c5. The instruction sequences were compared with the installed disassembly and pinned beam_ssa_codegen call setup. Existing linear binding allocation and call emission are reused. Dependencies and licenses are unchanged.

The first 46 child_spec/1 instructions match through the socket type lookup. The permanent comparison now checks the 308-byte Code prefix ending after that call. Code and Attr remain different. Both callback branches and malformed list rejection were verified in OTP. [Oracle output](image-oracle.txt).

The corpus runner was built with `dotnet build tools/Validation/Erlang.BeamCorpus -m:1 --no-restore -v:q -o artifacts/beam-backend/part739-final-tool`. Its DLL ran with the installed OTP root from artifacts/library376-official-compiler/official-admission.json and output artifacts/beam-backend/otp-corpus-part739.json.

The next difference is the final config-list read before the IP family lookup. OTP reads the list into X1 before replacing its Y slot with the previous result.

The corpus checked 1,366 files and reported 29 Equal, 8 Different, 488 SourceRejected and 841 Unsupported. The runner exited with status 1 because differences remain. Every module retained its previous status. [Corpus report](otp-corpus.json).

Built the harness with `dotnet build tests/Erlang.Tests -m:1 --no-restore -v:q -o artifacts/beam-backend/part739-tests`. All three selected compiler checks passed. The full runtime harness and canonical readiness regeneration remain pending. The readiness scope retains the immutable Part561 comparison report. [Focused report](focused.json).
