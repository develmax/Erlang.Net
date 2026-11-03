"""Capture actual local validation inputs; uncertainty disables reuse."""
import hashlib
import json
import os
from pathlib import Path
import platform
import subprocess

from policy import REQUIRED, digest
from dependencies import resolved


def command(*args):
    return subprocess.check_output(args, text=True, stderr=subprocess.STDOUT).strip()


def file_sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest().upper()


def capture(repository, provider_admission):
    root = Path(repository).resolve()
    tree = command("git", "-C", str(root), "rev-parse", "HEAD^{tree}")
    changed = command("git", "-C", str(root), "diff", "--name-only", "HEAD").splitlines()
    generated = {"docs/MODULE_READINESS.md", "docs/module-readiness.json", "docs/API_COMPATIBILITY.md",
                 "docs/API_COMPATIBILITY_INDEX.md", "docs/native-infrastructure.md"}
    if any(name not in generated and not name.startswith("docs/api/") for name in changed):
        raise ValueError("Tracked validation input changed before validation fingerprint")
    image = {name: os.environ.get(name, "") for name in ["ImageOS", "ImageVersion", "RUNNER_OS", "RUNNER_ARCH"]}
    if not all(image.values()):
        raise ValueError("Hosted runner image/platform identity is incomplete")
    admission_path = Path(provider_admission).resolve()
    admission = json.loads(admission_path.read_bytes())
    if admission.get("SourceCommit") != "ad05823719d77c8faee87348ea39513d4e2f99c5":
        raise ValueError("Provider is not the pinned OTP commit")
    manifest_path = Path(admission["ExecutionManifestPath"])
    if file_sha(manifest_path) != admission["ExecutionManifestSha256"].upper():
        raise ValueError("Provider execution inventory hash differs")
    provider = Path(admission["InstalledOtpRoot"])
    inventory = json.loads(manifest_path.read_bytes())
    if not isinstance(inventory, list) or not inventory:
        raise ValueError("Provider execution inventory is empty")
    for row in inventory:
        path = (provider / row["Path"]).resolve()
        if not path.is_relative_to(provider.resolve()) or path.is_symlink() or file_sha(path) != row["Sha256"].upper():
            raise ValueError("Provider inventory differs: " + row["Path"])
    workflow_sources = {name: file_sha(root / name) for name in REQUIRED}
    workflow_sources.update({str(path.relative_to(root)).replace("\\", "/"): file_sha(path)
                             for path in (root / "tools/Validation/Erlang.CiReuse").glob("*.py")})
    config_names = ["global.json", "Directory.Build.props", "Directory.Build.targets", "NuGet.Config", "nuget.config"]
    configuration = {name: file_sha(root / name) for name in config_names if (root / name).is_file()}
    dependencies = resolved(root)
    toolchain = {
        "Sdk": command("dotnet", "--version"),
        "Sdks": command("dotnet", "--list-sdks"),
        "Runtimes": command("dotnet", "--list-runtimes"),
        "Python": platform.python_version(),
    }
    sdk_version = toolchain["Sdk"]
    sdk_lines = [line for line in toolchain["Sdks"].splitlines() if line.startswith(sdk_version + " [")]
    if len(sdk_lines) != 1:
        raise ValueError("Selected compiler SDK path is ambiguous")
    sdk = Path(sdk_lines[0].split("[", 1)[1].rsplit("]", 1)[0]) / sdk_version
    compiler_files = [sdk / "Roslyn/bincore/csc.dll", sdk / "Roslyn/bincore/Microsoft.CodeAnalysis.dll",
                      sdk / "Roslyn/bincore/Microsoft.CodeAnalysis.CSharp.dll"]
    if not all(path.is_file() for path in compiler_files):
        raise ValueError("Resolved compiler reference bytes are unavailable")
    toolchain["ResolvedCompilerFiles"] = {path.name: file_sha(path) for path in compiler_files}
    compiler = ["where.exe", "cl.exe"] if os.name == "nt" else ["cc", "--version"]
    try:
        toolchain["Compiler"] = command(*compiler)
        if os.name == "nt":
            files = {}
            for name in toolchain["Compiler"].splitlines():
                binary = Path(name)
                for member in [binary, binary.with_name("c1xx.dll"), binary.with_name("c2.dll"), binary.with_name("link.exe")]:
                    if not member.is_file():
                        raise ValueError("Selected native compiler member is unavailable")
                    files[str(member)] = file_sha(member)
            toolchain["NativeCompilerFiles"] = files
    except (OSError, subprocess.CalledProcessError):
        # Windows cl is normally outside PATH. Fingerprint the actual installed VS catalog instead.
        vswhere = Path(os.environ.get("ProgramFiles(x86)", "")) / "Microsoft Visual Studio/Installer/vswhere.exe"
        if os.name != "nt" or not vswhere.is_file():
            raise ValueError("Compiler/toolchain identity is unavailable")
        toolchain["Compiler"] = json.loads(command(str(vswhere), "-all", "-format", "json", "-utf8"))
        binaries = {}
        for installation in toolchain["Compiler"]:
            location = Path(installation["installationPath"])
            for binary in location.glob("VC/Tools/MSVC/*/bin/Hostx64/x64/*"):
                if binary.name.lower() in ("cl.exe", "link.exe", "c1xx.dll", "c2.dll"):
                    binaries[str(binary)] = file_sha(binary)
        if not binaries:
            raise ValueError("Native compiler executable bytes are unavailable")
        toolchain["NativeCompilerFiles"] = binaries
    return {
        "Tree": tree,
        "WorkflowCheckerSources": workflow_sources,
        "Dependencies": dependencies,
        "Configuration": configuration,
        "Provider": {"Commit": admission["SourceCommit"], "Files": len(inventory),
                     "Inventory": digest(sorted(inventory, key=lambda row: row["Path"]))},
        "Platform": image,
        "Toolchain": toolchain,
    }
