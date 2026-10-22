Artifact-only recipes; not executed by author. No Main, git, SDK, native or network execution.

combined-original.yml replaces existing original workflow. ordsets-preflight.py copies to tools/Erlang.OrdsetsCoreOriginalObservations/ci-preflight.py and is bound by producer Git/dispatch Bindings separately from unchanged ordsets source manifest. One original build serves both independent adapters; own successful preparation is required; both collectors execute even after the other fails; aggregate fails unless both succeed; upload preserves both outcomes.

Passive audit CLI:
python -B artifacts/erlang371-passive-proposal2/audit.py --download-run REMOTE --artifact EXTRACTED --tool LOCAL_ADMITTED_TOOL --bindings DISPATCH_INTENT_JSON --output artifacts/erlang371-passive-proposal2/actual1 --expected-head EXACT_SHA --manifest-sha256 EXACT_ERLANG_MANIFEST_SHA --zip-sha256 API_ZIP_SHA --run-id RUN --job-id JOB --artifact-id ARTIFACT

REMOTE contains exact GitHub API run.json/jobs.json/artifacts.json and artifact-ID.zip. EXTRACTED must exactly match every ZIP entry byte and contain build/, preflight/ and capture/. Bindings JSON uses {Head: exact_SHA, Bindings:[{Path,Bytes,Sha256}]} and should include workflows and the new ordsets gate. Source admission re-reads producer files through git show at EXACT_SHA, compares all12008 pinned source blobs/modes, checks13 owned native processes and3 preflight processes, tests15 corruptions (five metadata packets x module MD5/scheduler/name corruption), proves exact751 values/3755 stack profiles. Default/0/1/2/8 stacks, error_info and all argument fields retained. Matched compiled-worker context checked in original command expressions. Native code and downloaded artifact code are never executed by passive review.

Comparison CLI:
python -B artifacts/erlang371-passive-proposal2/compare.py --native EXTRACTED/capture --sdk ACTUAL_MAIN_SDK --native-admission artifacts/erlang371-passive-proposal2/actual1/acceptance.json --native-admission-sha256 EXACT_ADMISSION_SHA --tool LOCAL_ADMITTED_TOOL --expected-head EXACT_SHA --manifest-sha256 EXACT_ERLANG_MANIFEST_SHA --output FRESH_ARTIFACT_DIRECTORY

Comparator verifies actual five Main DLL hashes, capture receipt, SDK source/compile/seal, admitted raw native bytes, exact names and five profile depths. It compares every typed field strictly (including binary IEEE float bits, external identity fields, entire stack and metadata). No frame deletion, location masking, PID waiver, source-line normalization or FUN relation. Differences are retained in full; nonzero exit on any mismatch. No automatic full-contract promotion.

Successor2 also expands every shared build manifest Files/Dependencies/Workflow entry into both producer snapshot and git-verified authority. The original pinned build recipe/owned process dependencies are bound independently rather than treating the manifest JSON alone as executable-source proof.
