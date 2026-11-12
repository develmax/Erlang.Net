# Part767 beam_opcodes release image parity

The C# compiler emits a selection table for distinct atom keys followed by exact integer checks in the successful branches. The existing unary literal dispatch handles integer keys and constant tuple results. Both forms share an error path for an explicit erlang:error/2 call that receives the original arguments in source order.

The Windows OTP 29.1.1 reference is ad05823719d77c8faee87348ea39513d4e2f99c5. The switch code in beam_ssa_codegen.erl emits select_val. The release disassembly establishes the order of entries and blocks and the use of a tail call to erlang:error/2. The implementation reuses the C# term order, literal, operand and line writers. The release test embeds the existing pinned source and image with their upstream provenance. Dependencies and licensing are unchanged.

The entire 19,492-byte image matches. opcode/2 now emits 540 instructions, down from 816. opname/1 emits 584, down from 976. [Oracle script](oracle.eval) and [output](oracle.txt).

Build the corpus runner with `dotnet build tools/Validation/Erlang.BeamCorpus -m:1 --no-restore -v:q -o artifacts/beam-backend/part767-tool`. Run its DLL against the pinned Windows OTP installation and write `artifacts/beam-backend/otp-corpus-part767.json`.

The corpus runner checked all 1,366 release images. It reported 41 Equal, 209 SourceRejected and 1,116 Unsupported. beam_opcodes is the only status change from Part766. Its status changed from Different to Equal. [Corpus report](otp-corpus.json).

The four targeted compiler checks passed, including the release-image regression. [Focused report](focused.json).

The compiler-block run passed 3,722 of 3,827 checks. All 105 failing test names also fail on the saved Part766 harness. The failures include the dynamic-apply value/stack check and port cases returning eacces or no_port. [Compiler block](compiler-block.json) and [baseline reproduction](baseline-failures.json). The full runtime suite was not run and canonical readiness was not regenerated.

The next unsupported module to investigate is asn1ct_table. The OTP image uses get/1 into x1, tests the result against undefined, and allocates a frame only for the success path. The failed assertion reports the original result through badmatch.
