from pathlib import Path
import re
import hashlib
import tempfile
import json
import unittest

from source_bindings import powershell_pin, qualified_inputs, require_producer_fields, verify


class IntegratedBindingsTests(unittest.TestCase):
    def test_known_producer_roles_cannot_drop_required_fields(self):
        root = Path(__file__).resolve().parents[3]
        pins = json.loads((root / "tools/Native/Erlang.NifIntegratedObservations/inputs.json").read_bytes())["PlanPins"]
        roles = {
            "N2_ORIGINAL_PLAN": ["NativeBuildPlanSha256", "WrongEntryNativeBuildPlans"],
            "N2_MANAGED_PLAN": ["NativeBuildPlanSha256", "OriginalPlanSha256", "WrongEntryNativeBuildPlans"],
            "ENV709_ORIGINAL_PLAN": ["NativeBuildPlanSha256"],
            "ENV709_MANAGED_PLAN": ["NativeBuildPlanSha256", "OriginalPlanSha256"],
        }
        for role, fields in roles.items():
            actual = json.loads((root / pins[role]["Path"]).read_bytes())
            for field in fields:
                changed = dict(actual)
                changed.pop(field)
                with self.assertRaisesRegex(ValueError, "Required producer fields missing"):
                    require_producer_fields(role, changed)

    def test_stale_powershell_caller_is_rejected_before_observer_execution(self):
        root = Path(__file__).resolve().parents[3]
        text = (root / "tools/validate-async-microstates.ps1").read_text()
        changed = re.sub(r"(?<=')([0-9A-F]{64})(?=')", "0" * 64, text)
        with self.assertRaisesRegex(ValueError, "PowerShell async"):
            powershell_pin(root, changed)

    def test_packet_attribute_never_resolves_to_repository_attribute(self):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            packet = root / "packet"
            packet.mkdir()
            (root / ".gitattributes").write_bytes(b"repository attributes")
            (packet / ".gitattributes").write_bytes(b"packet attributes")
            row = dict(Root="Packet", Path=".gitattributes",
                       Sha256=hashlib.sha256(b"repository attributes").hexdigest().upper())
            with self.assertRaisesRegex(ValueError, "Qualified source bytes differ"):
                qualified_inputs(root, packet, [row])
            row["Sha256"] = hashlib.sha256(b"packet attributes").hexdigest().upper()
            qualified_inputs(root, packet, [row])

    def test_actual_committed_workflow_matches_all_dependency_bytes(self):
        root = Path(__file__).resolve().parents[3]
        result = verify(root)
        self.assertEqual(result["PlanPins"], 11)
        self.assertEqual(result["DriverSources"], 6)
        self.assertFalse(result["BuildOrVmExecuted"])

    def test_stale_or_missing_environment_pin_is_rejected_before_execution(self):
        root = Path(__file__).resolve().parents[3]
        text = (root / ".github/workflows/nif-upgrade-integrated.yml").read_text()
        for key in ["ENV709_NATIVE_PLAN", "ENV709_ORIGINAL_PLAN", "ENV709_MANAGED_PLAN"]:
            for replacement in ["      " + key + ": " + "0" * 64, ""]:
                changed = re.sub(r"(?m)^      " + key + r": [0-9A-F]{64}$", replacement, text)
                with self.assertRaisesRegex(ValueError, key):
                    verify(root, changed)
