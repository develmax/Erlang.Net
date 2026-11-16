# Part890 Callback attributes and no_auto_import

The source mapper accepts callback and optional callback forms. The BEAM emitter builds behaviour_info/1 and its function-clause helper from parsed declarations. Release debug metadata includes the declarations, and EEP-48 documentation keeps callback entries separate from function entries. The compiler accepts no_auto_import in BEAM generation because source call resolution already applies it. The unsupported no_ssa_opt directive remains rejected.

The deterministic fixtures cover callback order, optional callbacks, no_auto_import and documented callbacks. Rebuild with `erlc +deterministic beam_callback_probe.erl beam_callback_docs_probe.erl`. Both complete images matched byte for byte. [OTP assembly](otp-direct-disassembly.S).

Generated behaviour_info/1 returned the full callback list and the optional subset on OTP and in the managed runtime.

Build the corpus tool with `dotnet build tools/Validation/Erlang.BeamCorpus -m:1 --no-restore -v:q` and run its DLL with the pinned installation and report paths. Build the focused runner with `dotnet build tests/Erlang.Tests -m:1 --no-restore -v:q` and select the whole-module, Docs and installed release image cases. All three passed. Full runtime and canonical readiness checks remain pending. [Focused report](focused.json).

The full 1,366-file corpus reported 100 Equal, 540 Different, 164 SourceRejected and 562 Unsupported. Fifty-five files moved from Unsupported to Different, including 33 previously blocked on callback and 22 on compile. The largest remaining groups are comprehension lowering (97) and pattern lowering (96). [Corpus report](otp-corpus.json).
