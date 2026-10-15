# Part302 tooling promotion and one-dispatch draft

This recipe is artifact-only and has not been executed. It changes no Main source, Git state or remote ref and performs no build, Erlang/native execution or network request. Never infer the future Part301/302 head from a filename or this recipe. Supply the actual reviewed checkout head at each step.

The source package is the immutable sibling raw-onload-combined303-execution-proposal2. Freeze SHA256 is A5D7FBD058265DCDFF948E04485612D35FE68F838F1FC7FDB9D5085D6FEFA3E2. Its19 overlay files consist of18 new tooling/workflow files and one byte-identical existing helper. The separate18-record execution closure SHA256 is97C447A8AD78D37EA4F092963B7D4E5A7FEF5879E55DF1E4E4739F1409D5FBA9. The nine-observation source plan remains36F3A580DAAB83675014C835F5537861C894440D761479B839384B722EDD5319. Nothing in this recipe modifies or reseals those files.

## Promotion and local verification, future operator actions

1. Publish the reviewed Part301 result first. Confirm its actual local and remote head and retain its real runtime validation evidence. This tooling-only increment must not claim a new runtime pass or silently carry additional runtime edits.
2. Review then invoke Promote-ReviewedRawOnLoad302.ps1 with -Repository pointing to Main and -ExpectedHead set to that explicit current local head. The script checks the complete frozen package before writing, refuses conflicting existing targets, copies only missing exact files and appends byte-preservation attributes. It neither stages nor commits. The unchanged original13 helper must already exist with its reviewed hash. Equivalent manual promotion is acceptable after applying the same checks.
3. Write the English Part302 changelog/validation documentation with source-only/synthetic status and the observed-plan/execution-seal distinction. These docs are outside the execution closure. Preserve the tool/workflow bytes; do not autoformat or normalize their newlines. Review git diff and stage only the18 new tooling/workflow files, .gitattributes and the intended Part302 docs. The existing helper must have no content diff. Recheck final installed execution-plan SHA remains97C447... before committing.
4. Run the local wrapper after promotion with the explicit current local head and a fresh evidence directory. Example (fill real paths/head):

       py -3 validate-local302.py --repository <Main> --expected-head <actual-current-head> --output <fresh-local-evidence>

   It verifies the19 installed files against the frozen package before importing the runner's pure closure helpers, checks exact18-path/byte execution closure, then invokes the frozen35 decoder controls and report-budget controls directly. Every child has PYTHONDONTWRITEBYTECODE=1 and enabled assertions. Commands, raw stdout/stderr and result are retained. It performs read-only git rev-parse HEAD, no Git mutation or remote access. It deliberately does not invoke run-reviewed.py verify or fabricate GITHUB_* values: CiIdentityVerified=false, OriginalExecuted=false, RawCaptureControlsExecuted=false, SdkExecuted=false, RuntimeRetested=false. POSIX process-group capture controls and the nine original VMs remain workflow-only.
5. Commit and publish Part302 after those checks. Read actual local and remote published head; do not guess or reuse the Part301 SHA. The exact source closure should remain identical after commit. If any source bytes changed, stop: create/review a new package/seal rather than supplying the old approved seal.

The unchanged-runtime validation claim is conditional on the actual Part301 full result and absence of runtime changes in Part302. This recipe has not verified future head, tests or publication and supplies no test count of its own.

## Single dispatch using the controlled main branch

Prepare-Dispatch302.ps1 accepts -ExactPublishedHead, -RepositorySlug and a fresh -OutputDirectory. It writes dispatch.json with ref=main and the fixed approved_execution_plan_sha256 input, plus dispatch-context.json marked DRAFT_NOT_DISPATCHED. It performs no remote check and no POST. Use the same evidence directory for the operator's following actions.

Immediately before dispatch, read GET repos/<owner>/<repo>/branches/main and require commit.sha equals the explicitly recorded published Part302 SHA. Save the raw branch response. Record the attempt timestamp, exact expected head, workflow filename, main ref, approved execution-plan SHA and dispatch-body SHA **before** the POST. Refuse a second attempt if that record exists. If preflight mismatches, do not POST and do not reinterpret a different head as the requested run. No tag creation or repository variable update is needed.

Then execute exactly one structured POST, retaining raw response stdout/stderr and CLI exit status:

    gh api --method POST repos/<owner>/<repo>/actions/workflows/raw-onload-original-observations.yml/dispatches --input <fresh-evidence>/dispatch.json --include

Do not retry automatically after a timeout, connection loss or ambiguous response: the first dispatch may already have succeeded. Inspect the external workflow-run records before deciding anything further. An HTTP acknowledgement or empty CLI output is not completed original evidence.

## External reader acceptance

The external reader must bind the actual run to this single attempt, workflow_dispatch event, workflow path, main branch, run/attempt/job and **exact expected Part302 head**; reject any mismatching run head even if its job succeeds. Recheck remote branch identity separately if it moves. Verify dispatch approved input equals97C447..., complete job terminal status, artifact archive hash and its exact file closure, build provenance harness/run/attempt/job, tool/source/header hashes and observed-plan36F3... binding. The local guard's CiIdentityNetworkAuthenticated=false is intentional: its self-reported identity is not this API/artifact proof.

Require all nine independent observations and actual certain assertions. A raw collector receipt is not a semantic pass. Surface any ORIGINAL_NINE_CERTAIN_PASS_HYPOTHESIS_CONTRADICTED status explicitly; the workflow warning and field-level contradictions may not be relabeled a matched hypothesis. No result from this original-only workflow proves an SDK comparison, generic runtime parity, native ABI behavior or new runtime regression pass. Until the external reader accepts actual terminal evidence, documentation remains pending original observation.
