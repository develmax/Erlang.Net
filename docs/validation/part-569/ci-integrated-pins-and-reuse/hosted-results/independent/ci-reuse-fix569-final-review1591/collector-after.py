"""Read-only GitHub provenance collection. Network or schema uncertainty reruns CI."""
import hashlib
import io
import json
import os
from pathlib import Path
import subprocess
import urllib.parse
import urllib.request
import urllib.error
import zipfile

from policy import EVENTS, REQUIRED, admit


class NoRedirect(urllib.request.HTTPRedirectHandler):
    def redirect_request(self, request, response, code, message, headers, new_url):
        return None


def storage_request(location):
    parsed = urllib.parse.urlparse(location)
    allowed = parsed.hostname and (parsed.hostname.endswith(".blob.core.windows.net")
                                  or parsed.hostname.endswith(".actions.githubusercontent.com")
                                  or parsed.hostname.endswith(".githubusercontent.com"))
    if parsed.scheme != "https" or not allowed or parsed.username or parsed.password:
        raise ValueError("Artifact redirect is not an approved HTTPS storage origin")
    # Deliberately create a new request: never forward Authorization to storage.
    return urllib.request.Request(location, headers={"Accept": "application/octet-stream"})


class GitHub:
    def __init__(self, repository, token):
        self.repository = repository
        self.token = token

    def get(self, suffix, binary=False):
        url = "https://api.github.com/repos/" + self.repository + "/" + suffix
        request = urllib.request.Request(url, headers={
            "Authorization": "Bearer " + self.token,
            "Accept": "application/vnd.github+json", "X-GitHub-Api-Version": "2022-11-28",
        })
        if binary:
            opener = urllib.request.build_opener(NoRedirect())
            try:
                with opener.open(request, timeout=60) as response:
                    raw = response.read()
            except urllib.error.HTTPError as error:
                if error.code not in (301, 302, 303, 307, 308):
                    raise
                location = error.headers.get("Location", "")
                anonymous = storage_request(location)
                with urllib.request.build_opener(NoRedirect()).open(anonymous, timeout=60) as response:
                    raw = response.read()
        else:
            with urllib.request.build_opener(NoRedirect()).open(request, timeout=60) as response:
                raw = response.read()
        return raw if binary else json.loads(raw)

    def pages(self, suffix, key=None):
        rows = []
        for page in range(1, 11):
            separator = "&" if "?" in suffix else "?"
            value = self.get(suffix + separator + "per_page=100&page=" + str(page))
            batch = value[key] if key else value
            if not isinstance(batch, list):
                raise ValueError("GitHub pagination schema differs")
            rows.extend(batch)
            if len(batch) < 100:
                return rows
        raise ValueError("Evidence exceeds bounded pagination")


def git(root, *args):
    return subprocess.check_output(["git", "-C", str(root), *args], text=True, stderr=subprocess.STDOUT).strip()


def run_snapshot(runs):
    fields = ["id", "path", "event", "head_sha", "run_attempt", "status", "conclusion"]
    return sorted([tuple(run.get(field) for field in fields) for run in runs if run.get("path") in REQUIRED],
                  key=lambda row: str(row[0]))


def normalize_run(api, run, head, repository):
    if run.get("head_repository", {}).get("full_name") != repository:
        raise ValueError("Workflow head belongs to another repository")
    attempt = run["run_attempt"]
    if attempt != 1:
        raise ValueError("Repeated attempt cannot prove absence of an unexplained earlier failure")
    # Re-fetch the concrete run; list results alone do not establish current completion.
    fresh = api.get("actions/runs/" + str(run["id"]))
    if fresh["run_attempt"] != attempt or fresh["head_sha"] != head:
        raise ValueError("Workflow attempt/head moved")
    jobs = api.pages("actions/runs/" + str(run["id"]) + "/attempts/" + str(attempt) + "/jobs", "jobs")
    normalized = []
    for job in jobs:
        steps = job.get("steps", [])
        normalized.append({
            "name": job["name"], "status": job["status"], "conclusion": job["conclusion"],
            "steps_complete": bool(steps) and all(step["status"] == "completed" for step in steps),
            "failed_steps": [step["number"] for step in steps
                             if step["conclusion"] not in ("success", "skipped")],
        })
    return dict(path=fresh["path"], repository=repository, event=fresh["event"], head=fresh["head_sha"],
                status=fresh["status"], conclusion=fresh["conclusion"], run_id=fresh["id"], attempt=attempt,
                latest_attempt_verified=True, jobs=normalized)


