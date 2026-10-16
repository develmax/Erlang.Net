from pathlib import Path
import hashlib
import json

r = Path.cwd()
c = r / 'artifacts/proc-lib335-candidate'
d = r / 'artifacts/proc-lib335-full-source'
j = lambda p: json.loads(p.read_bytes())
h = lambda value: hashlib.sha256(value).hexdigest().upper()
f = j(d / 'source-freeze.json')
assert h((d/'source-freeze.json').read_bytes()) == '9E2D477D756246BCC7F9403354D7B63CF50D923745DE0020B7CF212E6499C817'
assert f['ParentHead'] == '8f27347c3137172276b3292753cc7a4595746e2f'
assert (f['OwnedPaths'], f['OwnedCSharpFiles'], f['SourceClosurePaths']) == (24, 20, 2460)
owned = {row['Path'] for row in f['Files']}
for manifest in ['source-resource-closure.json', 'package-resources.json', 'readiness-inputs.json']:
    for row in j(d / manifest)['Files']:
        value = (c / row['Path']).read_bytes()
        assert len(value) == row['Bytes'] and h(value) == row['Sha256'], row['Path']
        if manifest != 'readiness-inputs.json' and row['Path'] not in owned:
            assert value == (r / row['Path']).read_bytes(), row['Path']
for row in f['Files']:
    value = (c / row['Path']).read_bytes()
    assert len(value) == row['Bytes'] and h(value) == row['Sha256']
    if row['Path'].endswith('.cs'):
        assert b'\r' not in value
for plan in f['Plans']:
    path = plan['Path']
    old = j(r / path)
    new = j(c / path)
    assert {k:v for k,v in old.items() if k!='Files'} == {k:v for k,v in new.items() if k!='Files'}
    assert len(new['Files']) == plan['Inputs']
    assert h((c/path).read_bytes()) == plan['Sha256']
    assert set(row['Path'] for row in old['Files']) <= set(row['Path'] for row in new['Files'])
    assert len(new['Files']) - len(old['Files']) == 9
    for row in new['Files']:
        value = (c/row['Path']).read_bytes()
        if row.get('HashMode') == 'TextLF':
            value = value.replace(b'\r\n',b'\n').replace(b'\r',b'\n')
        assert h(value) == row['Sha256'], row['Path']
    workflow = '.github/workflows/nif-pending-native.yml' if 'Varargs' in path else '.github/workflows/nif-reallocation-native.yml'
    value = (c/workflow).read_bytes()
    assert b'\r' not in value and plan['Sha256'].encode() in value
actual = r/'artifacts/proc-lib335-actual6'
assert j(actual/'stop-focused.json')['Status'] == 'PASS'
assert all(row['Status']=='PASS' for row in j(actual/'stop-focused.json')['Cases'])
assert j(actual/'build.process.json')['ExitCode'] == 0
assert j(actual/'stop-focused.process.json')['ExitCode'] == 0
report = {'Status':'PASS_ROOT335_SOURCE_COMPOSITION_WITH_AUTHORSHIP_DISCLOSURE','Passed':True,'SourceFreezeSha256':h((d/'source-freeze.json').read_bytes()),'Parent':f['ParentHead'],'OwnedPaths':24,'CSharpPaths':20,'ClosureRows':2460,'PackageResources':6,'Plans':[row['Inputs'] for row in f['Plans']],'AllNonFilesPoliciesUnchanged':True,'FocusedCompiled':29,'DirectContracts':4,'BareRuntimeHost':1,'SourceReviewedNoconnectionBoundaryAndExactExitWrapper':True,'AuthorshipDisclosure':'Root authored the initial unintegrated stop/sys draft; publisher ported/fixed protocol and authoring fixtures; recursion rebased, compiled and source-corrected fixtures. This is a passive composition review, not an independent implementation audit. Original execution remains required.','SdkExecutedByThisReview':False,'OriginalExecuted':False}
(r/'artifacts/root-part335-composition-review.json').write_text(json.dumps(report,indent=2)+'\n',encoding='utf-8')
print(json.dumps(report))
