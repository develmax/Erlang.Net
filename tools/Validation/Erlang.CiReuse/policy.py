"""Fail-closed admission of a completed PR matrix for an identical main merge.

This module never creates GitHub approvals, changes protection or edits results.
The network collector must verify artifacts before passing normalized evidence.
"""
import hashlib
import json

REQUIRED = {
    ".github/workflows/validate.yml": {
        "local-regressions", "original-resource-controls504", "relocated-tooling",
        "local-regressions (core)", "local-regressions (native)",
        "local-regressions (owned)", "local-regressions (observers)",
        "local-regressions (libraries)", "local-regressions (catalog)",
        "local-regressions (packages)", "local-regressions (tooling)", "local-regressions (bif)",
    },
    ".github/workflows/math-compatibility.yml": {"math (windows-latest)", "math (ubuntu-latest)", "math (macos-latest)"},
    ".github/workflows/nif-version651-linux.yml": {"n1-linux"},
    ".github/workflows/native-gc825.yml": {"linux"},
    ".github/workflows/nif-upgrade-integrated.yml": {"integrated-linux"},
}
EVENTS = {path: {"pull_request", "workflow_dispatch"} for path in REQUIRED}
EVENTS[".github/workflows/validate.yml"] = {"pull_request"}
EVENTS[".github/workflows/nif-upgrade-integrated.yml"] = {"workflow_dispatch"}
FINGERPRINT_FIELDS = {"Tree", "WorkflowCheckerSources", "Dependencies", "Configuration", "Provider", "Platform", "Toolchain"}


def canonical(value):
    return json.dumps(value, sort_keys=True, separators=(",", ":"), ensure_ascii=True).encode()


def digest(value):
    return hashlib.sha256(canonical(value)).hexdigest().upper()


def admit(context, evidence):
    """Return an auditable decision; any incomplete or different input runs CI."""
    reasons = []

    def require(condition, reason):
        if not condition:
            reasons.append(reason)

    require(context.get("event") == "push" and context.get("ref") == "refs/heads/main", "not-main-push")
    pr = evidence.get("pull_request", {})
    repository = context.get("repository")
    require(bool(repository) and pr.get("repository") == repository, "repository-mismatch")
    require(pr.get("head_repository") == repository, "untrusted-fork")
    require(pr.get("merged") is True and pr.get("base_ref") == "main", "not-merged-main-pr")
    require(pr.get("merge_commit") == context.get("commit"), "merge-commit-mismatch")
    parents = context.get("parents", [])
    require(len(parents) == 2 and parents[1] == pr.get("head"), "unsupported-or-different-merge-parents")
    require(context.get("base_is_ancestor_of_pr") is True, "changed-main-not-in-tested-pr")
    require(bool(context.get("tree")) and context.get("tree") == evidence.get("tested_tree"), "tree-mismatch")
    require(context.get("fingerprint_complete") is True, "missing-current-fingerprint")
    require(evidence.get("fingerprint_complete") is True, "missing-tested-fingerprint")
    for label, source in [("current", context), ("tested", evidence)]:
        fingerprint = source.get("fingerprint")
        valid = isinstance(fingerprint, dict) and FINGERPRINT_FIELDS.issubset(fingerprint)
        require(valid, "fingerprint-schema-missing:" + label)
        if valid:
            require(all(fingerprint[name] for name in FINGERPRINT_FIELDS), "fingerprint-empty-input:" + label)
            require(fingerprint["Tree"] == context.get("tree"), "fingerprint-tree-not-bound:" + label)
            require(isinstance(fingerprint["Platform"], dict) and bool(fingerprint["Platform"]), "platform-schema:" + label)
            require(isinstance(fingerprint["Toolchain"], dict) and bool(fingerprint["Toolchain"]), "toolchain-schema:" + label)
    require(context.get("fingerprint") == evidence.get("fingerprint"), "environment-dependency-config-provider-mismatch")
    require(evidence.get("artifact_verified") is True, "missing-or-unverified-artifact")
    require(evidence.get("artifact_head") == pr.get("head"), "artifact-head-mismatch")
    require(evidence.get("artifact_repository") == repository, "artifact-origin-mismatch")
    require(evidence.get("artifact_run") == evidence.get("receipt_run"), "artifact-run-mismatch")
    require(evidence.get("receipt_job") == context.get("job"), "receipt-job-mismatch")
    require(evidence.get("receipt_completed") is True, "incomplete-receipt")
    require(evidence.get("review_preserved") is True, "review-policy-not-preserved")

    runs = evidence.get("workflows", [])
    by_path = {run.get("path"): run for run in runs}
    require(len(by_path) == len(runs), "duplicate-workflow-evidence")
    require(set(by_path) == set(REQUIRED), "missing-required-workflow")
    for path, expected_jobs in REQUIRED.items():
        run = by_path.get(path, {})
        require(run.get("repository") == repository and run.get("event") in EVENTS[path], "untrusted-workflow:" + path)
        require(run.get("head") == pr.get("head"), "workflow-head-mismatch:" + path)
        require(run.get("status") == "completed" and run.get("conclusion") == "success", "workflow-not-successful:" + path)
        require(run.get("latest_attempt_verified") is True, "unverified-attempt:" + path)
        jobs = run.get("jobs", [])
        require(len(jobs) == len(expected_jobs) and {job.get("name") for job in jobs} == expected_jobs,
                "missing-required-matrix:" + path)
        for job in jobs:
            label = path + ":" + str(job.get("name"))
            require(job.get("status") == "completed" and job.get("conclusion") == "success", "job-not-successful:" + label)
            require(job.get("steps_complete") is True, "incomplete-job-steps:" + label)
            require(not job.get("failed_steps"), "failed-or-timedout-step:" + label)
    return {
        "Reuse": not reasons,
        "Reasons": sorted(set(reasons)),
        "CurrentCommit": context.get("commit"),
        "TestedHead": pr.get("head"),
        "Tree": context.get("tree"),
        "EvidenceSha256": digest(evidence),
        "CurrentFingerprintSha256": digest(context.get("fingerprint")),
        "FormalApprovalCreated": False,
        "ProtectionChanged": False,
    }
