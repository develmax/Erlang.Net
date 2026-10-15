from pathlib import Path
import hashlib
import json
import subprocess
import zipfile

root = Path.cwd()
art = root / 'artifacts'
proposal = art / 'part313-minimal-finalizer-proposal1'
out = art / 'root-part313-finalization-bound'
sha = lambda b: hashlib.sha256(b).hexdigest().upper()
read = lambda p: json.loads(p.read_bytes())
row = lambda p: {'Path': p.relative_to(root).as_posix(), 'Bytes': p.stat().st_size,
                 'Sha256': sha(p.read_bytes())}
assert not out.exists()
assert sha((proposal / 'proposal-freeze.json').read_bytes()) == '63C3D57D1A68EE59966C806418341ACB8D1F75A89A219200EF786144D80958E7'
for member in read(proposal / 'proposal-freeze.json')['Members']:
    p = root / member['Path']
    assert row(p) == member, member['Path']
assert sha((art / 'part313-finalizer-beam310-source-review1.json').read_bytes()) == 'B3793EC338713A0F5B163B3F850ACDB6380B16623CA613A058F86A47DF03C1E8'
config = read(proposal / 'config-unbound.json')
assert config['BoundToActualFinalGates'] is False and config['Readiness'] is None
out.mkdir()
pin = 'ad05823719d77c8faee87348ea39513d4e2f99c5'
source_rows = []
for name, first, last in [('erts/emulator/beam/erl_nif.c', 614, 627),
                          ('erts/emulator/beam/erl_nif_api_funcs.h', 90, 99)]:
    raw = subprocess.check_output(['git', '--no-optional-locks', '-C', str(root.parent / 'otp'), 'show', pin + ':' + name])
    p = out / Path(name).name
    p.write_bytes(raw)
    excerpt = out / (Path(name).name + '.excerpt')
    excerpt.write_bytes(b''.join(raw.splitlines(keepends=True)[first - 1:last]))
    source_rows.append({'PinnedCommit': pin, 'GitPath': name, 'Lines': [first, last],
                        'FullSource': row(p), 'Excerpt': row(excerpt)})
guidance = {'Status': 'ACTUAL_READ_ONLY_PINNED_OTP_SOURCE_BINDING', 'PinnedTag': 'OTP-29.1.1',
            'PinnedCommit': pin, 'Sources': source_rows,
            'ReallocationDeclarationLine': 95, 'ReallocationImplementationLines': [619, 622],
            'ActualWindowsPinnedHeaderSlot': 58, 'SlotByteOffset': 464,
            'LayoutProof': row(art / 'root-part313-native-windows-layout1/guard.json'),
            'OriginalExecutedByThisBinding': False}
(out / 'pinned-source-guidance.json').write_text(json.dumps(guidance, indent=2) + '\n', encoding='utf-8')
attribution = ('Pinned source: OTP-29.1.1, commit ' + pin + ', '
               'erts/emulator/beam/erl_nif_api_funcs.h:95 declares enif_realloc; the actual Windows header helper '
               'measures slot58/offset464. erts/emulator/beam/erl_nif.c:619-622 delegates to '
               'erts_realloc_fnf(ERTS_ALC_T_NIF, ptr, size). The CLR adaptation uses NativeMemory.Realloc '
               'under the actual owner/domain ledger and a library-scoped capability; request/backing-request '
               'counters do not represent ERTS carriers or a CLR memory partition. Platform ABI adaptation '
               'remains limited to this reviewed profile, and general ABI support remains unfinished.')
