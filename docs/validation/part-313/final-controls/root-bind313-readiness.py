from pathlib import Path
import hashlib
import json

root = Path.cwd()
art = root / 'artifacts'
out = art / 'root-part313-finalization-bound'
sha = lambda b: hashlib.sha256(b).hexdigest().upper()
read = lambda p: json.loads(p.read_bytes())
row = lambda p: {'Path': p.relative_to(root).as_posix(), 'Bytes': p.stat().st_size,
                 'Sha256': sha(p.read_bytes())}
assert not (out / 'config-final.json').exists()
config = read(out / 'config-scope.json')
assert sha((out / 'config-scope.json').read_bytes()) == '80909EB804FBAF464DBFBED47443C56AABF1B88C0051113F0CC1D3598313D103'
peer = art / 'part313-finalizer-beam310-bound-review2.json'
assert sha(peer.read_bytes()) == '129890FC8A0B1AB8AA8085F0A1CDBF86C2F29BA7ED06860F4C37F9690B48DB95'
ready = read(root / 'docs/module-readiness.json')
assert (ready['LocalPassedTests'], ready['RegisteredMfas'], ready['PreviousRegisteredMfas'],
        ready['RegisteredMfaDelta'], ready['ComparisonTestReport']) == (6640, 505, 505, 0, 'validation/part-312/tests.json')
assert read(out / 'scope-marker.json')['ScopeSha256'] == sha((root / 'docs/module-readiness-scope.json').read_bytes())
command = {'Status': 'ROOT_OBSERVED_ACTUAL_READINESS_CLI_EXIT_ZERO', 'ExitCode': 0,
           'Command': ['dotnet', 'tools/Erlang.Tool/bin/Debug/net10.0/Erlang.Tool.dll',
                       'readiness', 'docs', 'artifacts/tests.json', 'docs'],
           'Tool': row(root / 'tools/Erlang.Tool/bin/Debug/net10.0/Erlang.Tool.dll'),
           'ActualReport': row(root / 'docs/module-readiness.json'),
           'RawLog': row(out / 'readiness.log'), 'RegisteredMfaDelta': 0}
(out / 'readiness-command.json').write_text(json.dumps(command, indent=2) + '\n', encoding='utf-8')
entry = (out / 'english-entry.md').read_text(encoding='utf-8')
entry = entry.replace('Readiness is required to retain 505 registered MFAs, delta zero, and use the previous verified part312 comparison report.',
                      'Readiness reports 505 registered MFAs, delta zero, and compares against the previous verified part312 report.')
entry += ('\nThe actual readiness refresh used `dotnet tools/Erlang.Tool/bin/Debug/net10.0/Erlang.Tool.dll '
          'readiness docs artifacts/tests.json docs`; its exit was zero. See the '
          '[command and report identity](../validation/part-313/final-controls/readiness-command.json).\n')
(out / 'english-entry-final.md').write_text(entry, encoding='utf-8')
config['EnglishEntry'] = row(out / 'english-entry-final.md')
config['Readiness'] = row(root / 'docs/module-readiness.json')
config['BoundScopeSourcePeer'] = row(peer)
additions = [(root / ('docs/' + name), 'final-main/' + name) for name in
             ['module-readiness.json', 'MODULE_READINESS.md', 'module-readiness-scope.json', 'supported-mfas.json']]
additions += [(p, 'final-controls/' + p.name) for p in
              [out / 'scope-marker.json', out / 'config-scope.json', out / 'readiness.log',
               out / 'readiness-command.json', out / 'english-entry-final.md', peer,
               art / 'root-bind313-readiness.py']]
for p, destination in additions:
    record = row(p)
    assert record['Path'] not in {x['SourcePath'] for x in config['FinalCopyRows']}
    config['FinalCopyRows'].append({'SourcePath': record['Path'],
        'Destination': 'docs/validation/part-313/' + destination,
        'Bytes': record['Bytes'], 'Sha256': record['Sha256'], 'HashMode': 'BinaryExact'})
assert len({x['Destination'] for x in config['FinalCopyRows']}) == len(config['FinalCopyRows'])
raw = (json.dumps(config, indent=2) + '\n').encode()
(out / 'config-final.json').write_bytes(raw)
print(json.dumps({'FinalConfigSha256': sha(raw), 'FinalActualRows': len(config['FinalCopyRows']),
                  'ReadyRegisteredMfas': 505, 'ReadyDelta': 0, 'MainEditedByBinder': False}))
