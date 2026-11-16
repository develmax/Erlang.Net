# Part732 Byte-identical xmerl text image

The compiler can check an empty-list parameter and return a distinct variable parameter directly in X registers. The input arguments remain available to func_info on failure. The optimized path requires one empty-list pattern, distinct variable bindings and a variable result.

The reference is Windows OTP-29.1.1 ad05823719d77c8faee87348ea39513d4e2f99c5. The release profile records the compile options and the original xmerl.hrl path. The debug writer reuses the module parser's record definitions when parsing record defaults. It serializes string defaults and empty record constructors as their original abstract forms. Dependencies and licenses are unchanged.

The C#-generated xmerl_text.beam matches all 3,556 bytes and every chunk of the installed release. The generated file loaded in OTP. Its root handler returned the input data with an empty third argument and raised function_clause for a nonempty list or an atom. The permanent release comparison now includes the existing vendored Erlang source, header and image. [Oracle output](image-oracle.txt).

The corpus runner was built with `dotnet build tools/Validation/Erlang.BeamCorpus -m:1 --no-restore -v:q -o artifacts/beam-backend/part732-tool`. Its DLL ran with the installed OTP root from artifacts/library376-official-compiler/official-admission.json and output artifacts/beam-backend/otp-corpus-part732.json.

The next candidate is mnesia_ext_sup:start_proc/5. OTP emits its list comprehension as a separate helper. The C# code emits the comprehension inside the caller.

The corpus checked 1,366 files and reported 27 Equal, 10 Different, 488 SourceRejected and 841 Unsupported. The runner exited with status 1 because differences remain. xmerl_text was the sole status change. ELDAPv3 remained Equal. [Corpus report](otp-corpus.json).

Built the harness with `dotnet build tests/Erlang.Tests -m:1 --no-restore -v:q -o artifacts/beam-backend/part732-tests`. The first concurrent build hit a shared output file lock. The retry succeeded after the corpus runner had finished building. All three selected compiler checks passed. The full runtime harness and canonical readiness regeneration remain pending. The readiness scope retains the immutable Part561 comparison report. [Focused report](focused.json).
