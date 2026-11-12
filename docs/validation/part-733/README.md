# Part733 Byte-identical Mnesia external supervisor image

The compiler emits a separate recursive helper for the captured-variable list comprehension in the supported tuple-construction and call shape. It matches only a fixed proper source list of pairs, so the generated helper can extract the tuple fields without runtime tuple checks. The caller builds the source list, extracts the returned values, constructs the child tuple and preserves the external call and retry order. Other comprehension shapes retain their existing lowering.

The reference is Windows OTP-29.1.1 ad05823719d77c8faee87348ea39513d4e2f99c5. The implementation follows the installed disassembly and the pinned v3_core list comprehension lowering. Existing local helper naming, literal and type writers and call emission are reused. Dependencies and licenses are unchanged.

The generated mnesia_ext_sup.beam matches all 3,044 bytes and all sections of the installed release. The generated image loaded in OTP, applied an explicit shutdown override, retained the default child settings and restarted a stopped child. The permanent release comparison now includes the existing vendored source and image. [Oracle output](image-oracle.txt).

The corpus runner was built with `dotnet build tools/Validation/Erlang.BeamCorpus -m:1 --no-restore -v:q -o artifacts/beam-backend/part733-final-tool`. Its DLL ran with the installed OTP root from artifacts/library376-official-compiler/official-admission.json and output artifacts/beam-backend/otp-corpus-part733.json.

The next candidate is httpd_connection_sup:init/1. The pinned image has 23 disassembled instructions for that function and the current emitter has 66. Its compile options and source path were inspected for the next comparison.

The corpus checked 1,366 files and reported 28 Equal, 9 Different, 488 SourceRejected and 841 Unsupported. The runner exited with status 1 because differences remain. mnesia_ext_sup was the only status change. ELDAPv3 remained Equal. [Corpus report](otp-corpus.json).

Built the harness with `dotnet build tests/Erlang.Tests -m:1 --no-restore -v:q -o artifacts/beam-backend/part733-tests`. All three selected compiler checks passed. The full runtime harness and canonical readiness regeneration remain pending. The readiness scope retains the immutable Part561 comparison report. [Focused report](focused.json).
