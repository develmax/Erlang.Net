from pathlib import Path
import hashlib,json,runpy,sys
sys.dont_write_bytecode=True
r=Path.cwd();a=r/'artifacts';read=lambda p:json.loads(p.read_bytes());sha=lambda b:hashlib.sha256(b).hexdigest().upper()
f=a/'root-part310-final-source-freeze.json';v=read(f)
assert v['SourcePaths']==26 and v['CSharpPaths']==20 and v['PublicLoadNifRegistered'] and not v['DefaultNativeLoadInstructionHandlerInstalled']
for row in v['Files']:
 b=(r/row['Path']).read_bytes();assert len(b)==row['Bytes'] and sha(b)==row['Sha256'],row['Path']
assert read(a/'root-part310-full-exit.json')['ExitCode']==0
closurepath=a/'root-part310-combined-isolated/candidate/final-source-resource-closure.json'
assert sha(closurepath.read_bytes())==v['FinalSourceClosureSha256']
closure=read(closurepath);assert len(closure['Files'])==2149
for row in closure['Files']:
 b=(r/row['Path']).read_bytes();assert len(b)==row['Bytes'] and sha(b)==row['Sha256'],row['Path']
old=read(r/'docs/validation/part-309/tests.json');now=read(a/'tests.json')
assert old['Passed']==6637 and now['Passed']==6638 and now['Failed']==0 and len(now['Tests'])==6638
names=[x['Name'] for x in now['Tests']];previous=[x['Name'] for x in old['Tests']];assert len(set(names))==6638
assert [x for x in names if x in set(previous)]==previous
added=[x for x in names if x not in set(previous)];assert added==v['AddedHostNames']
assert all(x['Status']=='Passed' for x in now['Tests'])
integration=read(a/'integration-results.json');assert len(integration)==11 and integration==read(r/'docs/validation/part-309/integration-results.json')
assert 'Source layout: 0 files need formatting' in (a/'root-part310-full-validation.log').read_text(encoding='utf-8-sig')
plan=r/'tools/Erlang.NifPendingVarargsObservations/reviewed-source-plan.json';pv=read(plan);helper=runpy.run_path(str(plan.parent/'run-reviewed.py'))
assert len(pv['Files'])==1028 and helper['reviewed_inputs'](r,plan)=={x['Path'] for x in pv['Files']}
for x in pv['Files']:
 row=helper['record'](r/x['Path'],r,x['HashMode']);row['Path']=x['Path'];assert row==x,x['Path']
result={'Status':'ACTUAL_MAIN_6638_FULL_ALL6637_HISTORICAL_ORDERED_NAMES_26_SOURCE_20_CSHARP_AND_11_INTEGRATION_PASS','Passed':6638,'Failed':0,'AddedNames':added,'HistoricalOrderedNamesPreserved':6637,'SourceHashesExact':True,'SourceFreezeSha256':sha(f.read_bytes()),'SourcePaths':26,'CSharpPaths':20,'ReviewedSourceInputs':1028,'IntegrationResults':integration,'PublicLoadNifRegistered':True,'DefaultNativeLoadInstructionHandlerInstalled':False,'GeneralNifAbiComplete':False,'NewPublicMfa':False,'OriginalBeamRolesCompilationHead':'e829c53ef4323ea8cdd0948cd77c243efa787abd','NewOriginalFixtureBodyExecution':False}
optional=read(a/'root-part308-optional-io-guard.json')
assert any(x['Name']==optional['PassedHost'] and x['Status']=='Passed' for x in now['Tests'])
for x in optional['Sources']:assert sha((r/x['Path']).read_bytes())==x['Sha256'],x['Path']
optional['FullTestsSha256']=sha((a/'tests.json').read_bytes());op=a/'root-part310-optional-io-guard.json';assert not op.exists();op.write_text(json.dumps(optional,indent=2)+'\n',encoding='utf-8')
out=a/'root-part310-full-guard.json';assert not out.exists();out.write_text(json.dumps(result,indent=2)+'\n',encoding='utf-8');print(json.dumps(result))
