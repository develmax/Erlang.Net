# Part746 Byte-identical crash dump general callback image

The compiler emits a two-slot frame for the supported tagged-result projection sequence. It retains the first result until the later field extraction, reuses its slot for the local call result and preserves the unmatched value for the badmatch branch. The debug writer reconstructs record_info calls from source tokens while code emission uses the folded constant. The Observer release profile records the original include search paths.

The reference is Windows OTP-29.1.1 ad05823719d77c8faee87348ea39513d4e2f99c5. The generated cdv_gen_cb.beam matches all 4,580 bytes and all disassembled instructions. OTP compared one success and two badmatch cases with a crashdump_viewer test stub. This checks callback control flow with deterministic data. The existing vendored source, header and image now participate in the full image regression. Dependencies and licenses are unchanged. [Oracle output](image-oracle.txt).

The corpus runner was built with `dotnet build tools/Validation/Erlang.BeamCorpus -m:1 --no-restore -v:q -o artifacts/beam-backend/part746-final-tool`. Its DLL ran with the installed OTP root from artifacts/library376-official-compiler/official-admission.json and output artifacts/beam-backend/otp-corpus-part746-final.json.

The next candidate is the SSL documentation example client_server. Its start and connect sequences, option builder and release metadata still differ.

The corpus checked 1,366 files and reported 36 Equal, 1 Different, 488 SourceRejected and 841 Unsupported. The runner exited with status 1 because differences remain. cdv_gen_cb was the only status change. ELDAPv3 remained Equal. [Corpus report](otp-corpus.json).

Built the harness with `dotnet build tests/Erlang.Tests -m:1 --no-restore -v:q -o artifacts/beam-backend/part746-tests`. All three selected compiler checks passed. The full runtime harness and canonical readiness regeneration remain pending. The readiness scope retains the immutable Part561 comparison report. [Focused report](focused.json).
