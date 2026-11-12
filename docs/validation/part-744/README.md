# Part744 Megaco BER and PER encoder wrappers

The compiler lowers paired clauses with a compatibility list prefix and a version selector to register tests and external tail calls. It folds constant local bindings and preserves the original arguments on pattern failure. It also recognizes nested tagged tuple forwarding and common integer-version returns with an optional list guard.

The reference is Windows OTP-29.1.1 ad05823719d77c8faee87348ea39513d4e2f99c5. The compiler reconstructs explicit record fields in debug forms using the parser's record definitions. The release profiles record the binary encoder compile options and original include path. The regression recreates the local include_lib layout with the existing vendored header. Dependencies and licenses are unchanged.

Built the corpus runner with `dotnet build tools/Validation/Erlang.BeamCorpus -m:1 --no-restore -v:q -o artifacts/beam-backend/part744-final-tool`. Its DLL ran with the installed OTP root from artifacts/library376-official-compiler/official-admission.json and output artifacts/beam-backend/otp-corpus-part744.json.

The next candidate is megaco_binary_encoder. Its guarded encode_action_requests/3 produces 130 instructions compared with 69 in the release. The other disassembled functions have matching instruction counts.

The BER and PER images match all 7,004 and 6,100 bytes respectively. OTP verified 342 input cases per module. Both modules now participate in the whole-file release regression. [Oracle output](image-oracle.txt).

The corpus checked 1,366 files and reported 34 Equal, 3 Different, 488 SourceRejected and 841 Unsupported. The runner exited with status 1 because differences remain. The BER and PER modules were the only status changes. ELDAPv3 remained Equal. [Corpus report](otp-corpus.json).

Built the harness with `dotnet build tests/Erlang.Tests -m:1 --no-restore -v:q -o artifacts/beam-backend/part744-tests`. All three selected compiler checks passed. The full runtime harness and canonical readiness regeneration remain pending. The readiness scope retains the immutable Part561 comparison report. [Focused report](focused.json).
