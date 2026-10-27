"""Identify actual preloaded erlang bytes inside the admitted runtime core image."""
import hashlib
import json
from pathlib import Path
PIN = 'ad05823719d77c8faee87348ea39513d4e2f99c5'
PINNED_PRELOAD_SHA = '824E0B5DC24AD62198B3CC83A8F92E15732F29A8844C36F3D3DA9F5B8496AA64'

def digest(data):
    return hashlib.sha256(data).hexdigest().upper()

def module_name(data):
    chunks = {}
    offset = 12
    if data[:4] != b'FOR1' or data[8:12] != b'BEAM' or int.from_bytes(data[4:8], 'big') + 8 != len(data):
        return None
    while offset < len(data):
        if offset + 8 > len(data):
            return None
        name = data[offset:offset + 4]
        size = int.from_bytes(data[offset + 4:offset + 8], 'big')
        total = 8 + (size + 3) // 4 * 4
        if offset + total > len(data) or name in chunks:
            return None
        chunks[name] = data[offset + 8:offset + 8 + size]
        offset += total
    if not all((key in chunks for key in (b'AtU8', b'Code', b'ImpT', b'ExpT'))):
        return None
    atoms = chunks[b'AtU8']
    if len(atoms) < 5:
        return None
    count = int.from_bytes(atoms[:4], 'big', signed=True)
    position = 4
    if not count:
        return None
    first = atoms[position]
    position += 1
    if count > 0:
        length = first
    elif not first & 8:
        length = first >> 4
    elif not first & 16:
        if position >= len(atoms):
            return None
        length = first >> 5 << 8 | atoms[position]
        position += 1
    else:
        return None
    if position + length > len(atoms):
        return None
    return atoms[position:position + length]

def acquire(installed, output):
    installed = Path(installed)
    output = Path(output)
    sources = list(installed.glob('lib/erts-*/ebin/erlang.beam'))
    if len(sources) != 1:
        raise ValueError('Unique provider erlang metadata source required')
    raw = sources[0].read_bytes()
    matches = []
    for core in sorted(installed.glob('erts-*/bin/beam.smp*')):
        if not core.is_file() or core.suffix.lower() in ('.pdb', '.debug'):
            continue
        image = core.read_bytes()
        position = 0
        while True:
            position = image.find(b'FOR1', position)
            if position < 0:
                break
            size = int.from_bytes(image[position + 4:position + 8], 'big') + 8
            if 12 <= size <= 16 * 1024 * 1024 and position + size <= len(image):
                candidate = image[position:position + size]
                if module_name(candidate) == b'erlang':
                    matches.append((core, image, candidate, position))
            position += 4
    if len(matches) != 1:
        raise ValueError('Exactly one actual embedded erlang image required; got ' + str(len(matches)))
    core, image, effective, position = matches[0]
    source_path = output / 'metadata-source-erlang.beam'
    source_path.write_bytes(raw)
    effective_path = output / 'metadata-effective-erlang.beam'
    effective_path.write_bytes(effective)
    proof = {
        'Pin': PIN,
        'SourcePath': str(sources[0]),
        'SourceSha256': digest(raw),
        'SourceCopy': source_path.name,
        'RuntimeBinaryPath': str(core),
        'RuntimeBinarySha256': digest(image),
        'EmbeddedOffset': position,
        'EmbeddedLength': len(effective),
        'EffectiveImageSha256': digest(effective),
        'EffectiveImage': effective_path.name,
        'EmbeddingPolicy': 'ActualRuntimeEmbeddedImage',
        'PinnedPreloadGeneratorSha256': PINNED_PRELOAD_SHA,
        'PinnedPreloadGeneratorGitPath': 'erts/emulator/utils/make_preload',
        'OriginalOutcomeRead': False,
        'RunningModuleMd5Witness': 'native-metadata-executing.etf',
    }
    proof_path = output / 'native-metadata-identity.json'
    proof_path.write_text(json.dumps(proof, indent=2) + '\n', encoding='utf-8')
    return {
        'Module': 'erlang',
        'Name': effective_path.name,
        'Sha256': digest(effective),
        'IdentityName': proof_path.name,
        'IdentitySha256': digest(proof_path.read_bytes()),
    }
