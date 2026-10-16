"""Root invokes after actual gates. Writes only a fresh artifact directory, never Main."""
from pathlib import Path
import argparse
from gates326 import ROOT, HERE, sha, read, encoded, arguments, observed, readiness

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
assert note.startswith('Part 326:')
entry = None
if args.phase == 'final':
    ready = readiness(args, fp.parent)
    entry = (HERE / 'english-entry.md').read_text(encoding='utf-8')
    old = '**Part status:** source-prepared finalization template; actual final Main/full/native/readiness authority remains pending. This draft is not final publication authority.'
    assert entry.count(old) == 1
    entry = entry.replace(old, '**Part status:** bounded explicit local spawn async_dist implementation and actual Main/full/native/readiness validation completed; current original326 and whole erlang compatibility remain partial.')
    start = entry.index('### Final gates — pending')
    end = entry.index('### Previous-head remote checkpoints', start)
    actual = """### Actual final validation

Actual Main passed **6708/6708** tests and all eleven integration/package checks outside the known native named-pipe sandbox restriction. All6693 previous ordered names remain intact; fourteen appended ordinary fixtures and one new HOST registration account for the increase. All22 prior custom MFA limitations remain exact; only the eight option-bearing spawn_opt/2–5 and spawn_request/2–5 rows gain the bounded new scope. spawn_request/1 and every unrelated registry row remain exact. Registry509 / previous509 / delta0 and351erlang is API presence, not semantic completion.

Three fresh post-full trusted Windows processes passed legacy callbacks, five-phase same-runtime SDK-private requested/backing query and three reallocation observations. They do not execute original Erlang or measure physical memory. Actual readiness CLI exited0 comparing immutable325. Main371B, author98F6 and closure098DD bind13 owned paths/eight C# files/2370 complete inputs, unchanged native input identities/counts1109/1134 and canonical LF/workflow pins A673/5350 with unchanged ABI/import policy.

See [tests](../validation/part-326/final-main/tests.json), [full guard](../validation/part-326/final-main/full-guard.json), [native guard](../validation/part-326/final-native/guard.json), [readiness](../validation/part-326/module-readiness.json) and [receipt](../validation/part-326/evidence-receipt.json). Current original326 execution remains pending; planned5905 ordinary plus fd8 is not an observed result. The optional generated historical companion remains unexecuted.

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
