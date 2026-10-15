"""Artifact-only binder: explicit reuse, standalone tool proof and current remote states."""
from pathlib import Path
import hashlib,json,subprocess,ast
r=Path.cwd();a=r/'artifacts';b=Path(__file__).resolve().parent;head='e1d8a999faac113d98ebf2d4471f80ef30daf84a';h=lambda x:hashlib.sha256(x).hexdigest().upper();j=lambda p:json.loads(p.read_bytes());rows=[]
def pin(name):
 p=r/name;data=p.read_bytes();return {'Path':name,'Bytes':len(data),'Sha256':h(data)}
def add(source,dest,category):
 actual=pin(source);assert not any(x['Destination']==dest for x in rows);rows.append({'SourcePath':source,'Destination':dest,'Bytes':actual['Bytes'],'Sha256':actual['Sha256'],'Category':category})
reuse=b/'reused308';reuse.mkdir(exist_ok=True)
published={'Tests':'docs/validation/part-308/tests.json','Integration':'docs/validation/part-308/integration-results.json','Registry':'docs/validation/part-308/supported-mfas.json','ReusedNativeGuard':'docs/validation/part-308/root-artifacts/root-part308-main-native-guard.json','ReusedFullGuard':'docs/validation/part-308/root-artifacts/root-part308-full-guard.json','ReusedSourceFreeze':'docs/validation/part-308/root-artifacts/root-part308-final-source-freeze.json','ReusedRawRegistry':'docs/validation/part-308/root-artifacts/root-part308-raw-supported-mfas.json','ReusedOptionalGuard':'docs/validation/part-308/root-artifacts/root-part308-optional-io-guard.json'}
c={'Part':309,'ParentHead':head,'SourcePaths':8,'CSharpPaths':3,'ExpectedPassed':6637,'ExpectedRegistry':505,'ExpectedNativeInputs':1020,'ExpectedOptionalIo':507,'ExpectedErlang':347,'NewFullValidationExecuted':False,'ComparisonTestReport':'validation/part-308/tests.json','ReusedPublishedPaths':published}
for label,path in published.items():
 data=subprocess.check_output(['git','cat-file','blob',head+':'+path],cwd=r);dst=reuse/(label+'.json')
 if dst.exists():assert dst.read_bytes()==data
 else:dst.write_bytes(data)
 source=dst.relative_to(r).as_posix();c[label]=pin(source)
 destination={'Tests':'tests.json','Integration':'integration-results.json','Registry':'supported-mfas.json'}.get(label,'reused308/'+dst.name)
 add(source,'docs/validation/part-309/'+destination,'reused-published308')
statepath=b/'remote308-states.json';frozenstates=j(statepath) if statepath.exists() else None
states={'Head':head,'Runs':{}};mapping={'ci':38763479716,'native':38763668519,'original':38763659694};summary=[]
for kind,rid in mapping.items():
 snapshot=(r/frozenstates['Runs'][kind]['Snapshot']['Path']) if frozenstates else sorted(a.glob(f'pack308-watch-{rid}-*.json'))[-1];v=j(snapshot);run=v['Run'];jobs=v['Jobs']['jobs'];assert run['head_sha']==head and run['run_attempt']==1 and len(jobs)==1
 manifest=a/f'pack308-{kind}-detached-staging-manifest.json'
 guardname={'ci':'pack308-ci-38763479716-independent-guard.json','native':'pack308-native-combined-independent-guard.json','original':'pack308-original-38763659694-semantic-guard.json'}[kind]
 guardpath=a/guardname
 verified=manifest.is_file() and guardpath.is_file() and j(guardpath).get('Head')==head and j(guardpath).get('Status','').startswith('PASS_') and run['status']=='completed' and run['conclusion']=='success'
 if frozenstates:verified=frozenstates['Runs'][kind]['IndependentVerified']
 state={'Run':rid,'Job':jobs[0]['id'],'Head':head,'Attempt':1,'Status':run['status'],'Conclusion':run['conclusion'],'IndependentVerified':verified,'Snapshot':pin(snapshot.relative_to(r).as_posix()),'FetchedUtc':v['FetchedUtc']};states['Runs'][kind]=state
 add(snapshot.relative_to(r).as_posix(),'docs/validation/part-309/remote308/'+snapshot.name,'remote308-snapshot')
 if manifest.is_file() and verified:
  for row in j(manifest):assert pin(row['SourcePath'])['Sha256']==row['Sha256'];rows.append(dict(row,Category='remote308-verified' if verified else 'remote308-preserved-unverified'))
  add(manifest.relative_to(r).as_posix(),'docs/validation/part-309/remote308/'+manifest.name,'remote308-verified' if verified else 'remote308-preserved-unverified')
 summary.append(kind.upper()+(' independently verified' if verified else (' '+run['status'].replace('_',' ')+(('; '+str(run['conclusion'])+'; independent review pending') if run['status']=='completed' else ''))))
