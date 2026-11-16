# Part728 Byte-identical SNMP manager network filter image

The C# compiler now emits register type tests and constant returns without a stack frame for clauses that require no argument destructuring. It omits leading literal expressions that have no effect and jumps to the function info label if the final guard fails. This aligns the instructions, tables and checksum with the Windows OTP-29.1.1 reference.

The pinned reference is ad05823719d77c8faee87348ea39513d4e2f99c5. The OTP epp records the path returned by the include search. The release profile now records the original snmp_debug.hrl path, including its ../misc segment, in the debug forms independently of the location used to read the source. Dependencies and licenses are unchanged.

The generated snmpm_net_if_filter.beam matches all 2,256 bytes and every chunk of the installed release. The permanent release-image regression uses the existing vendored source, header and BEAM. It reuses the existing documentation fallback on hosts without the pinned compressor. [Direct image comparison](image-oracle.txt).

The corpus runner was built with `dotnet build tools/Validation/Erlang.BeamCorpus -m:1 --no-restore -v:q -o artifacts/beam-backend/part728-final-tool` and its DLL was run against the installed root from artifacts/library376-official-compiler/official-admission.json with output artifacts/beam-backend/otp-corpus-part728.json.

The test harness was built with `dotnet build tests/Erlang.Tests -m:1 --no-restore -v:q -o artifacts/beam-backend/part728-tests`. All three selected compiler cases passed. The full runtime harness and canonical readiness regeneration remain pending. The readiness scope retains the immutable Part561 comparison report. [Focused checks](focused.json).

The next emitted-image mismatch is odbc_sup:init/1. The OTP disassembly matches the singleton list in X registers, folds the fully constant strategy tuple, and reserves 15 heap words once for the result. The C# disassembly uses a 15-slot stack frame and four separate heap tests.

The runner checked 1,366 images and exited with status 1 because differences remain. It reported 23 Equal, 14 Different, 488 SourceRejected and 841 Unsupported. The sole status transition was snmpm_net_if_filter from Different to Equal. [Corpus report](otp-corpus.json).
