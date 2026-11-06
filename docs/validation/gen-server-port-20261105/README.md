# gen_server compatibility

The C# backend implements the 42 non-metadata exports of OTP 29.1.1. Select it with `RuntimeModuleSelection.Managed("gen_server")`. The existing BEAM selection executes the original Erlang module.

The [original capture](final-107/provenance.json) binds the OTP inputs and observer sources. The [Managed report](main-replay/Managed-report.json) and [BEAM report](main-replay/Beam-report.json) cover 107 API contracts and 70 scenarios per backend, including four tests from the pinned upstream suite. The reports record the runtime and managed assembly hashes.

The replay targets main at `9132cafb499215698a0accce7d3d621f7459398e` plus this change. The initial replay reproduced a missing stack frame for a non-tail wrong-arity callback in `system_replace_state/2`. The interpreter now uses its existing failed-fun capture helper with the call site's tail position.

The [validator instructions](../../../tools/Validation/Erlang.GenServerCompatibility/README.md) describe the focused replay. Full upstream, distributed, platform and resource acceptance remains open.
