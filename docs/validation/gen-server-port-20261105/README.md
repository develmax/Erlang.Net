# gen_server compatibility

The C# backend implements the 42 non-metadata exports of OTP 29.1.1. Select it with `RuntimeModuleSelection.Managed("gen_server")`. The existing BEAM selection executes the original Erlang module.

The [original capture](final-107/provenance.json) binds the OTP inputs and observer sources. The [Managed report](main-replay/Managed-report.json) and [BEAM report](main-replay/Beam-report.json) cover 107 API contracts and 70 scenarios per backend, including four tests from the pinned upstream suite. The reports record the runtime and managed assembly hashes.

The replay targets main at `9132cafb499215698a0accce7d3d621f7459398e` plus this change. The initial replay reproduced a missing stack frame for a non-tail wrong-arity callback in `system_replace_state/2`. The interpreter now uses its existing failed-fun capture helper with the call site's tail position.

The [validator instructions](../../../tools/Validation/Erlang.GenServerCompatibility/README.md) describe the focused replay. Full upstream, distributed, platform and resource acceptance remains open.

## PR #125 CI admission

The first CI run stopped at source admission. The source-plan digests, byte counts, workflow pins, and descriptor-poll production tree now match the PR. The monitor source freeze records the same 488 paths in a new snapshot linked to its predecessor. The local preflight passed all 13 focused plans, 4,490 Resource529 dependencies, 5,400 MFA and native bindings, and the OwnedGuest506 and Unix filename source admissions.
