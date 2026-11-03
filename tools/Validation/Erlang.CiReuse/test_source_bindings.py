from pathlib import Path
import re
import hashlib
import tempfile
import unittest

from source_bindings import qualified_inputs, verify


class IntegratedBindingsTests(unittest.TestCase):
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
