from pathlib import Path
import hashlib, json

r = Path.cwd()
c = r / 'artifacts/length343-candidate'
d = r / 'artifacts/length343-full-actual1'
read = lambda p: json.loads(p.read_bytes())
sha = lambda p: hashlib.sha256(p.read_bytes()).hexdigest().upper()
assert sha(d/'source-freeze.json') == '2D0B06DF331EAB1A3E0C4F96BA954B25A392D4B699C9A0EC7C50D0BF87162BC2'
f = read(d/'source-freeze.json')
owned = {x['Path'] for x in f['Files']}
assert len(owned) == 43 and f['ParentHead'] == '0114bc4ba2c7b8152612e930e3cd36db1caf4180'
for row in f['Files']:
    assert (c/row['Path']).stat().st_size == row['Bytes'] and sha(c/row['Path']) == row['Sha256']
focused = read(r/'artifacts/length343-focused-result/source-freeze.json')
cs = [x for x in focused['Files'] if x['Path'].endswith('.cs')]
assert len(cs) == 32
for row in cs:
    assert sha(c/row['Path']) == row['Sha256']
assert sha(r/'artifacts/length343-focused-independent/report.json') == '6EC6D0FE7FB47E880C18B695F08B2C43F7E27B67E9CEE6CE993500A627F6C990'
assert sha(r/'artifacts/length343-classification-correction-review.json') == 'D3253B40231AF576CC37DB0208ABA78134BFF5DD98B88152251093A3FE1F74D9'
baseline = read(r/'artifacts/part343-delivery-helpers-draft/baseline.json')
changed = []
eol = []
for row in baseline['ProtectedBaselineFiles']:
    name = row['Path']
    assert sha(r/name) == row['Sha256']
    if name == 'docs/supported-mfas.json':
        continue
    previous = (r/name).read_bytes()
    path = c/name
    current = path.read_bytes() if path.exists() else previous
    if previous != current:
        changed.append(name)
        if previous.replace(b'\r\n',b'\n') == current.replace(b'\r\n',b'\n'):
            eol.append(name)
        else:
            assert name in owned
assert set(changed) == {'.gitattributes', '.github/workflows/nif-pending-native.yml', '.github/workflows/nif-reallocation-native.yml', 'docs/mfa-compatibility.json', 'docs/module-readiness-scope.json', 'tests/Erlang.Tests/Program.cs', 'tools/Erlang.Differential/CompiledModuleCases.cs', 'tools/Erlang.NifPendingVarargsObservations/reviewed-source-plan.json', 'tools/Erlang.NifReallocationObservations/SOURCE-PLAN.json'}
old = read(r/'docs/mfa-compatibility.json')
new = read(c/'docs/mfa-compatibility.json')
assert len(old['Entries']) == len(new['Entries']) == 587
for before, after in zip(old['Entries'], new['Entries'], strict=True):
    assert all(before[k] == after[k] for k in ('Module','Function','Arity'))
    assert after['Evidence'][:len(before['Evidence'])] == before['Evidence']
    assert all(x['Kind'] == 'SourceReview' for x in after['Evidence'][len(before['Evidence']):])
    if (before['Module'],before['Function'],before['Arity']) == ('erlang','length',1):
        assert before['Status'] == 'Known differences' and after['Status'] == 'Needs verification'
    else:
        assert all(before[k] == after[k] for k in ('Status','Scope','Limits'))
for source in new['VerifiedSourceFiles']:
    assert sha(c/source['Path']) == source['Sha256']
assert len(new['VerifiedSourceFiles']) == 11
out = r/'artifacts/root-part343-binding-update-review.json'
assert not out.exists()
result = dict(Passed=True, Status='PASS_CURRENT343_SOURCE_BINDING_AND_LENGTH_ONLY_CLASSIFICATION', SourceFreezeSha256=sha(d/'source-freeze.json'), ProtectedBaselineChangedPaths=sorted(changed), EolOnlyProtectedPaths=eol, ReviewedOwnedRows=43, Focused32CSharpRawExact=True, Other586StatusesScopesLimitsPreserved=True, HistoricalEvidencePrefixesRetained=True, CurrentVerifiedSourceFiles=11, OriginalComparisonClaimed=False)
out.write_text(json.dumps(result,indent=2)+'\n',encoding='utf-8',newline='\n')
print(json.dumps(result))
