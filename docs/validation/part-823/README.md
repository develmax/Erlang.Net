# Part823 SSL and TLS named supervisor images

The C# emitter retains the local name helper call before the remote tail call. It uses a literal tuple when the helper's result can be proved from its atom clauses. When a wrapper forwards an existing argument, the emitter saves it across the local call and preserves the helper's result in X0.

The private helper uses one exact comparison once every call site is shown to pass one of the two allowed atoms and the function has no escaping reference. The exported helper retains both tests via a value table. The distinction follows the argument-type inference for private and exported functions in the pinned OTP compiler's `beam_ssa_type.erl`.

Both full images match Windows OTP 29.1.1. The pinned OTP probe verified registration and child routing for both normal and distribution supervisors, using stub child modules, and compared initialization results. [Image hashes](oracle.txt), [probe](behavior.eval) and [results](behavior.txt).

The corpus tool was built with `dotnet build tools/Validation/Erlang.BeamCorpus -m:1 --no-restore -v:q -o artifacts/beam-backend/part823-tool`. The runner was built with `dotnet build tests/Erlang.Tests -m:1 --no-restore -v:q -o artifacts/beam-backend/part823-tests`.

The 1,366-file corpus reported 86 Equal, 195 SourceRejected and 1,085 Unsupported. Exactly the two named supervisors changed status. The corpus CLI returned 1 because overall parity remains incomplete. All four focused tests passed, including the permanent full-image regression for both supervisors. The full runtime suite and canonical readiness regeneration were outside this run. [Corpus report](otp-corpus.json) and [focused report](focused.json).

The next SSL emission gaps include forwarding a dynamic value in the child specification of tls_dyn_connection_sup and handling the child-count dispatch in ssl_upgrade_server_session_cache_sup.
