# Part731 Byte-identical Diameter ETCP supervisor image

The compiler now folds a fresh constant binding before an external tail call in a single clause with an atom parameter pattern. It reuses the existing literal test and call argument move ordering. A failed pattern branches to the function info label while the original argument remains in x0. Other shapes retain the existing lowering.

The reference is Windows OTP-29.1.1 ad05823719d77c8faee87348ea39513d4e2f99c5. The release profile records transport/diameter_etcp_sup.erl for debug forms and the line table. The documentation writer reproduces the source path recorded by OTP using the existing relative-name rule. Dependencies and licenses are unchanged.

The generated image matches all 2,580 bytes and all chunks of the installed release. The OTP VM loaded the generated image, started and stopped the registered supervisor, and raised function_clause for a different atom. The existing vendored source and image now participate in the permanent release comparison. [Oracle output](image-oracle.txt).

The corpus runner was built with `dotnet build tools/Validation/Erlang.BeamCorpus -m:1 --no-restore -v:q -o artifacts/beam-backend/part731-tool`. Its DLL ran with the installed OTP root from artifacts/library376-official-compiler/official-admission.json and output artifacts/beam-backend/otp-corpus-part731.json.

The next candidate is xmerl_text. Its root handler checks the empty-list pattern in the third argument and returns the first argument. The release performs both directly in X registers.

The corpus checked 1,366 files and reported 26 Equal, 11 Different, 488 SourceRejected and 841 Unsupported. The runner exited with status 1 because differences remain. diameter_etcp_sup was the sole status change. ELDAPv3 remained Equal. [Corpus report](otp-corpus.json).

Built the harness with `dotnet build tests/Erlang.Tests -m:1 --no-restore -v:q -o artifacts/beam-backend/part731-tests`. All three selected compiler checks passed. The full runtime harness and canonical readiness regeneration remain pending. The readiness scope retains the immutable Part561 comparison report. [Focused report](focused.json).
