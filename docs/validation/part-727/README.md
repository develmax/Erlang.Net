# Part727 Byte-identical IPv6 distribution image

The Windows OTP-29.1.1 release is the reference, pinned to ad05823719d77c8faee87348ea39513d4e2f99c5. Its is_node_name/1 checks the incoming X register and branches to func_info on guard failure. On success it tail calls inet_tcp_dist:is_node_name/1. The C# compiler now reuses its tail-call lowering for this shape, avoiding a stack frame and a separate failure label. This also aligns later entry labels and the module checksum.

The OTP beam_doc source derives a single-clause signature from the clause arguments independently of its guard. The C# documentation writer now preserves those argument names as well. No external dependencies or license changes were needed.

All 2,896 bytes and every chunk of inet6_tcp_dist.beam match the installed Windows release. The existing vendored source and image now participate in the permanent release-image test. The check uses the existing documentation compression fallback on hosts without the pinned compressor. [Direct image comparison](image-oracle.txt).

The corpus runner was built with `dotnet build tools/Validation/Erlang.BeamCorpus -m:1 --no-restore -v:q -o artifacts/beam-backend/part727-final-tool`. Its DLL was run with the installed OTP root from artifacts/library376-official-compiler/official-admission.json and output artifacts/beam-backend/otp-corpus-part727.json.

The test harness was built with `dotnet build tests/Erlang.Tests -m:1 --no-restore -v:q -o artifacts/beam-backend/part727-tests`. The three compiler cases selected with `--case` all passed. [Focused report](focused.json). The complete runtime harness and canonical readiness regeneration remain pending. The readiness scope retains the immutable Part561 comparison report.

The runner checked all 1,366 images and exited with status 1 because differences remain. It reported 22 Equal, 15 Different, 488 SourceRejected and 841 Unsupported. The sole status transition was inet6_tcp_dist from Different to Equal. ELDAPv3, KEMAlgorithmInformation-2023 and snmp_pdus_basic remain Equal. [Corpus report](otp-corpus.json).
