"""Independent offline lifecycle checks against the published fixed collector."""
import hashlib
import json
import os
from pathlib import Path
import sys
from unittest.mock import patch

source_dir = Path("D:/Distr/Work/Codex/Erlang.Net/w-docs/tools/Validation/Erlang.CiReuse")
sys.path.insert(0, str(source_dir))
import collector
import test_lifecycle


class ScenarioRejected(Exception):
    pass


def scenario(kind):
    observed = []
    original_collect = collector.collect

    def review_collect(root, fingerprint, job):
        if os.environ.get("GITHUB_EVENT_NAME") != "push" or observed:
            return original_collect(root, fingerprint, job)

        api_type = collector.GitHub
        original_pages, original_get = api_type.pages, api_type.get
        state = {"ZipDownloaded": False, "CurrentListReads": 0}

        def pages(api, suffix, key=None):
            rows = original_pages(api, suffix, key)
            if suffix == "actions/runs?head_sha=merge":
                state["CurrentListReads"] += 1
                # Keep the consumer's ID distinct from producer run100.
                rows = [{**row, "id": 900, "run_attempt": 1,
                         "event": "push", "head_sha": "merge"} for row in rows]
                if state["ZipDownloaded"] and kind in ("late-incomplete-sibling", "late-failed-sibling"):
                    failed = kind == "late-failed-sibling"
                    rows += [dict(id=901, path=".github/workflows/native-gc825.yml",
                                  run_attempt=1, event="push", head_sha="merge",
                                  status="completed" if failed else "in_progress",
                                  conclusion="failure" if failed else None)]
            return rows

        def get(api, suffix, binary=False):
            value = original_get(api, suffix, binary)
            if suffix == "actions/artifacts/99/zip":
                state["ZipDownloaded"] = True
                if kind == "late-producer-attempt2":
                    original_get(api, "actions/runs/100")["run_attempt"] = 2
            elif (state["ZipDownloaded"] and kind == "late-concrete-attempt2"
                  and suffix == "actions/runs/100"):
                # Simulate list endpoint lag: only the concrete run has advanced.
                value = {**value, "run_attempt": 2}
            return value

        with patch.dict(os.environ, {"GITHUB_RUN_ID": "900"}), \
                patch.object(api_type, "pages", pages), patch.object(api_type, "get", get):
            try:
                result = original_collect(root, fingerprint, job)
            except ValueError as error:
                observed.append({"Scenario": kind, **state, "Rejected": True, "Reason": str(error)})
                raise ScenarioRejected from error
        observed.append({"Scenario": kind, **state, "Rejected": False, "Reuse": result["Reuse"]})
        return result

    try:
        with patch.object(collector, "collect", review_collect):
            case = test_lifecycle.WorkflowLifecycleTests("test_prepare_seal_and_verified_collector_reuse")
            case.test_prepare_seal_and_verified_collector_reuse()
    except ScenarioRejected:
        pass

    assert len(observed) == 1, observed
    result = observed[0]
    assert result["ZipDownloaded"] is True, result
    if kind == "unchanged-positive":
        assert result["Rejected"] is False and result["Reuse"] is True, result
        assert result["CurrentListReads"] == 2, result
    else:
        assert result["Rejected"] is True, result
    return result


cases = [scenario(kind) for kind in (
    "unchanged-positive", "late-incomplete-sibling", "late-failed-sibling",
    "late-producer-attempt2", "late-concrete-attempt2",
)]
report = {
    "SourceHead": "1a85b50ffb9e14c9d17fa3a0ddec7110af15218d",
    "CollectorSha256": hashlib.sha256((source_dir / "collector.py").read_bytes()).hexdigest(),
    "Cases": cases,
    "Passed": len(cases),
    "ActualGitHubRunsStarted": 0,
    "ProductionSourceChanged": False,
    "Scope": "Independent mocked lifecycle review only; no formal approval or live hosted acceptance",
}
Path(__file__).with_suffix(".json").write_text(json.dumps(report, indent=2) + "\n", encoding="utf-8")
print(json.dumps(report, indent=2))
