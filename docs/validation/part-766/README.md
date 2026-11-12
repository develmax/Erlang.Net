# Part766 erl_bifs release image parity

The compiler builds grouped boolean dispatch from literal module, function and arity patterns. It preserves the first matching source clause, shares identical arity sets and emits the OTP block order. Singleton arity sets use an exact equality BIF. Larger sets share the true return block.

The Windows OTP 29.1.1 erl_bifs image at ad05823719d77c8faee87348ea39513d4e2f99c5 matches byte for byte. Its size is 6,844 bytes. The three predicates have 39, 23 and 17 instructions. [Oracle script](oracle.eval) and [output](oracle.txt).

The release profile restores bootstrap compiler 10.0.4 and the original include and warning options. The regression uses the original source and BEAM with the upstream source notice retained. A separate deterministic fixture covers grouped dispatch. Dependencies are unchanged.

The remaining emitted image to align is beam_opcodes. Its initial opcode/2 has 816 instructions versus 540 in OTP, and opname/1 has 976 versus 584.

Build the corpus runner with `dotnet build tools/Validation/Erlang.BeamCorpus -m:1 --no-restore -v:q -o artifacts/beam-backend/part766-final-tool`. Run its DLL against the installed Windows OTP root and write `artifacts/beam-backend/otp-corpus-part766.json`.

Built the harness with `dotnet build tests/Erlang.Tests -m:1 --no-restore -v:q -o artifacts/beam-backend/part766-tests`. All four focused checks passed. Full runtime and canonical readiness validation remain pending. [Focused report](focused.json).

The full Windows OTP 29.1.1 run checked 1,366 images. Results were 40 Equal, 1 Different (beam_opcodes), 209 SourceRejected and 1,116 Unsupported. [Corpus report](otp-corpus.json).
