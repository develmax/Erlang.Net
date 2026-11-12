# Part725 Windows release source paths and build profiles

All eighteen prior Different images had their first mismatch at byte 6 of the container length field. The installed release supplied the compile options and debug option order. The compiler now supports an explicit source filename recorded in abstract forms and the line table, and derives the documentation path from that relative name without normalizing its path segments.

Eight profiles restore release metadata for inet6_tcp_dist, mnesia_ext_sup, odbc_sup, ssl_srp_primes, KEMAlgorithmInformation-2023, diameter_etcp_sup and two SNMP modules. The KEM image matches byte for byte. Its vendored Erlang source and BEAM now participate in the permanent release image regression.

The reference is Windows OTP-29.1.1 ad05823719d77c8faee87348ea39513d4e2f99c5. Existing profile, chunk and test infrastructure was reused. Dependencies and licenses are unchanged.

Ten experimental profiles encountered existing debug form or source handling gaps, so they are excluded from the final configuration. The final comparison retains each existing source admission and generated image status except for the KEM match. [Chunk comparison for the eighteen prior differences](chunk-comparison.txt).

Built the corpus runner with `dotnet build tools/Validation/Erlang.BeamCorpus -m:1 --no-restore -v:q -o artifacts/beam-backend/part725-verified-tool`. Ran its DLL with the installed OTP root from artifacts/library376-official-compiler/official-admission.json and output artifacts/beam-backend/otp-corpus-part725-verified.json. Full runtime validation and canonical readiness regeneration remain pending. The readiness scope retains the immutable Part561 comparison report.

Three focused checks passed. The full corpus checked 1,366 images and reported 20 Equal, 17 Different, 488 SourceRejected and 841 Unsupported. KEMAlgorithmInformation-2023 is the sole status transition. ELDAPv3 remains Equal. [Focused checks](focused.json), [corpus](otp-corpus.json).
