from pathlib import Path
import hashlib,json,runpy,sys
sys.dont_write_bytecode=True
r=Path.cwd();a=r/'artifacts';read=lambda p:json.loads(p.read_bytes());sha=lambda b:hashlib.sha256(b).hexdigest().upper()
f=a/'root-part307-final-source-freeze2.json';assert sha(f.read_bytes())=='9C6CE588B09C182AF582252BB377CB3E0DC4650F78444BD1A49D6371AA78964D';v=read(f)
for row in v['Files']:
 b=(r/row['Path']).read_bytes();assert len(b)==row['Bytes'] and sha(b)==row['Sha256'],row['Path']
assert read(a/'root-part307-full-exit.json')['ExitCode']==0
old=read(r/'docs/validation/part-306/tests.json');now=read(a/'tests.json')
assert old['Passed']==now['Passed']==6627 and now['Failed']==0 and len(now['Tests'])==6627
assert [x['Name'] for x in now['Tests']]==[x['Name'] for x in old['Tests']]
assert all(x['Status']=='Passed' for x in now['Tests'])
assert sum(x['Name']==v['ExtendedExistingHost'] for x in now['Tests'])==1
integration=read(a/'integration-results.json');assert len(integration)==11 and integration==read(r/'docs/validation/part-306/integration-results.json')
assert 'Source layout: 0 files need formatting' in (a/'root-part307-full-validation.log').read_text(encoding='utf-8-sig')
plan=r/'tools/Erlang.NifPendingVarargsObservations/reviewed-source-plan.json'; pv=read(plan); helper=runpy.run_path(str(plan.parent/'run-reviewed.py'))
assert len(pv['Files'])==1013 and helper['reviewed_inputs'](r,plan)=={x['Path'] for x in pv['Files']}
for x in pv['Files']:
 row=helper['record'](r/x['Path'],r,x['HashMode']);row['Path']=x['Path'];assert row==x,x['Path']
out=a/'root-part307-full-guard.json';assert not out.exists()
result={'Status':'ACTUAL_MAIN_6627_FULL_ALL6627_ORDERED_NAMES_12_SOURCE_8_CSHARP_AND_11_INTEGRATION_PASS','Passed':6627,'Failed':0,'AddedNames':[],'HistoricalOrderedNamesPreserved':6627,'ExtendedExistingHost':v['ExtendedExistingHost'],'SourceHashesExact':True,'SourceFreezeSha256':sha(f.read_bytes()),'SourcePaths':12,'CSharpPaths':8,'ReviewedSourceInputs':1013,'IntegrationResults':integration,'NativeExecutedByThisGuard':False,'PublicLoadNifActivated':False}
out.write_text(json.dumps(result,indent=2)+'\n',encoding='utf-8');print(json.dumps(result))
