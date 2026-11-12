# Part745 Byte-identical Megaco binary encoder wrapper

The compiler now emits a payload is_list check after the version test when all paired list-tail clauses require the same payload guard. A failed guard can jump directly to func_info because every remaining pair requires the same check. Calls resolved to a local is_list function, different guard shapes and mixed guarded and unguarded pairs retain their existing lowering.

The reference is Windows OTP-29.1.1 ad05823719d77c8faee87348ea39513d4e2f99c5. The binary wrapper uses the same build profile and include as the BER and PER wrappers. Existing register matching, tail calls, metadata and release comparison infrastructure is reused. Dependencies and licenses are unchanged.

The generated megaco_binary_encoder.beam matches all 9,408 bytes and all disassembled functions of the installed release. encode_action_requests/3 matches all 69 instructions. OTP verified 570 combinations of input terms and compared values and errors with the installed image. The existing vendored source and image now participate in the whole-file release regression. [Oracle output](image-oracle.txt).

Built the corpus runner with `dotnet build tools/Validation/Erlang.BeamCorpus -m:1 --no-restore -v:q -o artifacts/beam-backend/part745-tool`. Its DLL ran with the installed OTP root from artifacts/library376-official-compiler/official-admission.json and output artifacts/beam-backend/otp-corpus-part745.json.

The next candidate is cdv_gen_cb. Its get_info/0 sequence matches a tagged call result and retains two values across subsequent calls. The release uses a two-slot frame and a separate badmatch branch.

The corpus checked 1,366 files and reported 35 Equal, 2 Different, 488 SourceRejected and 841 Unsupported. The runner exited with status 1 because differences remain. megaco_binary_encoder was the only status change. ELDAPv3 remained Equal. [Corpus report](otp-corpus.json).

Built the harness with `dotnet build tests/Erlang.Tests -m:1 --no-restore -v:q -o artifacts/beam-backend/part745-tests`. All three selected compiler checks passed. The full runtime harness and canonical readiness regeneration remain pending. The readiness scope retains the immutable Part561 comparison report. [Focused report](focused.json).
