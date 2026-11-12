"""Inventory the unchanged pinned timer source and BEAM imports, without executing tests."""
from pathlib import Path
import hashlib
import json
import re
import struct
import subprocess
import sys
import timer_validation_contract as contract

root = Path(__file__).resolve().parents[2]
reference = Path(sys.argv[2]).resolve() if len(sys.argv) > 2 else root.parent / "otp"
source = subprocess.check_output([
    "git", "-C", str(reference), "show",
    f"{contract.PINNED_TAG}:{contract.PINNED_SOURCE}"])
retained = root / contract.BEAM_MODULE_PATH
assert source == (retained / "timer.erl").read_bytes()
beam = (retained / "timer.beam").read_bytes()
chunks = {}
offset = contract.BEAM_FORM_HEADER_BYTES
while offset < len(beam):
    name = beam[offset:offset + contract.CHUNK_NAME_BYTES].decode("ascii")
    size = struct.unpack_from(contract.UINT32_FORMAT, beam, offset + contract.CHUNK_NAME_BYTES)[0]
    chunks[name] = beam[offset + contract.CHUNK_HEADER_BYTES:offset + contract.CHUNK_HEADER_BYTES + size]
    offset += contract.CHUNK_HEADER_BYTES + (size + contract.CHUNK_ALIGNMENT_BYTES - 1) // contract.CHUNK_ALIGNMENT_BYTES * contract.CHUNK_ALIGNMENT_BYTES
atoms = [None]
atom_chunk = chunks.get(contract.UTF8_ATOMS_CHUNK, chunks.get(contract.LEGACY_ATOMS_CHUNK))
position = contract.COUNT_BYTES
count = struct.unpack_from(contract.INT32_FORMAT, atom_chunk)[0]
for _ in range(abs(count)):
    first = atom_chunk[position]
    position += 1
    if count >= 0:
        size = first
    elif first & contract.COMPACT_LARGE_FLAG == 0:
        size = first >> contract.COMPACT_SMALL_SHIFT
    elif first & contract.COMPACT_LONG_FLAG == 0:
        size = ((first & contract.COMPACT_HIGH_VALUE_MASK) << contract.COMPACT_MEDIUM_SHIFT) | atom_chunk[position]
        position += 1
    else:
        width = (first >> contract.COMPACT_WIDTH_SHIFT) + contract.COMPACT_MIN_LONG_BYTES
        assert width <= contract.COMPACT_MAX_SUPPORTED_LONG_BYTES
        size = int.from_bytes(atom_chunk[position:position + width], "big")
        position += width
    atoms.append(atom_chunk[position:position + size].decode("utf-8"))
    position += size
imports = chunks[contract.IMPORTS_CHUNK]
rows = []
for index in range(struct.unpack_from(contract.UINT32_FORMAT, imports)[0]):
    module, function, arity = struct.unpack_from(contract.IMPORT_FORMAT, imports, contract.COUNT_BYTES + index * contract.IMPORT_ENTRY_BYTES)
    rows.append({"module": atoms[module], "function": atoms[function], "arity": arity})
text = source.decode("utf-8")
exports = []
for declaration in re.findall(r"-export\(\[(.*?)\]\)\.", text, re.S):
    exports.extend({"function": name, "arity": int(arity)}
                   for name, arity in re.findall(r"(\w+)/(\d+)", declaration))
result = {
    "baseline": contract.PINNED_TAG, "commit": contract.PINNED_COMMIT,
    "sourceSha256": hashlib.sha256(source).hexdigest(),
    "beamSha256": hashlib.sha256(beam).hexdigest(),
    "sourceUnchanged": True, "exports": exports, "imports": rows,
    "clauses": [{"line": n, "text": line} for n, line in enumerate(text.splitlines(), 1)
                if re.match(r"^[a-z][a-z_]*\(", line)]}
output = Path(sys.argv[1])
output.parent.mkdir(parents=True, exist_ok=True)
output.write_text(json.dumps(result, indent=2) + "\n", encoding="utf-8")
print(f"timer: {len(exports)} exports, {len(rows)} imports; pinned source unchanged")
