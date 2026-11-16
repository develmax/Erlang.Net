# Part743 Byte-identical Megaco configuration helper image

The compiler emits register-only predicates for boolean patterns and guarded integer comparisons. Literal-prefix clauses can tail call a local function without a frame. A supported short-circuit sequence calls two local boolean predicates and compares the original arguments. It retains the order of the calls and skips clauses only when their literal test is already known to fail. Other shapes retain existing lowering.

The Windows OTP-29.1.1 reference is ad05823719d77c8faee87348ea39513d4e2f99c5. The release profile records the compile definition, working directory, include path, warning order and app_vsn. The debug writer distinguishes explicit andalso expressions from comma-separated guards and handles parenthesized left operands. Existing call, comparison and typed operand writers are reused. Dependencies and licenses are unchanged.

The generated megaco_config_misc.beam matches all 3,444 bytes and all disassembled functions of the installed release. OTP produced identical results for 2,450 input combinations across eleven exported verification functions. The existing vendored source and BEAM now participate in the whole-file regression. [Oracle output](image-oracle.txt).

Built the corpus runner with `dotnet build tools/Validation/Erlang.BeamCorpus -m:1 --no-restore -v:q -o artifacts/beam-backend/part743-final-tool`. Its DLL ran with the installed OTP root from artifacts/library376-official-compiler/official-admission.json and output artifacts/beam-backend/otp-corpus-part743.json.

The next candidate is megaco_ber_encoder. Its release options were inspected and its largest instruction differences are in the versioned encode and decode functions.

The corpus checked 1,366 files and reported 32 Equal, 5 Different, 488 SourceRejected and 841 Unsupported. The runner exited with status 1 because differences remain. megaco_config_misc was the only status change. ELDAPv3 remained Equal. [Corpus report](otp-corpus.json).

Built the harness with `dotnet build tests/Erlang.Tests -m:1 --no-restore -v:q -o artifacts/beam-backend/part743-tests`. All three selected compiler checks passed. The full runtime harness and canonical readiness regeneration remain pending. The readiness scope retains the immutable Part561 comparison report. [Focused report](focused.json).
