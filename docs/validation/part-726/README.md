# Part726 Byte-identical SNMP basic PDU image

The C# compiler infers a positive length for a proven nonempty list of encoded octets. The lower bound follows tuple matches and additions into local ASN.1 length and tag helpers. The recursive shift loop retains a zero lower bound for its accumulator input because a shift can produce zero.

The reference is Windows OTP-29.1.1 ad05823719d77c8faee87348ea39513d4e2f99c5. The generated snmp_pdus_basic.beam matches all 10,724 bytes, including the previously different Code, Type and Attr chunks. The permanent release-image check now compiles the vendored Erlang source and compares it with the release image. It reuses the existing release comparer and its decoded documentation fallback on hosts without the pinned compressor. Dependencies and licenses are unchanged. [Direct image comparison](image-oracle.txt).

Built the corpus runner with `dotnet build tools/Validation/Erlang.BeamCorpus -m:1 --no-restore -v:q -o artifacts/beam-backend/part726-tool`. Ran the built DLL with the installed OTP root from artifacts/library376-official-compiler/official-admission.json and output artifacts/beam-backend/otp-corpus-part726.json. The runner checked all 1,366 images and returned 1 because differences remain. Results were 21 Equal, 16 Different, 488 SourceRejected and 841 Unsupported. The sole status change was snmp_pdus_basic from Different to Equal. ELDAPv3 and KEMAlgorithmInformation-2023 remained Equal. [Corpus report](otp-corpus.json).

All three selected compiler checks passed. The full runtime harness and canonical readiness regeneration remain pending. The readiness scope retains the immutable Part561 comparison report. [Focused checks](focused.json).
