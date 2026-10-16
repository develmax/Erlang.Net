"""Root invokes after actual gates. Writes only a fresh artifact directory, never Main."""
from pathlib import Path
import argparse
from gates327 import ROOT, HERE, sha, read, encoded, arguments, observed, readiness

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
assert note.startswith('Part 327:')
entry = None
if args.phase == 'final':
    ready = readiness(args, fp.parent)
    entry = (HERE / 'english-entry.md').read_text(encoding='utf-8')
    old = '**Part status:** source-prepared finalization template. Final Main/full/native/readiness authority remains pending; this draft is not publication authority.'
    assert entry.count(old) == 1
    entry = entry.replace(old, '**Part status:** bounded registered undefined-function handler implementation and actual Main/full/native/readiness validation completed; current original327 and whole erlang compatibility remain partial.')
    start = entry.index('### Final gates — pending')
    end = entry.index('### Previous-head outcomes and next work', start)
    actual = """### Actual final validation

Actual Main passed **6729/6729** tests and all eleven integration/package checks outside the sandbox. All6708 previous ordered names remain intact; twenty appended ordinary contracts and one HOST registration account for the increase. All30 prior custom MFA limits were restored; only process_flag/2 gains the bounded handler scope and the other29 custom limits plus every unrelated row remain exact. Registry509 / previous509 / delta0 and351erlang are API presence, not semantic completion.

Three fresh post-full trusted Windows SDK processes separately passed legacy callbacks, five-phase same-runtime requested/backing query and three reallocation observations. They do not execute original Erlang or compare physical memory. Actual readiness CLI exited0 comparing immutable326. Main380AF, author464C and closure57275A bind22 owned paths/seventeen C# files/2379 complete inputs; native input counts1114/1139 and canonical LF/workflow pins preserve all ABI/import/profile policy.

Actual commands/reports are tools/validate.ps1, the reviewed Root post-full native runner and the separately recorded readiness CLI. See [tests](../validation/part-327/final-main/tests.json), [full guard](../validation/part-327/final-main/full-guard.json), [native guard](../validation/part-327/final-native/guard.json), [readiness](../validation/part-327/module-readiness.json) and [receipt](../validation/part-327/evidence-receipt.json). Current original327 execution remains pending; planned5925 ordinary plus fd8 is not an observed result.

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
