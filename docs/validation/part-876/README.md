# Part876 Local function references

The emitter uses the existing zero-capture FunT entry for local function references in general expressions. Direct returns and tuples preserve OTP's register layout. Repeated references to the same entry reuse the fun table row, and deferred function labels are resolved before writing the table.

The deterministic fixture checks direct and nested references and row reuse. Rebuild it with pinned `erlc.exe +deterministic beam_local_reference_probe.erl`. The runtime probe creates a reference inside a lifted closure and invokes the resulting function.

Build the corpus tool with `dotnet build tools/Validation/Erlang.BeamCorpus -m:1 --no-restore -v:q`. Run it with the pinned installation and report destination. Build the focused test runner with `dotnet build tests/Erlang.Tests -m:1 --no-restore -v:q`.

The deterministic fixture matched byte for byte and all three focused tests passed. The full corpus reported 100 Equal, 73 Different, 164 SourceRejected and 1,029 Unsupported. Of the 34 prior function-reference failures, xmerl_xs emitted a different image and 33 reached subsequent blockers. Selective receive is the most frequent remaining specific expression failure at 33 files. The full runtime suite and canonical readiness regeneration were outside this run. [Corpus](otp-corpus.json) and [focused tests](focused.json).