def collect(root, fingerprint, job):
    repository = os.environ["GITHUB_REPOSITORY"]
    commit = os.environ["GITHUB_SHA"]
    if os.environ.get("GITHUB_EVENT_NAME") != "push" or os.environ.get("GITHUB_REF") != "refs/heads/main":
        return {"Reuse": False, "Reasons": ["PR/manual runs always execute required checks"]}
    api = GitHub(repository, os.environ["GH_TOKEN"])
    # A successful earlier PR must never erase an observed failure of this merge.
    current_runs = api.pages("actions/runs?head_sha=" + urllib.parse.quote(commit), "workflow_runs")
    for run in current_runs:
        if run.get("path") in REQUIRED and run.get("conclusion") in ("failure", "timed_out", "cancelled", "action_required"):
            raise ValueError("Existing unsuccessful merge validation requires full validation: " + str(run["id"]))
        if run.get("path") in REQUIRED and run.get("run_attempt") != 1:
            raise ValueError("Repeated merge validation attempt requires full validation")
        if run.get("path") in REQUIRED and str(run.get("id")) != os.environ.get("GITHUB_RUN_ID"):
            if run.get("status") != "completed" or run.get("conclusion") != "success":
                raise ValueError("Other merge validation is incomplete or ambiguous")
    if os.environ.get("GITHUB_RUN_ATTEMPT") != "1":
        raise ValueError("Repeated merge attempt requires full validation")
    parents = git(root, "show", "-s", "--format=%P", commit).split()
    if len(parents) != 2:
        raise ValueError("Only an ordinary two-parent merge can reuse PR evidence")
    candidates = api.pages("commits/" + commit + "/pulls")
    candidates = [pr for pr in candidates if pr.get("merged_at") and pr.get("merge_commit_sha") == commit]
    if len(candidates) != 1:
        raise ValueError("No unique merged PR provenance")
    pr = api.get("pulls/" + str(candidates[0]["number"]))
    head = pr["head"]["sha"]
    ancestor = subprocess.run(["git", "-C", str(root), "merge-base", "--is-ancestor", parents[0], head],
                              stdout=subprocess.PIPE, stderr=subprocess.PIPE).returncode == 0
    tree = git(root, "rev-parse", commit + "^{tree}")
    tested_tree = git(root, "rev-parse", head + "^{tree}")
    equivalent = {line.split()[0] for line in git(root, "log", "--format=%H %T", commit).splitlines()
                  if len(line.split()) == 2 and line.split()[1] == tree} - {commit, head}
    if len(equivalent) > 10:
        raise ValueError("Equivalent-tree history exceeds bounded evidence collection")
    history_snapshots = {}
    for previous in sorted(equivalent):
        history = api.pages("actions/runs?head_sha=" + urllib.parse.quote(previous), "workflow_runs")
        if any(run.get("path") in REQUIRED and (run.get("status") != "completed" or run.get("conclusion") != "success" or run.get("run_attempt") != 1) for run in history):
            raise ValueError("Earlier equivalent-tree validation is unsuccessful or incomplete")
        history_snapshots[previous] = run_snapshot(history)
    runs = api.pages("actions/runs?head_sha=" + urllib.parse.quote(head), "workflow_runs")
    if any(run.get("path") in REQUIRED and run.get("run_attempt") != 1 for run in runs):
        raise ValueError("Repeated attempt requires full validation of tested-head evidence")
    if any(run.get("path") in REQUIRED and (run.get("status") != "completed" or run.get("conclusion") != "success") for run in runs):
        raise ValueError("Unsuccessful or incomplete tested-head evidence requires full validation")
    selected = []
    for path in REQUIRED:
        matches = [run for run in runs if run.get("path") == path and run.get("event") in EVENTS[path]]
        if not matches:
            raise ValueError("Required workflow is missing: " + path)
        latest = max(matches, key=lambda run: run["id"])
        selected.append(normalize_run(api, latest, head, repository))
    validation = next(run for run in selected if run["path"] == ".github/workflows/validate.yml")
    artifact_name = "ci-reuse-v1-" + job
    artifacts = api.pages("actions/runs/" + str(validation["run_id"]) + "/artifacts", "artifacts")
    artifacts = [artifact for artifact in artifacts if artifact["name"] == artifact_name and not artifact["expired"]]
    if len(artifacts) != 1:
        raise ValueError("No unique trusted job receipt artifact")
    artifact = artifacts[0]
    origin = artifact.get("workflow_run", {})
    if origin.get("head_sha") != head or origin.get("id") != validation["run_id"]:
        raise ValueError("Receipt artifact origin differs")
    raw = api.get("actions/artifacts/" + str(artifact["id"]) + "/zip", binary=True)
    actual = "sha256:" + hashlib.sha256(raw).hexdigest()
    if artifact.get("digest") != actual:
        raise ValueError("Receipt ZIP digest is not verified")
    with zipfile.ZipFile(io.BytesIO(raw)) as archive:
        if archive.namelist() != ["receipt.json"]:
            raise ValueError("Unexpected receipt ZIP members")
        if archive.getinfo("receipt.json").file_size > 2 * 1024 * 1024:
            raise ValueError("Receipt exceeds bound")
        receipt = json.loads(archive.read("receipt.json"))
    if receipt.get("Head") != head or receipt.get("Run") != validation["run_id"] or receipt.get("Attempt") != validation["attempt"]:
        raise ValueError("Receipt producer head/run/attempt differs")
    # Downloading the artifact may take time. Do not admit an earlier API snapshot
    # after a producer rerun or a new merge validation has appeared meanwhile.
    refreshed_head = api.pages("actions/runs?head_sha=" + urllib.parse.quote(head), "workflow_runs")
    if run_snapshot(refreshed_head) != run_snapshot(runs):
        raise ValueError("Tested-head workflows changed during admission")
    for run in selected:
        fresh = api.get("actions/runs/" + str(run["run_id"]))
        if (fresh.get("run_attempt") != run["attempt"] or fresh.get("head_sha") != head
                or fresh.get("status") != "completed" or fresh.get("conclusion") != "success"):
            raise ValueError("Producer completion changed during admission")
    for previous, before in history_snapshots.items():
        refreshed = api.pages("actions/runs?head_sha=" + urllib.parse.quote(previous), "workflow_runs")
        if run_snapshot(refreshed) != before:
            raise ValueError("Equivalent-tree workflows changed during admission")
    refreshed_current = api.pages("actions/runs?head_sha=" + urllib.parse.quote(commit), "workflow_runs")
    if run_snapshot(refreshed_current) != run_snapshot(current_runs):
        raise ValueError("Merge workflows changed during admission")
    context = dict(event=os.environ["GITHUB_EVENT_NAME"], ref=os.environ["GITHUB_REF"], repository=repository,
                   commit=commit, parents=parents, base_is_ancestor_of_pr=ancestor, tree=tree, job=job,
                   fingerprint=fingerprint, fingerprint_complete=True)
    evidence = dict(pull_request=dict(repository=repository, head_repository=pr["head"]["repo"]["full_name"],
                    merged=pr["merged"], base_ref=pr["base"]["ref"], merge_commit=pr["merge_commit_sha"], head=head),
                    tested_tree=tested_tree, fingerprint=receipt.get("Fingerprint"),
                    fingerprint_complete=receipt.get("FingerprintComplete") is True,
                    artifact_verified=True, artifact_head=origin.get("head_sha"), artifact_repository=repository,
                    artifact_run=origin.get("id"), receipt_run=receipt.get("Run"), receipt_job=receipt.get("Job"),
                    receipt_completed=receipt.get("Completed") is True,
                    # This gate changes push-main work only. PR checks and their current-review gate are unchanged.
                    review_preserved=True, workflows=selected)
    decision = admit(context, evidence)
    decision["Origin"] = dict(PullRequest=pr["number"], WorkflowRun=validation["run_id"],
                              Attempt=validation["attempt"], Artifact=artifact["id"], ZipDigest=actual)
    return decision
