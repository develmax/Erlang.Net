# Allocator/memory original-only proposal: frozen handoff

Artifact-only handoff; no Main edits, Git mutation, POST/dispatch, build, native execution or original execution occurred. `validation-result.json` and `source-freeze.json` are the current final records. Earlier `pre-peer-*`, `superseded-*`, `final-composed-projections46.json` and old synthetic projection records are retained history, not current seals.

The promotion map is exactly `source-freeze.json:PromotedFiles`: copy each `overlay/<Path>` to repository `<Path>`, preserving bytes. There are 12 new tool files (including the execution seal) and one new workflow. Do not copy the artifact-level validator or composed-source tree into tools. The six `UnchangedPublishedHelpers` already exist in Main and must remain byte-exact. Their paths and SHA256 values are in the same map. `parent-reuse-bindings.json` records additional derivation parents; it does not authorize replacing those parents.

The approved execution-plan SHA256 is **C73AFABAF43F98F81BD94C52788189429F85931C3D7228C8C8346C38FCEDD0F5**. Its closure is 11 allocator source files, one workflow and six unchanged published helpers; the seal itself is the twelfth allocator tool file. Observation plan SHA256 is **B565EA20C36B326D98CE9F854556C432142F0E074D33077679F6BF482CDBCC54**; policy SHA256 is **8C205BF5171D233DEAD25A12C8F36E1CA90F2866C531BC2D29A0295997515C4B**. Source audit seal is **AEE91714DF27F17289A3FC3E73276261E2455CD01866C0FE35944D2786A7C7A6**.

Before promotion the following read-only command validates the complete composed closure. After byte-exact promotion replace the repository argument with `D:/Distr/Work/Codex/Erlang.Net/Erlang.Net`. It executes no Git, original VM or SDK and writes no files:

```powershell
py -3.12 -B D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/allocator-memory309-original18-proposal1/validate-frozen-source.py --repository D:/Distr/Work/Codex/Erlang.Net/Erlang.Net/artifacts/allocator-memory309-original18-proposal1/composed-source
```

Fresh local results are `frozen-projections52.json`, `frozen-decoder35.json`, `frozen-stack16.json`, and `frozen-report-budget/`. The 52 controls include a positive actual-shaped full-info tree and refusals for disabled false, empty list, empty instance/sizes-only data, missing calls and malformed version arity. The preserved first18 run bodies were compared to the baseline, with only the final clause punctuation changed to append case19. All six reused helper files were compared to current Main bytes. Python syntax was parsed without compiling/executing original code.

After independent peer source approval, ordinary publication can include this tooling alongside part306. After publication record the immutable published SHA containing the exact validated files; verify the workflow is present there. The dispatch recipe below is a future once-only authorized operation, not an operation performed by this handoff. Use the authorized published tag/ref verified to resolve to that SHA as `<published-ref>`; the resulting run head must equal the recorded SHA. Run exactly once and retain the returned run identity instead of redispatching if the response is ambiguous:

```powershell
gh workflow run allocator-memory-original-observations.yml --repo develmax/Erlang.Net --ref <published-ref> -f approved_execution_plan_sha256=C73AFABAF43F98F81BD94C52788189429F85931C3D7228C8C8346C38FCEDD0F5
```

Then inspect that exact run, head, job and attempt and preserve the uploaded `allocator-memory-original-nineteen-observations` artifact and terminal logs. Review complete receipt/file closure, compiler/disassembly, installed source-built binaries/header provenance, owned-process completion and typed report independently. A green workflow alone is not authenticated terminal evidence. Preserve failures without changing the policy, retrying or excluding cases. The workflow compiles the exact pinned original OTP29.1.1/ad05823719d77c8faee87348ea39513d4e2f99c5 once and collects19 fresh VMs:18 historical proposal cases (9 allocator,6 memory,3 adjacent) and one separately named positive `alloc_info_binary_full`.

There is no successful original observation yet. Disabled allocator/memory `notsup` precedence remains a separately deferred startup profile; its exact pinned disable flag is `+MHe false` and is not enabled by this19-case default profile. Physical memory values are original investigative data, with `SdkCompared`, `SdkPass` and `GenericRuntimeParity` false. Prior269 SDK hash allocation diagnostics are unrelated to native allocator API proof.