note = (proposal / 'runtime-note.txt').read_text(encoding='utf-8-sig').strip() + ' ' + attribution
(out / 'runtime-note.txt').write_text(note + '\n', encoding='utf-8')
entry = (proposal / 'english-entry.md').read_text(encoding='utf-8-sig').strip()
entry += ('\n\n' + attribution + ' See the [exact source binding](../validation/part-313/final-controls/pinned-source-guidance.json).'
          '\n\nExecuted commands: `pwsh -NoProfile -File artifacts/root-run313-main-full.ps1` runs '
          '`./tools/validate.ps1`; `python artifacts/root-guard313-full.py` validates the full report. '
          '`python artifacts/root-run313-main-native.py` runs the old and new native modes in separate '
          'processes; `python artifacts/root-guard313-main-native.py` verifies their exact captured outputs. '
          'Python commands used the installed Codex runtime Python executable. See the '
          '[full guard](../validation/part-313/final-main/root-part313-main1/full-guard.json), '
          '[full raw log](../validation/part-313/final-main/root-part313-main1/full-validation.log), and '
          '[post-full native guard](../validation/part-313/final-main/root-part313-main-windows-native1/guard.json). '
          'Current-head remote runs require publication first; the once-only dispatcher and raw run mapping '
          'are retained in the following checkpoint after actual requests, without implying a terminal result.\n')
(out / 'english-entry.md').write_text(entry, encoding='utf-8')

# Retain actual Main managed dependencies, which later Main builds will replace.
bindir = root / 'tools/Erlang.NifNativeBackend/bin/Debug/net10.0'
deps = bindir / 'Erlang.NifNativeBackend.deps.json'
names = {'Erlang.NifNativeBackend.deps.json', 'Erlang.NifNativeBackend.runtimeconfig.json'}
for target in read(deps)['targets'].values():
    for library in target.values():
        names.update(library.get('runtime', {}))
assert all(Path(name).name == name for name in names)
records = [row(bindir / name) for name in sorted(names)]
archive = out / 'actual-main-managed-native-dependencies.zip'
with zipfile.ZipFile(archive, 'x', zipfile.ZIP_DEFLATED, compresslevel=9) as z:
    z.writestr('index.json', json.dumps({'Status': 'ACTUAL_MAIN_POST_FULL_NATIVE_DEPENDENCIES', 'Files': records}, indent=2))
    for record in records:
        z.writestr(Path(record['Path']).name, (root / record['Path']).read_bytes())
with zipfile.ZipFile(archive) as z:
    for record in records:
        raw = z.read(Path(record['Path']).name)
        assert len(raw) == record['Bytes'] and sha(raw) == record['Sha256']

config['RuntimeNote'] = row(out / 'runtime-note.txt')
config['EnglishEntry'] = row(out / 'english-entry.md')
config['BoundToActualFinalGates'] = True
config['RootSourceReview'] = row(art / 'part313-finalizer-beam310-source-review1.json')
additional = [out / 'runtime-note.txt', out / 'english-entry.md', out / 'pinned-source-guidance.json', archive,
              art / 'root-bind313-finalization.py', art / 'root-promote313-native.py',
              art / 'root-archive313-isolated.py', art / 'part313-finalizer-beam310-source-review1.json',
              art / 'root313-postfull-scripts-beam310-source-peer.json']
additional += [root / x[k]['Path'] for x in source_rows for k in ['FullSource', 'Excerpt']]
additional += [proposal / x for x in ['proposal-freeze.json', 'config-unbound.json', 'finalize313.py', 'README.md']]
existing = {x['SourcePath'] for x in config['FinalCopyRows']}
for p in additional:
    if p.relative_to(root).as_posix() in existing:
        continue
    record = row(p)
    config['FinalCopyRows'].append({'SourcePath': record['Path'],
        'Destination': 'docs/validation/part-313/final-controls/' + p.name,
        'Bytes': record['Bytes'], 'Sha256': record['Sha256'], 'HashMode': 'BinaryExact'})
    existing.add(record['Path'])
payload = (json.dumps(config, indent=2) + '\n').encode()
(out / 'config-scope.json').write_bytes(payload)
print(json.dumps({'ScopeConfigSha256': sha(payload), 'FinalRowsBeforeReadiness': len(config['FinalCopyRows']),
                  'ActualMainManagedDependencyFiles': len(records), 'NativeExecutedByBinder': False,
                  'MainEditedByBinder': False, 'OriginalExecutedByBinder': False}))
