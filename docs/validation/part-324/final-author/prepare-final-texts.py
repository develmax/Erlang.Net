"""Root invokes after actual gates. Writes only a fresh artifact directory, never Main."""
from pathlib import Path
import argparse
from gates324 import ROOT, HERE, sha, read, encoded, arguments, observed, readiness

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
assert note.startswith('Part 324:')
entry = None
if args.phase == 'final':
    ready = readiness(args, fp.parent)
    entry = (HERE / 'english-entry.md').read_text(encoding='utf-8')
    old = '**Part status:** isolated implementation and focused SDK validation completed; final Main/full/native/readiness gates and original execution remain pending. This draft is not final publication authority.'
    assert entry.count(old) == 1
    entry = entry.replace(old, '**Part status:** bounded implementation and actual Main/full/native/readiness validation completed; whole erlang compatibility and current original execution remain partial.')
    start = entry.index('### Final gates — pending')
    end = entry.index('### Historical remote checkpoint', start)
    actual = '''### Actual final validation

Actual final Main passed **6692/6692** tests and all eleven integration/package checks outside the known native named-pipe sandbox restriction. All6685 previous ordered names remain intact. Three fresh separate post-full trusted Windows roles passed; these check the existing legacy/query/reallocation callbacks, not original Erlang or physical memory. Actual readiness CLI exit0 compares immutable323 and reports509 current/509 previous/delta0, including351 erlang exports. API presence is not semantic completion.

The final source has13 owned paths/eight C# files/2353 complete inputs. Both native source plans and workflow pins are bound to the actual source authority; import/ABI policies remain unchanged. See [tests](../validation/part-324/final-main/tests.json), [full guard](../validation/part-324/final-main/full-guard.json), [native guard](../validation/part-324/final-native/guard.json), [readiness](../validation/part-324/module-readiness.json) and [receipt](../validation/part-324/evidence-receipt.json). The current324 original ordinary run remains unexecuted; its planned5891 plus eight independent fd observations are not observed results. The optional historical raw-badarg companion remains unexecuted; actual historical proof is the separately captured DefaultQuery assertion RED.

'''
    entry = entry[:start] + actual + entry[end:]
    start = entry.index('### Publication handoff')
    entry = entry[:start] + '''### Handoff

This part begins new active volume0050 and preserves closed0049 byte for byte, including its earlier published checkpoint. Exact new evidence is indexed under part324; historical terminal supplements preserve their distinct raw/semantic outcomes. Current original324 and the separate325 retained-LINK work continue after this bounded slice. Final publication authority is the actual evidence receipt, not a source-only draft or planned case count.
'''
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
