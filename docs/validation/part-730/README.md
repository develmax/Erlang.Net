# Part730 Byte-identical SRP parameter image

The compiler emits a grouped binary literal dispatch for two arguments when the unguarded literal clauses share a result, the first pattern is a byte, and the second patterns admit the supported two-byte prefix tree. The fallback must be an unconditional literal return. The compiler keeps the existing general path for other shapes.

The reference is Windows OTP-29.1.1 ad05823719d77c8faee87348ea39513d4e2f99c5. The pinned beam_core_to_ssa groups byte segments for two-byte dispatch and beam_dict interns string fragments by searching the existing table. The C# emitter uses typed select tables, matches shared binary prefixes using StrT and compares a singleton secondary pattern as a literal. Existing BEAM opcodes, type and literal writers are reused. Dependencies and licenses are unchanged.

The generated ssl_srp_primes.beam matches all 24,140 bytes and all sections of the installed release. The generated image loaded in OTP and accepted all five standard SRP parameter pairs. It rejected 30 truncated, modified, extended or wrong-type inputs. The existing vendored Erlang source and image now participate in the permanent release comparison. [OTP oracle](image-oracle.txt).

Built the corpus runner with `dotnet build tools/Validation/Erlang.BeamCorpus -m:1 --no-restore -v:q -o artifacts/beam-backend/part730-tool` and ran its DLL with the installed OTP root from artifacts/library376-official-compiler/official-admission.json and output artifacts/beam-backend/otp-corpus-part730.json.

Built the harness with `dotnet build tests/Erlang.Tests -m:1 --no-restore -v:q -o artifacts/beam-backend/part730-tests`. All three selected compiler checks passed. The full runtime harness and canonical readiness regeneration remain pending. The readiness scope retains the immutable Part561 comparison report. [Focused report](focused.json).

The next emitted-image candidate is diameter_etcp_sup:start_link/1. The release tests the atom directly in x0 and tail calls supervisor:start_link/3. The current compiler uses a stack frame and an ordinary call. The release also records transport/diameter_etcp_sup.erl in the line table.

The corpus checked 1,366 files and reported 25 Equal, 12 Different, 488 SourceRejected and 841 Unsupported. The runner exited with status 1 because differences remain. ssl_srp_primes was the sole status change. ELDAPv3 remained Equal. [Corpus report](otp-corpus.json).
