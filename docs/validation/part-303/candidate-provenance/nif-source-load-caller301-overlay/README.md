# Direct source load_nif caller prerequisite

Source-only artifact. No build, HOST, native fixture, original emulator, Main edit, commit, or publication was executed by the author.

The pinned OTP `ops.tab` rewrites literal `call_ext`, `call_ext_last`, and `call_ext_only` of `erlang:load_nif/2` to `i_load_nif` before deallocation/return. `bif_instrs.tab` passes the actual instruction pointer to `erts_load_nif`. The ordinary `load_nif_2` BIF rejects apply with `{error,{bad_lib,Message}}`. The exact pinned `erl_internal.erl` contains no auto-imported `load_nif/2`.

This overlay recognizes only explicit literal `Expr.Call` to `erlang:load_nif/2`, after evaluation of both arguments and before generic source/runtime tail transfer. Unqualified/local names, dynamic apply, and function references remain ordinary calls. No default handler, Core BIF, inventory change, native profile, or general loader is activated.

An internal handler is installed explicitly on one registry. Without it, the dedicated compiler branch falls back to the existing ordinary registry call. With it, the runtime captures the actual executing image and owner, reserves one additional real source-code lease, and passes an internal capability as an explicit delegate argument. There is no ambient authority or inherited marker. Capability consumption requires the exact ProcessContext, a live owned lease, and a previously unconsumed capability. Foreign-process attempts do not consume it. Its ownership ends after the actual handler task unwinds, including cancellation, cleanup failure, and post-callback halt checks. Generic calls retain no additional frame or allocation.

Capture reads the context code gate and releases it before registry admission. Reserving the additional lease follows the existing registry gate -> context code gate order. Handler work, awaiting, and tracing run outside the registry gate. Capability disposal drops its private gate before releasing the source lease. No unload or user callback runs under those gates. The capability does not acquire a code-modification ticket: a future loader must consume the exact caller and use the existing generation-checked coordinator, which independently owns its ticket and source lease across initialization.

Production files are Execution.cs plus four new files. Execution.cs is the only replacement; parent byte SHA is recorded in parents.json. There is no overlap with the held lifetime overlay's 12 runtime paths, no ModuleRegistry.cs or ProcessContext.cs replacement, and no native backend file change.

Register the additional HOST with:

```csharp
Test(Erlang.Tests.SourceLoadNifHostNames.Test, Erlang.Tests.SourceLoadNifHostChecks.Run);
```

HOST source covers unchanged behavior without a handler; direct tail and non-tail calls; a source tail with a preceding binding; cross-module caller ownership; local shadow exclusion; dynamic apply and external function-reference rejection using a locally registered fake ordinary stub with the pinned typed error; argument side effects before capture; no executing-source rejection; exact old-image capture followed by the real coordinator's OldCode refusal; one-shot and live foreign-context rejection; additional real lease references during a failing/cancelled handler's blocked finally; and no references after actual completion.

The pending test uses hash-checked genuine pending BEAM metadata in Staged phase, then explicitly evaluates a managed source call under that exact prepared owner/image. It verifies the capability and the existing coordinator agree on that pending identity. This is controlled managed source evaluation, not evidence for a compiled BEAM `i_load_nif` instruction or an actual source on_load callback. No new native/original parity claim is made. Generic BEAM load instructions, the public apply rejection stub, actual public load_nif activation, arbitrary native libraries, and full ABI/resource/dirty-thread support remain separate work.
