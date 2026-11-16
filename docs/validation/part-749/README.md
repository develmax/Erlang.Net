# Part749 external function literals

The C# compiler emits an external function literal for `erlang:make_fun/3` with literal module, function and valid arity. This includes the AST for `fun Module:Function/Arity`. The same rule is used for direct returns and expressions within the general emitter. Dynamic operands and out-of-range arities stay on the call path.

OTP 29.1.1 lowers external fun syntax to `make_fun/3` in `v3_core.erl` and treats it as pure in `erl_bifs.erl`. The C# implementation reuses the ETF export tag writer and literal table interning. Dependencies and licenses are unchanged.

The OTP disassembler confirms that `client_server:start/0` now emits `fun application:stop/1` as a literal. The instruction count is 188, down from 191 in Part748, compared with 118 in the release. Eight chunks still differ. [Oracle script](image-oracle.eval) and [output](image-oracle.txt).

Built the corpus runner with `dotnet build tools/Validation/Erlang.BeamCorpus -m:1 --no-restore -v:q -o artifacts/beam-backend/part749-tool`. Ran its DLL with the Windows OTP root from `artifacts/library376-official-compiler/official-admission.json` and output `artifacts/beam-backend/otp-corpus-part749.json`. All 1,366 modules were checked. Module statuses stayed unchanged from Part748. There were 36 Equal, 1 Different, 488 SourceRejected and 841 Unsupported. [Corpus report](otp-corpus.json).

The regression fixture is built by OTP with `compile:file(Path,[deterministic,{outdir,Dir}])`, matching the C# standalone image's debug policy. The harness compares the entire file and invokes the external BIF reference through the .NET runtime's prepared module registry.

Built the harness with `dotnet build tests/Erlang.Tests -m:1 --no-restore -v:q -o artifacts/beam-backend/part749-tests`. All three selected compiler checks passed. [Focused report](focused.json).

The next difference is the frame and call sequence of `start/0`, including tagged tuple checks and cleanup tail calls. The full runtime suite and canonical readiness regeneration remain pending.
