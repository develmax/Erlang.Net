"""Read-only offline review using the committed lifecycle's mocked API."""
import json
import os
from pathlib import Path
import sys
from unittest.mock import patch

sys.path.insert(0, "D:/Distr/Work/Codex/Erlang.Net/w-docs/tools/Validation/Erlang.CiReuse")
import collector
import test_lifecycle

observed = []
original_collect = collector.collect


def review_collect(root, fingerprint, job):
    if os.environ.get("GITHUB_EVENT_NAME") != "push" or observed:
        return original_collect(root, fingerprint, job)

    api_type = collector.GitHub
    original_pages = api_type.pages
    original_get = api_type.get
    state = {"late_sibling_exists": False, "main_run_list_reads": 0}

    def pages(api, suffix, key=None):
        rows = original_pages(api, suffix, key)
        if suffix == "actions/runs?head_sha=merge":
            state["main_run_list_reads"] += 1
            if state["late_sibling_exists"]:
                rows = rows + [dict(id=1011, path=".github/workflows/native-gc825.yml",
                                    status="in_progress", conclusion=None)]
        return rows

    def get(api, suffix, binary=False):
        value = original_get(api, suffix, binary)
        if suffix == "actions/artifacts/99/zip":
            # A sibling is dispatched while the receipt ZIP is downloaded.
            state["late_sibling_exists"] = True
        return value

    with patch.object(api_type, "pages", pages), patch.object(api_type, "get", get):
        result = original_collect(root, fingerprint, job)
    observed.append({**state, "Reuse": result["Reuse"], "Reasons": result["Reasons"]})
    return result


with patch.object(collector, "collect", review_collect):
    case = test_lifecycle.WorkflowLifecycleTests("test_prepare_seal_and_verified_collector_reuse")
    case.test_prepare_seal_and_verified_collector_reuse()

assert len(observed) == 1
assert observed[0]["late_sibling_exists"] is True
assert observed[0]["main_run_list_reads"] == 1
assert observed[0]["Reuse"] is True

report = {
    "SourceHead": "6c966ada688cf35f9202febf8029872015825671",
    "Scenario": "Mocked sibling starts during receipt download after initial main-run listing",
    "ActualGitHubRunsStarted": 0,
    "ProductionSourceChanged": False,
    "Observed": observed[0],
    "ExpectedForAdmissionTimeSnapshot": "Reject reuse when refreshed main-run listing contains the incomplete sibling",
    "SuggestedControl": "Re-fetch current merge and producer head runs immediately before admission; test a late incomplete or failed sibling and a late producer rerun",
}
output = Path(__file__).with_suffix(".json")
output.write_text(json.dumps(report, indent=2) + "\n", encoding="utf-8")
print(json.dumps(report, indent=2))
