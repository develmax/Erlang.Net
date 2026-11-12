# Part741 ASN.1 runtime template tuple assertion and debug metadata

The compiler checks a three-element tuple once for a dispatch over two atom keys and integer membership. Equivalent arity tests share blocks. The optimization applies to unguarded, distinct tuple patterns with one common literal result and preserves function_clause for unmatched arguments.

The reference is Windows OTP-29.1.1 ad05823719d77c8faee87348ea39513d4e2f99c5. The grouping was compared with the pinned beam_core_to_ssa and installed disassembly. The release profile records the build options. The debug writer preserves source string segments within binary expressions. The internal debug encoding limit is 512 levels for the nested abstract list forms. Ordinary ETF encoding and decoding retain their existing limit. Dependencies and licenses are unchanged.

The first function matches all 48 disassembled instructions. CInf, Dbgi and Docs match byte for byte. In OTP the generated image accepted all 104 declared assertion keys and rejected five invalid inputs. The permanent partial check covers these three chunks and the assertion instruction bytes. [Oracle output](image-oracle.txt).

The corpus runner was built with `dotnet build tools/Validation/Erlang.BeamCorpus -m:1 --no-restore -v:q -o artifacts/beam-backend/part741-tool`. Its DLL ran with the installed OTP root from artifacts/library376-official-compiler/official-admission.json and output artifacts/beam-backend/otp-corpus-part741.json.

The next step is grouping dependencies/1 and ordering its return blocks and literals. The function currently emits 2,162 instructions compared with 618 in OTP. AtU8, Code, ExpT, LitT and Attr still differ.

The corpus checked 1,366 files and reported 30 Equal, 7 Different, 488 SourceRejected and 841 Unsupported. The runner exited with status 1 because differences remain. Every module retained its previous status. [Corpus report](otp-corpus.json).

Built the harness with `dotnet build tests/Erlang.Tests -m:1 --no-restore -v:q -o artifacts/beam-backend/part741-tests`. All five selected checks passed, including ETF roundtrip and malformed input limits. The full runtime harness and canonical readiness regeneration remain pending. The readiness scope retains the immutable Part561 comparison report. [Focused report](focused.json).
