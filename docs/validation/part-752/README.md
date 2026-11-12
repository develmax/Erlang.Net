# Part752 dynamic external fun references

The parser accepts variable module, function and arity operands in external fun references. It lowers them to the existing `erlang:make_fun/3` BIF with the fun expression's source location. The BEAM emitter uses the existing tail-call and argument-move infrastructure. It preserves the OTP order for the independent constant move ahead of the forwarding chain. Static external fun references still use literal emission.

The release debug writer retains the `fun` form with variable or literal operands. The pinned reference is Windows OTP 29.1.1 ad05823719d77c8faee87348ea39513d4e2f99c5. The implementation follows the external fun grammar in `erl_parse.yrl` and lowering in `v3_core.erl`. Existing C# call and BIF implementations are reused. Dependencies and licenses are unchanged.

The OTP fixture covers all-variable operands, a variable module and a variable arity. OTP builds it with `compile:file(Path,[deterministic,{outdir,Dir}])`. The standalone image matches byte for byte. The oracle verifies the abstract form, successful invocation and invalid-arity `badarg`. The release-profile image's three function forms also match the OTP abstract forms exactly. [Oracle script](oracle.eval), [output](oracle.txt) and [debug comparison](debug-oracle.txt).

Build the corpus runner with `dotnet build tools/Validation/Erlang.BeamCorpus -m:1 --no-restore -v:q -o artifacts/beam-backend/part752-final-tool`. Run its DLL with the installed Windows OTP root from `artifacts/library376-official-compiler/official-admission.json` and output `artifacts/beam-backend/otp-corpus-part752-final.json`.

The final corpus checked 1,366 files and reported 37 Equal, 277 SourceRejected and 1,052 Unsupported. The only changed statuses were dbg_iload, inet_tcp_dist and fprof, which advanced from SourceRejected to Unsupported. [Corpus report](otp-corpus.json).

Built the harness with `dotnet build tests/Erlang.Tests -m:1 --no-restore -v:q -o artifacts/beam-backend/part752-tests`. All three selected compiler cases passed, including invocation and invalid-arity behavior in the .NET BEAM runtime. The full runtime suite and canonical readiness regeneration remain pending. [Focused report](focused.json).

Named recursive funs require a separate closure-binding change so that the self binding is local to the fun and its exported environment remains acyclic.
