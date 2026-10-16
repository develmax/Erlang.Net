"""Root invokes after actual gates. Writes only a fresh artifact directory, never Main."""
from pathlib import Path
import argparse
from gates325 import ROOT, HERE, sha, read, encoded, arguments, observed, readiness

parser = argparse.ArgumentParser()
parser.add_argument('phase', choices=['scope', 'final'])
arguments(parser)
parser.add_argument('--output', required=True, type=Path)
parser.add_argument('--readiness-sha256')
parser.add_argument('--readiness-exit-sha256')
args = parser.parse_args()
cfg, fp, closure, fullpath, nativepath, _ = observed(args)
out = args.output.resolve()
assert out.is_relative_to(ROOT / 'artifacts') and not out.exists()
note = (HERE / 'runtime-note-template.txt').read_text(encoding='utf-8').strip()
assert note.startswith('Part 325:')
entry = None
if args.phase == 'final':
    ready = readiness(args, fp.parent)
    entry = (HERE / 'english-entry.md').read_text(encoding='utf-8')
    old = '**Part status:** source-prepared finalization; actual final Main/full/native/readiness gates remain pending. This draft is not final publication authority.'
    assert entry.count(old) == 1
    entry = entry.replace(old, '**Part status:** bounded retained-LINK implementation and actual Main/full/native/readiness validation completed; current original325 execution and whole erlang compatibility remain partial.')
    start = entry.index('### Final gates — pending')
    end = entry.index('### Historical remote checkpoint', start)
    actual = """### Actual final validation

Actual Main passed **6693/6693** tests and all eleven integration/package checks outside the known native named-pipe sandbox restriction. Every6692 previous ordered test remains present. All22 earlier custom MFA limitations were restored before updating only link/1, link/2 and the obsolete325 follow-up phrase in process_info/2. Unlink/1, process_flag/2, process_flag/3, process_info/1 and every unrelated registry row remain exactly the published parent values. Registry509 / previous509 / delta0 and351erlang is API presence, not semantic completion.

Three fresh separate post-full trusted Windows roles passed: legacy callbacks, five-phase live-runtime SDK-private allocation query, and three reallocation observations. These do not execute original Erlang or measure physical memory. The actual readiness CLI exited0 against immutable324. Final source authority binds23 owned paths/seven C# files/2366 complete inputs,1109 legacy and1134 reallocation inputs with exact canonical LF/workflow pins and unchanged import/ABI policies.

See [tests](../validation/part-325/final-main/tests.json), [full guard](../validation/part-325/final-main/full-guard.json), [native guard](../validation/part-325/final-native/guard.json), [readiness](../validation/part-325/module-readiness.json) and [receipt](../validation/part-325/evidence-receipt.json). The new original325 normal/async retention scenarios remain unexecuted; future source-plan/VM/compiler attestations are not observed outcomes.

"""
    entry = entry[:start] + actual + entry[end:]
    assert '### Final gates — pending' not in entry and 'This draft is not final publication authority' not in entry
out.mkdir()
(out / 'runtime-note.txt').write_bytes((note + '\n').encode('utf-8'))
if entry is not None:
    (out / 'english-entry.md').write_bytes((entry.rstrip() + '\n').encode('utf-8'))
(out / 'text-bindings.json').write_bytes(encoded({
    'Phase': args.phase, 'MainFreezeSha256': sha(fp.read_bytes()), 'FullGuardSha256': sha(fullpath.read_bytes()),
    'NativeGuardSha256': sha(nativepath.read_bytes()), 'ReadinessSha256': args.readiness_sha256,
    'RuntimeNoteSha256': sha((out / 'runtime-note.txt').read_bytes()),
    'EnglishEntrySha256': sha((out / 'english-entry.md').read_bytes()) if entry is not None else None,
    'PreparerSha256': sha(Path(__file__).read_bytes()), 'MainEdited': False, 'OriginalExecuted': False}))
print(str(out))
