import copy
import unittest

from policy import EVENTS, FINGERPRINT_FIELDS, REQUIRED, admit


def fixture():
    fingerprint = {
        "Tree": "tree", "WorkflowCheckerSources": "source-digest", "Dependencies": "locked-digest",
        "Configuration": "config-digest", "Provider": "verified-6427-file-digest",
        "Platform": {"OS": "Windows", "Architecture": "X64", "ImageVersion": "20261103.1"},
        "Toolchain": {"Sdk": "10.0.400", "Runtimes": ["10.0.11"]},
    }
    context = dict(event="push", ref="refs/heads/main", repository="owner/repo", commit="merge",
                   parents=["base", "head"], base_is_ancestor_of_pr=True, tree="tree", job="local-regressions",
                   fingerprint=fingerprint, fingerprint_complete=True)
    evidence = dict(pull_request=dict(repository="owner/repo", head_repository="owner/repo", merged=True,
                    base_ref="main", merge_commit="merge", head="head"), tested_tree="tree",
                    fingerprint=copy.deepcopy(fingerprint), fingerprint_complete=True,
                    artifact_verified=True, artifact_head="head", artifact_repository="owner/repo",
                    artifact_run=100, receipt_run=100, receipt_job="local-regressions", receipt_completed=True,
                    review_preserved=True, workflows=[])
    for path, names in REQUIRED.items():
        evidence["workflows"].append(dict(path=path, repository="owner/repo", event=sorted(EVENTS[path])[0], head="head",
            status="completed", conclusion="success", latest_attempt_verified=True,
            jobs=[dict(name=name, status="completed", conclusion="success", steps_complete=True, failed_steps=[]) for name in sorted(names)]))
    return context, evidence


class AdmissionTests(unittest.TestCase):
    def test_matching_missing_or_false_declared_fingerprints_are_rejected(self):
        for value in [None, {}, {"Tree": "tree"}]:
            context, evidence = fixture()
            context["fingerprint"] = value; evidence["fingerprint"] = copy.deepcopy(value)
            self.assertFalse(admit(context, evidence)["Reuse"])
        for name in FINGERPRINT_FIELDS:
            context, evidence = fixture()
            context["fingerprint"].pop(name); evidence["fingerprint"].pop(name)
            self.assertFalse(admit(context, evidence)["Reuse"])
        context, evidence = fixture()
        context["fingerprint"]["Tree"] = "false-declared-tree"
        evidence["fingerprint"]["Tree"] = "false-declared-tree"
        self.assertFalse(admit(context, evidence)["Reuse"])

    def test_manual_integrated_matrix_is_required(self):
        context, evidence = fixture()
        path = ".github/workflows/nif-upgrade-integrated.yml"
        evidence["workflows"] = [run for run in evidence["workflows"] if run["path"] != path]
        self.assertFalse(admit(context, evidence)["Reuse"])

    def test_exact_proven_equivalence(self):
        context, evidence = fixture()
        result = admit(context, evidence)
        self.assertTrue(result["Reuse"], result)
        self.assertFalse(result["FormalApprovalCreated"])
        self.assertFalse(result["ProtectionChanged"])

    def test_every_input_mismatch_fails_closed(self):
        for name in ["Tree", "WorkflowCheckerSources", "Dependencies", "Configuration", "Provider", "Platform", "Toolchain"]:
            with self.subTest(name=name):
                context, evidence = fixture()
                evidence["fingerprint"][name] = "different"
                self.assertFalse(admit(context, evidence)["Reuse"])

    def test_changed_main_and_tree(self):
        for field, value in [("tree", "new-tree"), ("base_is_ancestor_of_pr", False), ("parents", ["different", "other"])]:
            context, evidence = fixture(); context[field] = value
            self.assertFalse(admit(context, evidence)["Reuse"])

    def test_missing_failed_timedout_or_untrusted_evidence(self):
        for field in ["artifact_verified", "fingerprint_complete", "receipt_completed", "review_preserved"]:
            context, evidence = fixture(); evidence[field] = False
            self.assertFalse(admit(context, evidence)["Reuse"])
        for state in ["failure", "cancelled", "timed_out", "action_required", None]:
            context, evidence = fixture(); evidence["workflows"][0]["jobs"][0]["conclusion"] = state
            self.assertFalse(admit(context, evidence)["Reuse"])
        context, evidence = fixture(); evidence["workflows"][0]["jobs"][0]["failed_steps"] = [13]
        self.assertFalse(admit(context, evidence)["Reuse"])
        context, evidence = fixture(); evidence["workflows"].pop()
        self.assertFalse(admit(context, evidence)["Reuse"])
        context, evidence = fixture(); evidence["pull_request"]["head_repository"] = "fork/repo"
        self.assertFalse(admit(context, evidence)["Reuse"])

    def test_artifact_provenance_and_attempt(self):
        for field, value in [("artifact_head", "stale"), ("artifact_repository", "attacker/repo"),
                             ("artifact_run", 101), ("receipt_job", "other")]:
            context, evidence = fixture(); evidence[field] = value
            self.assertFalse(admit(context, evidence)["Reuse"])
        context, evidence = fixture(); evidence["workflows"][0]["latest_attempt_verified"] = False
        self.assertFalse(admit(context, evidence)["Reuse"])


if __name__ == "__main__":
    unittest.main()
