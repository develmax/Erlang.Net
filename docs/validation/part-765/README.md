# Part765 erl_reply release image parity

The C# compiler emits the checked argument list, socket call sequence, trimmed frame and badmatch exits used by reply/1. The IP helper keeps the original list in x0 for its failure path and reuses Y registers across the four conversions. Both paths are matched by AST structure and argument bindings.

The pinned Windows OTP 29.1.1 compiler image at ad05823719d77c8faee87348ea39513d4e2f99c5 matches byte for byte. The generated file is 2,504 bytes. The disassembler confirms 57 reply/1 instructions and 37 ip_string_to_tuple/1 instructions. The SHA-256 is C47D6FA92F6333C248BEE2B39BA71E5E9D454F24B4ED1B9E2C7C947D305FF075. [Oracle script](oracle.eval) and [output](oracle.txt).

The release profile restores bootstrap compiler 10.0.4 and the pinned kernel options. The regression uses the original release source and BEAM with the Apache-2.0 source notice retained. No network sockets were opened by this check. Dependencies are unchanged.

Built the corpus runner with `dotnet build tools/Validation/Erlang.BeamCorpus -m:1 --no-restore -v:q -o artifacts/beam-backend/part765-tool`. Ran its DLL with the installed Windows OTP root from `artifacts/library376-official-compiler/official-admission.json` and output `artifacts/beam-backend/otp-corpus-part765.json`.

All 1,366 release files were checked. The report has 39 Equal, 2 Different, 209 SourceRejected and 1,116 Unsupported. The only status change was erl_reply from Different to Equal. [Corpus report](otp-corpus.json).

Built the test harness with `dotnet build tests/Erlang.Tests -m:1 --no-restore -v:q -o artifacts/beam-backend/part765-tests`. All four focused checks passed, including the new release fixture. The full runtime suite and canonical readiness regeneration remain pending. [Focused report](focused.json).

The next generated image is erl_bifs. Its initial is_pure/3 has 879 instructions versus 39 in OTP, and is_safe/3 has 312 versus 23. The smallest dispatch, is_exit_bif/3, merges exit and throw into one arity comparison. The remaining change requires dispatch grouping and the release profile.
