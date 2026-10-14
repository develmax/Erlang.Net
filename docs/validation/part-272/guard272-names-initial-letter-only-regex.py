from pathlib import Path
import json, re, hashlib
r = Path.cwd()
sha = lambda p: hashlib.sha256(p.read_bytes()).hexdigest().upper()
oldpath = r / 'docs/validation/part-271/tests.json'
newpath = r / 'artifacts/tests.json'
old, new = [json.loads(p.read_bytes()) for p in (oldpath, newpath)]
assert (old['Passed'], new['Passed'], new['Failed']) == (6497, 6515, 0)
source = r / 'tools/Erlang.Differential/AtomScalarValidationCases.cs'
modules = re.findall(r'"(atom-scalar-validation-[a-z-]+)"', source.read_text())
assert len(modules) == len(set(modules)) == 13
diagnostics = ['quoted', 'bare', 'variable', 'quoted-supplementary']
oldnames = [v['Name'] for v in old['Tests']]
names = [v['Name'] for v in new['Tests']]
added = set(names) - set(oldnames)
assert len(added) == 18
assert {v for v in added if '/compiled-module/' in v} == {'oracle/compiled-module/' + v for v in modules}
assert sum('atom-name-lexer-diagnostic-' in v for v in added) == 4
for d in diagnostics:
    assert sum(v.endswith('atom-name-lexer-diagnostic-' + d) for v in added) == 1
host = [v for v in added if 'atom-scalar-validation' not in v and 'atom-name-lexer-diagnostic' not in v]
assert len(host) == 1
assert [v for v in names if v not in added] == oldnames
assert len(names) == len(set(names))
inherited = json.loads((r / 'artifacts/part271-test-name-preservation-guard.json').read_bytes())['ExactlyDeclaredRemovedNames']
result = {'Status': 'PASS', 'OldCount': 6497, 'NewCount': 6515, 'NewNames': sorted(added), 'ExactlyDeclaredRemovedNames': inherited + sorted(added), 'AllOldNamesAndOrderExact': True, 'OldReportSha256': sha(oldpath), 'NewReportSha256': sha(newpath), 'AtomDeclarationsSha256': sha(source), 'NativeExecuted': False}
(r / 'artifacts/part272-test-name-preservation-guard.json').write_bytes((json.dumps(result, indent=2) + '\n').encode())
print('PASS old 6497 names/order exact after precisely 13 compiled, four diagnostic and one HOST additions filtered')
