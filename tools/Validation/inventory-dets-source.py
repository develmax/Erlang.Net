"""Inventory pinned DETS source/BEAM; does not execute or test either backend."""
import argparse
import hashlib
import json
import pathlib
import re
import struct
import subprocess


def digest(data):
    return hashlib.sha256(data).hexdigest()


def beam_chunks(data):
    if data[:4] != b"FOR1" or data[8:12] != b"BEAM":
        raise ValueError("Not a BEAM container")
    chunks = {}
    offset = 12
    while offset < len(data):
        name = data[offset:offset + 4].decode("ascii")
        size = struct.unpack_from(">I", data, offset + 4)[0]
        chunks[name] = data[offset + 8:offset + 8 + size]
        offset += 8 + ((size + 3) & ~3)
    return chunks


def beam_imports(data):
    chunks = beam_chunks(data)
    atom_chunk = chunks.get("AtU8", chunks.get("Atom"))
    if atom_chunk is None:
        raise ValueError("BEAM atom table missing")
    atoms = [None]
    offset = 4
    count = struct.unpack_from(">i", atom_chunk)[0]
    for _ in range(abs(count)):
        first = atom_chunk[offset]
        offset += 1
        if count >= 0:
            size = first
        elif not first & 8:
            size = first >> 4
        elif not first & 16:
            size = ((first >> 5) << 8) | atom_chunk[offset]
            offset += 1
        else:
            width = (first >> 5) + 2
            if width == 9:
                raise ValueError("Unexpected extended atom length")
            size = int.from_bytes(atom_chunk[offset:offset + width], "big")
            offset += width
        atoms.append(atom_chunk[offset:offset + size].decode("utf-8" if "AtU8" in chunks else "latin-1"))
        offset += size
    imports = chunks["ImpT"]
    return [
        {"module": atoms[module], "function": atoms[function], "arity": arity}
        for module, function, arity in struct.iter_unpack(">III", imports[4:])
    ]


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--output", type=pathlib.Path, required=True)
    args = parser.parse_args()
    root = pathlib.Path(__file__).resolve().parents[2]
    baseline = json.loads((root / "reference-baseline.json").read_text(encoding="utf-8"))
    upstream = root.parent / "otp"
    resolved = subprocess.check_output([
        "git", "-C", str(upstream), "rev-parse", baseline["tag"] + "^{commit}"
    ]).decode("ascii").strip()
    if resolved != baseline["commit"]:
        raise ValueError("Pinned OTP tag does not resolve to the declared commit")
    packaged = root / "src/Otp/Original/Foundation/Erlang.Otp.Stdlib.Beam/Otp"
    modules = []
    for name in ["dets", "dets_server", "dets_sup", "dets_utils", "dets_v9"]:
        path = "lib/stdlib/src/" + name + ".erl"
        original = subprocess.check_output(["git", "-C", str(upstream), "show", baseline["tag"] + ":" + path])
        source = original.decode("utf-8")
        exports = [
            {"function": function, "arity": int(arity)}
            for group in re.findall(r"-export\(\[(.*?)\]\)\.", source, re.S)
            for function, arity in re.findall(r"([a-z][a-z_0-9]*)/([0-9]+)", group)
        ]
        beam = (packaged / (name + ".beam")).read_bytes()
        modules.append({
            "module": name,
            "sourcePath": path,
            "sourceLines": len(source.splitlines()),
            "sourceSha256": digest(original),
            "packagedSourceSha256": digest((packaged / (name + ".erl")).read_bytes()),
            "beamSha256": digest(beam),
            "exports": exports,
            "imports": beam_imports(beam),
        })
    result = {
        "baseline": baseline,
        "kind": "Static source and BEAM inventory; not execution evidence",
        "modules": modules,
        "managedSources": [
            {"path": str(path.relative_to(root)).replace("\\", "/"), "sha256": digest(path.read_bytes())}
            for path in sorted((root / "src/Otp/Managed/Erlang.Otp.Stdlib/Storage/Dets").glob("*.cs"))
        ],
        "acceptance": "Static inventory only; see separate execution receipts",
    }
    args.output.parent.mkdir(parents=True, exist_ok=True)
    args.output.write_text(json.dumps(result, indent=2) + "\n", encoding="utf-8")
    print(f"Inventoried {len(modules)} modules; no backend executed")


if __name__ == "__main__":
    main()
