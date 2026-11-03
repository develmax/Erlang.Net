import copy
import hashlib
import io
import json
import os
from pathlib import Path
import tempfile
import unittest
from unittest.mock import patch
import zipfile

import collector
import main as entry
from policy import EVENTS, REQUIRED
from test_policy import fixture


class WorkflowLifecycleTests(unittest.TestCase):
    def test_other_incomplete_merge_run_is_not_ignored(self):
        class API:
            def __init__(self, repository, token):
                pass

            def pages(self, path, key=None):
                return [dict(id=89, path=".github/workflows/validate.yml", status="in_progress", conclusion=None, run_attempt=1)]

        env = dict(GITHUB_EVENT_NAME="push", GITHUB_REF="refs/heads/main", GITHUB_SHA="merge",
                   GITHUB_REPOSITORY="owner/repo", GH_TOKEN="test-only", GITHUB_RUN_ID="90", GITHUB_RUN_ATTEMPT="1")
        with patch.dict(os.environ, env), patch.object(collector, "GitHub", API):
            with self.assertRaisesRegex(ValueError, "incomplete or ambiguous"):
                collector.collect(Path("."), {}, "local-regressions")

    def test_existing_merge_failure_cannot_reuse_old_pr_success(self):
        class API:
            def __init__(self, repository, token):
                pass

            def pages(self, path, key=None):
                return [dict(id=88, path=".github/workflows/validate.yml", conclusion="failure")]

        env = dict(GITHUB_EVENT_NAME="push", GITHUB_REF="refs/heads/main", GITHUB_SHA="merge",
                   GITHUB_REPOSITORY="owner/repo", GH_TOKEN="test-only")
        with patch.dict(os.environ, env), patch.object(collector, "GitHub", API):
            with self.assertRaisesRegex(ValueError, "unsuccessful merge"):
                collector.collect(Path("."), {}, "local-regressions")

    def test_prepare_seal_and_verified_collector_reuse(self):
        context, base = fixture()
        fingerprint = context["fingerprint"]
        with tempfile.TemporaryDirectory() as folder:
            output = Path(folder) / "receipt"
            arguments = ["main.py", "prepare", "--job", "local-regressions", "--output", str(output),
                         "--provider-admission", "actual-provider.json"]
            env = {"GITHUB_EVENT_NAME": "pull_request", "GITHUB_RUN_ID": "100", "GITHUB_RUN_ATTEMPT": "1",
                   "GITHUB_SHA": "head", "EXPECTED_HEAD": "head", "GITHUB_REPOSITORY": "owner/repo"}
            with patch.dict(os.environ, env), patch("sys.argv", arguments), patch.object(entry, "capture", return_value=fingerprint):
                entry.main()
                self.assertFalse(json.loads((output / "decision.json").read_bytes())["Reuse"])
                arguments[1] = "seal"
                entry.main()
            receipt = (output / "receipt.json").read_bytes()
            zipped = io.BytesIO()
            with zipfile.ZipFile(zipped, "w") as archive:
                archive.writestr("receipt.json", receipt)
            archive_bytes = zipped.getvalue()
            runs = []
            for index, (path, names) in enumerate(REQUIRED.items()):
                runs.append(dict(id=100 + index, run_attempt=1, head_sha="head", head_repository={"full_name": "owner/repo"},
                                 path=path, event=sorted(EVENTS[path])[0], status="completed", conclusion="success"))

            class API:
                prior_failure = False
                current_attempt = 1
                late = None
                downloaded = False

                def __init__(self, repository, token):
                    pass

                def pages(self, path, key=None):
                    if path == "actions/runs?head_sha=merge":
                        values = [dict(id=900, path=".github/workflows/validate.yml", status="in_progress", conclusion=None,
                                       run_attempt=API.current_attempt)]
                        if API.downloaded and API.late in ("incomplete", "failure"):
                            values.append(dict(id=999, path=".github/workflows/validate.yml", run_attempt=1,
                                               status="in_progress" if API.late == "incomplete" else "completed",
                                               conclusion=None if API.late == "incomplete" else "failure"))
                        return values
                    if path == "actions/runs?head_sha=prior":
                        return [dict(path=".github/workflows/validate.yml", status="completed",
                                     conclusion="failure" if API.prior_failure is True or (API.downloaded and API.late == "history") else "success",
                                     run_attempt=2 if API.prior_failure == "retry" else 1)]
                    if path == "commits/merge/pulls":
                        return [dict(number=102, merged_at="2026-11-03", merge_commit_sha="merge")]
                    if path.startswith("actions/runs?head_sha="):
                        values = copy.deepcopy(runs)
                        if API.downloaded and API.late == "producer":
                            values[0]["run_attempt"] = 2
                        return values
                    if path.endswith("/jobs"):
                        number = int(path.split("/")[2]); run = next(run for run in runs if run["id"] == number)
                        return [dict(name=name, status="completed", conclusion="success",
                                     steps=[dict(number=1, status="completed", conclusion="success")])
                                for name in REQUIRED[run["path"]]]
                    if path == "actions/runs/100/artifacts":
                        return [dict(id=99, name="ci-reuse-v1-local-regressions", expired=False,
                                     digest="sha256:" + hashlib.sha256(archive_bytes).hexdigest(),
                                     workflow_run=dict(head_sha="head", id=100))]
                    raise AssertionError(path)

                def get(self, path, binary=False):
                    if path == "pulls/102":
                        return dict(merged=True, number=102, merge_commit_sha="merge", base=dict(ref="main"),
                                    head=dict(sha="head", repo=dict(full_name="owner/repo")))
                    if path.startswith("actions/runs/"):
                        value = copy.deepcopy(next(run for run in runs if run["id"] == int(path.split("/")[2])))
                        if API.downloaded and API.late == "concrete":
                            value["run_attempt"] = 2
                        return value
                    if path == "actions/artifacts/99/zip":
                        API.downloaded = True
                        return archive_bytes
                    raise AssertionError(path)

            def git(root, *args):
                if args[0] == "show":
                    return "base head"
                if args[0] == "log":
                    return "prior tree" if API.prior_failure or API.late == "history" else "head tree"
                return "tree"

            env.update(GITHUB_EVENT_NAME="push", GITHUB_SHA="merge", GITHUB_REF="refs/heads/main", GH_TOKEN="test-only", GITHUB_RUN_ID="900")
            with patch.dict(os.environ, env), patch.object(collector, "GitHub", API), patch.object(collector, "git", git), \
                    patch.object(collector.subprocess, "run", return_value=type("Result", (), {"returncode": 0})()):
                result = collector.collect(Path(folder), fingerprint, "local-regressions")
                for late in ["incomplete", "failure", "producer", "concrete", "history"]:
                    API.late = late
                    API.downloaded = False
                    with self.assertRaisesRegex(ValueError, "changed during admission"):
                        collector.collect(Path(folder), fingerprint, "local-regressions")
                API.late = None
                API.downloaded = False
                API.prior_failure = True
                with self.assertRaisesRegex(ValueError, "equivalent-tree"):
                    collector.collect(Path(folder), fingerprint, "local-regressions")
                API.prior_failure = False
                API.current_attempt = 2
                with self.assertRaisesRegex(ValueError, "Repeated merge"):
                    collector.collect(Path(folder), fingerprint, "local-regressions")
                API.current_attempt = 1
                API.prior_failure = "retry"
                with self.assertRaisesRegex(ValueError, "equivalent-tree"):
                    collector.collect(Path(folder), fingerprint, "local-regressions")
                API.prior_failure = False
                runs[0]["run_attempt"] = 2
                with self.assertRaisesRegex(ValueError, "Repeated attempt"):
                    collector.collect(Path(folder), fingerprint, "local-regressions")
            self.assertTrue(result["Reuse"], result)
            self.assertEqual(result["Origin"]["Artifact"], 99)

    def test_missing_fresh_assets_never_seals_a_receipt(self):
        with tempfile.TemporaryDirectory() as folder:
            args = ["main.py", "prepare", "--job", "local-regressions", "--output", folder,
                    "--provider-admission", "actual-provider.json"]
            with patch("sys.argv", args), patch.object(entry, "capture", side_effect=ValueError("resolved assets missing")):
                entry.main()
                self.assertFalse(json.loads((Path(folder) / "decision.json").read_bytes())["Reuse"])
                args[1] = "seal"; entry.main()
            self.assertFalse((Path(folder) / "receipt.json").exists())

    def test_failed_recapture_removes_stale_receipt(self):
        with tempfile.TemporaryDirectory() as folder:
            receipt = Path(folder) / "receipt.json"
            receipt.write_text('{"Completed":true}')
            args = ["main.py", "seal", "--job", "local-regressions", "--output", folder,
                    "--provider-admission", "missing-provider.json"]
            with patch("sys.argv", args):
                entry.main()
            self.assertFalse(receipt.exists())


if __name__ == "__main__":
    unittest.main()
