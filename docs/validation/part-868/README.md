# Part868 General anonymous closures

The general emitter builds anonymous closures with `make_fun3` and lifts their bodies after module functions. It uses the existing free-variable analysis, appends captures after call arguments in the lifted body, and uses the parsed Core ordinal and original lexical function for generated names. Nested closure bodies are emitted in additional batches. Named recursive functions continue through the existing specialized paths where supported.

The focused runtime probe constructs a multi-clause closure from a matched tuple, invokes it, and invokes a nested closure that captures the same value. The pinned OTP compiler and runtime returned `{17,{second,17}}` for the same source and arguments.

Build the corpus tool with `dotnet build tools/Validation/Erlang.BeamCorpus -m:1 --no-restore -v:q`. Run the built tool with the pinned installation and report destination. Build the test runner with `dotnet build tests/Erlang.Tests -m:1 --no-restore -v:q`.

All three focused tests passed. The closure fixture matches OTP lifted names and the atom chunk, while Code, ExpT, FunT, LocT, Attr and Line still differ. The full corpus reported 99 Equal, 15 Different, 164 SourceRejected and 1,088 Unsupported. All 66 previous fun-expression failures reached subsequent unsupported checks. List concatenation is now the most frequent specific expression failure at 58 images. The full runtime suite and canonical readiness regeneration were outside this run. [Corpus](otp-corpus.json) and [focused checks](focused.json).
