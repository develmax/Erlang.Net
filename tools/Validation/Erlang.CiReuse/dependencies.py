"""Hash resolved NuGet assets and the package/compiler bytes actually selected."""
import hashlib
import json
from pathlib import Path
import xml.etree.ElementTree as ET

from policy import digest


def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest().upper()


def resolved(root):
    root = Path(root).resolve()
    assets = sorted(path for path in root.glob("**/obj/project.assets.json")
                    if "artifacts" not in path.relative_to(root).parts and ".git" not in path.parts)
    if not assets:
        raise ValueError("Resolved dependency assets are unavailable; restore is required")
    solution = root / "Erlang.Net.slnx"
    if solution.is_file():
        pending = [root / item.attrib["Path"] for item in ET.parse(solution).iter("Project")]
        projects = set()
        while pending:
            project = pending.pop().resolve()
            if project in projects:
                continue
            if not project.is_relative_to(root) or not project.is_file():
                raise ValueError("Solution project cannot be resolved")
            projects.add(project)
            for item in ET.parse(project).iter("ProjectReference"):
                include = item.attrib.get("Include", "")
                if not include or "$" in include or "*" in include:
                    raise ValueError("Dynamic project graph cannot be safely fingerprinted")
                pending.append(project.parent / include.replace("\\", "/"))
        expected = {project.parent / "obj/project.assets.json" for project in projects}
        if not expected.issubset(set(assets)):
            raise ValueError("Resolved solution graph is incomplete; restore every project")
        assets = sorted(expected)
    records = []
    for path in assets:
        value = json.loads(path.read_bytes())
        folders = [Path(name) for name in value.get("packageFolders", {})]
        packages = []
        for name, entry in value.get("libraries", {}).items():
            if entry.get("type") != "package":
                continue
            relative = entry["path"]
            matches = [folder / relative for folder in folders if (folder / relative).is_dir()]
            if len(matches) != 1:
                raise ValueError("Resolved package content unavailable or ambiguous: " + name)
            files = []
            for file in entry.get("files", []):
                target = (matches[0] / file).resolve()
                if not target.is_relative_to(matches[0].resolve()) or not target.is_file() or target.is_symlink():
                    raise ValueError("Resolved package member differs: " + name)
                files.append({"Path": file, "Sha256": sha(target)})
            if not files:
                raise ValueError("Resolved package has no content inventory: " + name)
            packages.append({"Library": name, "Files": files})
        records.append({"Project": path.relative_to(root).as_posix(), "AssetsSha256": sha(path),
                        "Packages": packages})
    return {"Projects": len(records), "ResolvedAssetsAndContent": digest(records)}
