"""Read existing settled receipts. Never run tests or alter original reports."""
import json, sys, time
from pathlib import Path
sys.path.insert(0, str(Path(__file__).parent/'library413-final-regression-proposal1'))
from common import sha, snapshot, catalog, write
r=Path(__file__).resolve().parent.parent
o=r/'artifacts/library436-passive-coverage-audit1'
assert not o.exists(), 'Fresh output required'
def read(p): return json.loads(p.read_text(encoding='utf-8-sig'))
s1=read(r/'artifacts/library436-affected-selection1/selection.json')
s2=read(r/'artifacts/library436-affected-selection2/selection.json')
run1=r/'artifacts/library436-affected-run1'
run2=r/'artifacts/library436-affected-run2'
assert (run2/'completion.json').is_file(), 'Wait for original runner settlement'
c2=read(run2/'completion.json')
assert c2['Passed'] is False and c2['PreparationPassed'] and c2['ImmutableInputs']
assert not c2['PreparationFailures']
i1=read(run1/'invocation.json'); i2=read(run2/'invocation.json')
assert i1['Bindings']==i2['Bindings']==s1['Bindings']==s2['Bindings']==snapshot(r)
assert i1['SelectionSha256']==sha(r/'artifacts/library436-affected-selection1/selection.json')
assert i2['SelectionSha256']==sha(r/'artifacts/library436-affected-selection2/selection.json')
names=catalog(run2/'actual-catalog.json')
assert set(names)==set(catalog(r/'artifacts/library436-affected-selection2/catalog.json'))
assert len(names)==8813
rows=[]; all_names=[]
def verify(directory, shard, metadata, retry):
    d=directory/shard['Name']; report=d/'report.json'; a=read(report)
    got=[x['Name'] for x in a['Tests']]; expected=shard['ExactCases']
    assert a['Partial'] and a['SelectedCount']==len(got)==len(set(got))==len(expected)
    assert set(got)==set(expected)==set(a['ExactCases']) and a['Blocks']==[]
    assert a['TotalCount']==len(names)
    assert all(x['Status']=='Passed' for x in a['Tests'])
    assert a['Passed']==len(got) and a['Failed']==0
    receipt=read(d/'execution.exit.json')
    assert receipt['Settled'] and not receipt['TimedOut'] and not receipt['Failures'] and receipt['ExitCode']==0
    assert receipt['StdoutSha256']==sha(d/'execution.stdout.log')
    assert receipt['StderrSha256']==sha(d/'execution.stderr.log')
    command=receipt['Command']; selected=[command[i+1] for i,x in enumerate(command) if x=='--case']
    assert selected==expected and Path(command[-1]).resolve()==report.resolve()
    assert metadata['ImmutableInputs'] and metadata['Requested']==len(got)
    if retry:
        assert metadata['Failures']==["KeyError('CatalogCount')"] and not metadata['Passed']
    else:
        assert metadata['Passed'] and not metadata['Failures']
        assert metadata['ReportSha256']==sha(report) and set(metadata['ActualNames'])==set(got)
    all_names.extend(got)
    rows.append(dict(Run=directory.name,Shard=shard['Name'],Count=len(got),ReportSha256=sha(report),ReceiptSha256=sha(d/'execution.exit.json'),ElapsedSeconds=receipt['ElapsedSeconds'],RetainedWrapperFailures=metadata['Failures']))
p1=read(run1/'progress.json'); map1={x['Name']:x for x in s1['Shards']}
for entry in p1:
    if entry['Passed']: verify(run1,map1[entry['Shard']],entry,False)
assert len(all_names)==350
p2=read(run2/'progress.json'); map2={x['Name']:x for x in s2['Shards']}
assert len(p2)==len(map2) and {x['Shard'] for x in p2}==set(map2)
for entry in p2: verify(run2,map2[entry['Shard']],entry,True)
assert len(all_names)==len(set(all_names))==s1['Count']==3528
assert set(all_names)==set(s1['ExactCases'])
assert set(s2['ExactCases']).issubset(set(all_names)) and len(s2['ExactCases'])==3178
assert snapshot(r)==i2['Bindings']
o.mkdir()
write(o/'completion.json',dict(Passed=True,Count=len(all_names),ExactCoverage=True,CatalogCount=len(names),ImmutableInputs=True,CompositeRun=True,CanonicalFullRegression=False,OriginalRunnerCompletionPassed=False,RetainedFirstRunOomAndInterruption=True,RetainedRetryAggregationError="KeyError('CatalogCount')",NoReportOrSourceMutation=True,ElapsedChildSeconds=sum(x['ElapsedSeconds'] for x in rows),Blocks=rows,Bindings=i2['Bindings'],AuditScriptSha256=sha(Path(__file__))))
print(f"Passive composite audit PASS {len(all_names)}/{s1['Count']}; {len(rows)} settled blocks; original wrapper errors retained")
