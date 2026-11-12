# Part764 SSH no-I/O release image parity

The macro preprocessor now carries the last substituted argument token's annotation into following replacement tokens. This follows epp.erl expand_arg and expand_macro in pinned OTP. The SSH disconnect wrappers use that annotation for LINE and for debug operands. They pass lines 38, 45, 51 and 57 while keeping the call instructions at lines 37, 44, 50 and 56.

The release profile records the bootstrap compiler version 10.0.4, the kernel include path, the SSH source path and the pinned warning options. The Windows OTP 29.1.1 image at ad05823719d77c8faee87348ea39513d4e2f99c5 matches byte for byte. Its length is 2,940 bytes and its SHA-256 is 21545723D99CBE52130DDDC2660BAEBAC4952E86443C88BD074BDAD269ACBEE7. [Oracle script](oracle.eval) and [output](oracle.txt).

The regression uses the original release source and header with their Apache-2.0 notices and the release BEAM as the expected image. Dependencies are unchanged.

The next generated image is erl_reply. In the initial comparison its reply/1 has 79 instructions versus 57 in OTP, and ip_string_to_tuple/1 has 51 versus 37.

Built the corpus runner with `dotnet build tools/Validation/Erlang.BeamCorpus -m:1 --no-restore -v:q -o artifacts/beam-backend/part764-tool`. Ran its DLL with the installed Windows OTP root from `artifacts/library376-official-compiler/official-admission.json` and output `artifacts/beam-backend/otp-corpus-part764.json`.

All 1,366 release files were checked. The report has 38 Equal, 3 Different, 209 SourceRejected and 1,116 Unsupported. The only status change was ssh_no_io from Different to Equal. [Corpus report](otp-corpus.json).

Built the harness with `dotnet build tests/Erlang.Tests -m:1 --no-restore -v:q -o artifacts/beam-backend/part764-tests`. All four focused checks passed, including the new release image fixture. The full runtime suite and canonical readiness regeneration remain pending. [Focused report](focused.json).
