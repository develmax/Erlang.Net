# Part866 Dynamic remote calls

The general emitter evaluates dynamic remote call operands in OTP order, module and function before arguments. It places arguments in the initial X registers and then places the module and function in the next two. It emits `apply` in value position and `apply_last` in tail position. The existing direct lowering remains available.

The focused runtime case invokes a dynamic remote function in value and tail positions after tuple matching. The wxDataObject profile reuses the shared wx compile and debug options confirmed from the pinned release.

Build the corpus tool with `dotnet build tools/Validation/Erlang.BeamCorpus -m:1 --no-restore -v:q`. Run the built tool with the pinned installation and report destination. Build the test runner with `dotnet build tests/Erlang.Tests -m:1 --no-restore -v:q`.

All three focused tests passed. OTP returned `{{"probe",probe},"probe"}` for the same dynamic call probe. Its generated Code, ExpT and Attr still differ from the deterministic OTP reference.

The full corpus reported 99 Equal, 12 Different, 164 SourceRejected, and 1,091 Unsupported. The previous 53 dynamic call failures moved to 11 Different and 42 subsequent unsupported checks. wxDataObject moved from Different to Equal. Record construction is now the most frequent expression fallback at 53 files. The full runtime suite and canonical readiness regeneration were outside this run. [Corpus](otp-corpus.json) and [focused tests](focused.json).
