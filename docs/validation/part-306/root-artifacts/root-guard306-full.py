from pathlib import Path
import hashlib,json,runpy,sys
sys.dont_write_bytecode=True
r=Path.cwd(); a=r/'artifacts'; read=lambda p:json.loads(p.read_bytes()); sha=lambda b:hashlib.sha256(b).hexdigest().upper()
f=a/'root-part306-final-source-freeze2.json'; assert sha(f.read_bytes())=='C6AD4F666C908F34F5A4F64E9B7B98C1B1B4FE352F6309FA1FBC26819CBB68F3'; v=read(f)
for row in v['Files']:
 b=(r/row['Path']).read_bytes(); assert len(b)==row['Bytes'] and sha(b)==row['Sha256'],row['Path']
assert read(a/'root-part306-full-exit.json')['ExitCode']==0
old=read(r/'docs/validation/part-305/tests.json'); now=read(a/'tests.json')
assert old['Passed']==6624 and now['Passed']==6627 and now['Failed']==0 and len(now['Tests'])==6627
oldnames=[x['Name'] for x in old['Tests']]; added=[x['Name'] for x in now['Tests'] if x['Name'] not in oldnames]
assert len(added)==3 and len(set(added))==3 and all(x in added for x in v['AddedHostNames'])
assert [x['Name'] for x in now['Tests'] if x['Name'] not in added]==oldnames
assert all(x['Status']=='Passed' for x in now['Tests'])
integration=read(a/'integration-results.json'); assert len(integration)==11 and integration==read(r/'docs/validation/part-305/integration-results.json')
assert 'Source layout: 0 files need formatting' in (a/'root-part306-full-validation.log').read_text(encoding='utf-8-sig')
plan=r/'tools/Erlang.NifPendingVarargsObservations/reviewed-source-plan.json'; pv=read(plan); helper=runpy.run_path(str(plan.parent/'run-reviewed.py'))
assert len(pv['Files'])==1012 and helper['reviewed_inputs'](r,plan)=={x['Path'] for x in pv['Files']}
for x in pv['Files']:
 row=helper['record'](r/x['Path'],r,x['HashMode']); row['Path']=x['Path']; assert row==x,x['Path']
out=a/'root-part306-full-guard.json'; assert not out.exists()
result={'Status':'ACTUAL_MAIN_6627_FULL_ALL6624_ORDERED_NAMES_38_SOURCE_22_CSHARP_AND_11_INTEGRATION_PASS','Passed':6627,'Failed':0,'AddedNames':added,'HistoricalOrderedNamesPreserved':6624,'SourceHashesExact':True,'SourceFreezeSha256':sha(f.read_bytes()),'SourcePaths':38,'CSharpPaths':22,'ReviewedSourceInputs':1012,'IntegrationResults':integration,'NativeExecutedByThisGuard':False,'PublicLoadNifActivated':False}
out.write_text(json.dumps(result,indent=2)+'\n',encoding='utf-8'); print(json.dumps(result))
