import json
from pathlib import Path
import tempfile
import unittest

from dependencies import resolved


class DependencyTests(unittest.TestCase):
    def test_content_change_without_declared_tree_change_is_detected(self):
        with tempfile.TemporaryDirectory() as folder:
            root = Path(folder); package = root / "packages/x/1.0"; package.mkdir(parents=True)
            binary = package / "x.dll"; binary.write_bytes(b"first")
            assets = root / "app/obj/project.assets.json"; assets.parent.mkdir(parents=True)
            assets.write_text(json.dumps({"packageFolders": {str(root / "packages"): {}},
                "libraries": {"x/1.0": {"type": "package", "path": "x/1.0", "files": ["x.dll"]}}}))
            first = resolved(root)
            binary.write_bytes(b"changed")
            self.assertNotEqual(first, resolved(root))

    def test_missing_assets_or_package_never_becomes_empty_success(self):
        with tempfile.TemporaryDirectory() as folder:
            with self.assertRaises(ValueError):
                resolved(folder)

    def test_partially_restored_solution_is_rejected(self):
        with tempfile.TemporaryDirectory() as folder:
            root = Path(folder)
            (root / "Erlang.Net.slnx").write_text('<Solution><Project Path="a/a.csproj"/><Project Path="b/b.csproj"/></Solution>')
            for name in ["a", "b"]:
                (root / name / "obj").mkdir(parents=True)
                (root / name / (name + ".csproj")).write_text("<Project />")
            (root / "a/obj/project.assets.json").write_text('{"libraries":{}}')
            with self.assertRaisesRegex(ValueError, "incomplete"):
                resolved(root)


if __name__ == "__main__":
    unittest.main()