if frozenstates:assert states==frozenstates
else:statepath.write_text(json.dumps(states,indent=2)+'\n')
c['RemoteStates']=pin(statepath.relative_to(r).as_posix());c['Remote308Summary']='; '.join(summary)+'. Exact capture times and authenticated snapshots are archived.';add(statepath.relative_to(r).as_posix(),'docs/validation/part-309/remote308/states.json','remote308-snapshot')
tool=a/'beam-load-nif-opcodes313-validated1';tf=j(tool/'validation-freeze.json');assert len(tf['Files'])==103
for row in tf['Files']:
 source=(tool/row['Path']).relative_to(r).as_posix();actual=pin(source);assert actual['Bytes']==row['Bytes'] and actual['Sha256']==row['Sha256'];add(source,'docs/validation/part-309/tooling313/'+row['Path'],'tooling313-frozen')
add((tool/'validation-freeze.json').relative_to(r).as_posix(),'docs/validation/part-309/tooling313/validation-freeze.json','tooling313-frozen')
for name in ['root-part309-final-source-freeze.json','root-part309-reuse-and-tool-guard.json','root-guard309-reuse.py','root-prepare309.py','root-audit309-erlang-table.py','root-part309-main-tool-target-exit.json','root-part309-main-tool-target.log']:
 add('artifacts/'+name,'docs/validation/part-309/root-artifacts/'+name,'root-explicit')
for name in ['audit.json','ERLANG_COMPATIBILITY.md']:add('artifacts/root-part309-erlang-table-candidate/'+name,'docs/validation/part-309/table-correction/'+name,'table-correction')
add('artifacts/beam-load-nif-opcodes313-peer-allocator309-review1.json','docs/validation/part-309/independent-reviews/beam-load-nif-opcodes313-peer-allocator309-review1.json','independent-review')
bindings={'SourceFreeze':'artifacts/root-part309-final-source-freeze.json','ReuseGuard':'artifacts/root-part309-reuse-and-tool-guard.json','ToolTargetExit':'artifacts/root-part309-main-tool-target-exit.json','ToolTargetLog':'artifacts/root-part309-main-tool-target.log','ToolFreeze':'artifacts/beam-load-nif-opcodes313-validated1/validation-freeze.json','ToolReceipt':'artifacts/beam-load-nif-opcodes313-validated1/validation-receipt.json','NativeSourcePlan':'tools/Erlang.NifPendingVarargsObservations/reviewed-source-plan.json','TableAudit':'artifacts/root-part309-erlang-table-candidate/audit.json','TableCandidate':'artifacts/root-part309-erlang-table-candidate/ERLANG_COMPATIBILITY.md','MetadataNote':'artifacts/part309-packaging-proposal1/part309-metadata-note.txt','ChangelogDraft':'artifacts/part309-packaging-proposal1/part309-changelog-draft.md','ReadinessScopeBefore':'docs/module-readiness-scope.json','ReadinessResult':'docs/module-readiness.json','ActiveChangelogBefore':'docs/changelog/0046.md','ChangelogIndexBefore':'CHANGELOG.md'}
c.update({key:pin(value) for key,value in bindings.items()});f=j(r/c['SourceFreeze']['Path']);c['AllowedTextLfSources']=[x['Path'] for x in f['Files'] if not x['Path'].endswith('.cs')]
metadata=['AGENTS.md','docs/PROGRESS.md','docs/NEXT_STEPS.md','docs/COMPATIBILITY.md','docs/ERLANG_COMPATIBILITY.md','docs/DECISIONS.md'];c['MetadataPaths']=metadata;c['StageMetadataPaths']=metadata+['CHANGELOG.md','docs/changelog/0046.md','docs/module-readiness-scope.json','docs/module-readiness.json','docs/MODULE_READINESS.md','docs/supported-mfas.json'];c['ChangelogIndexOldLabel']='parts302–308 (active)';c['ChangelogIndexNewLabel']='parts302–309 (active)'
for p in sorted(b.iterdir()):
 if p.is_file() and p.suffix in ['.py','.md','.txt']:add(p.relative_to(r).as_posix(),'docs/validation/part-309/packaging-proposal1/'+p.name,'packaging')
for source,dest in [('docs/module-readiness.json','module-readiness.json'),('docs/MODULE_READINESS.md','MODULE_READINESS.md')]:add(source,'docs/validation/part-309/'+dest,'readiness')
(b/'evidence-index.json').write_text(json.dumps(rows,indent=2)+'\n');c['EvidenceIndex']=pin((b/'evidence-index.json').relative_to(r).as_posix());(b/'config-actual.json').write_text(json.dumps(c,indent=2)+'\n')
for p in b.glob('*.py'):ast.parse(p.read_text(encoding='utf-8-sig'))
print(json.dumps({'IndexedFiles':len(rows),'NewFullValidationExecuted':False,'Reused308Passed':6637,'NewStandaloneToolChecks':True,'Remote308Summary':c['Remote308Summary']}))
