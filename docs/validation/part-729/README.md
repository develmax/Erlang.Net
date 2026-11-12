# Part729 Byte-identical ODBC supervisor image

The compiler matches a singleton list in the incoming X register and retains the original argument for the function_clause failure path. For a sequence of fresh local bindings and pure nested constructors, it folds constant subterms and emits a chain of constructors with one heap reservation. The chain carries one register operand per step. Existing lowering handles other shapes.

The reference is Windows OTP-29.1.1 ad05823719d77c8faee87348ea39513d4e2f99c5. For odbc_sup:init/1 the resulting code uses one 15-word heap reservation and matches every chunk and all 1,968 bytes of the release image. The OTP VM loaded the generated file, returned the expected supervisor configuration for a singleton list and raised function_clause for an atom, an empty list, a two-element list and an improper list. [Oracle output](image-oracle.txt).

The permanent release-image check now compiles the existing vendored source and compares it with the release image. The compiler reuses existing literal normalization and operand writers. Dependencies and licenses are unchanged.

The runner was built with `dotnet build tools/Validation/Erlang.BeamCorpus -m:1 --no-restore -v:q -o artifacts/beam-backend/part729-tool`. Its DLL ran with the installed OTP root from artifacts/library376-official-compiler/official-admission.json and output artifacts/beam-backend/otp-corpus-part729.json.

The next candidate is ssl_srp_primes:check_srp_params/2. The reference groups binary literal patterns differently, with 45 disassembled instructions versus 71 in the current output.

The full corpus checked 1,366 images and reported 24 Equal, 13 Different, 488 SourceRejected and 841 Unsupported. Its exit status was 1 because differences remain. The only status change was odbc_sup from Different to Equal. [Corpus report](otp-corpus.json).

Built the test harness with `dotnet build tests/Erlang.Tests -m:1 --no-restore -v:q -o artifacts/beam-backend/part729-tests`. All three selected compiler cases passed. The full runtime harness and canonical readiness regeneration remain pending. The readiness scope retains the immutable Part561 comparison report. [Focused checks](focused.json).
