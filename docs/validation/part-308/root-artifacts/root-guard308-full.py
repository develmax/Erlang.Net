from pathlib import Path
import hashlib,json,runpy,sys
sys.dont_write_bytecode=True
r=Path.cwd();a=r/'artifacts';read=lambda p:json.loads(p.read_bytes());sha=lambda b:hashlib.sha256(b).hexdigest().upper()
f=a/'root-part308-final-source-freeze.json';v=read(f)
assert v['SourcePaths']==32 and v['CSharpPaths']==28 and v['PublicLoadNifRegistered'] and not v['DefaultNativeLoadInstructionHandlerInstalled']
for row in v['Files']:
 b=(r/row['Path']).read_bytes();assert len(b)==row['Bytes'] and sha(b)==row['Sha256'],row['Path']
assert read(a/'root-part308-full-exit.json')['ExitCode']==0
old=read(r/'docs/validation/part-307/tests.json');now=read(a/'tests.json')
assert old['Passed']==6627 and now['Passed']==6637 and now['Failed']==0 and len(now['Tests'])==6637
names=[x['Name'] for x in now['Tests']];previous=[x['Name'] for x in old['Tests']];assert len(set(names))==6637
assert [x for x in names if x in set(previous)]==previous
added=[x for x in names if x not in set(previous)];assert len(added)==10 and all(x in names for x in v['AddedHostNames'])
assert all(x['Status']=='Passed' for x in now['Tests'])
integration=read(a/'integration-results.json');assert len(integration)==11 and integration==read(r/'docs/validation/part-307/integration-results.json')
assert 'Source layout: 0 files need formatting' in (a/'root-part308-full-validation.log').read_text(encoding='utf-8-sig')
plan=r/'tools/Erlang.NifPendingVarargsObservations/reviewed-source-plan.json';pv=read(plan);helper=runpy.run_path(str(plan.parent/'run-reviewed.py'))
assert len(pv['Files'])==1020 and helper['reviewed_inputs'](r,plan)=={x['Path'] for x in pv['Files']}
for x in pv['Files']:
 row=helper['record'](r/x['Path'],r,x['HashMode']);row['Path']=x['Path'];assert row==x,x['Path']
result={'Status':'ACTUAL_MAIN_6637_FULL_ALL6627_HISTORICAL_ORDERED_NAMES_32_SOURCE_28_CSHARP_AND_11_INTEGRATION_PASS','Passed':6637,'Failed':0,'AddedNames':added,'HistoricalOrderedNamesPreserved':6627,'SourceHashesExact':True,'SourceFreezeSha256':sha(f.read_bytes()),'SourcePaths':32,'CSharpPaths':28,'ReviewedSourceInputs':1020,'IntegrationResults':integration,'PublicLoadNifRegistered':True,'DefaultNativeLoadInstructionHandlerInstalled':False,'GeneralNifAbiComplete':False,'NewOriginalSevenCompared':False}
out=a/'root-part308-full-guard.json';assert not out.exists();out.write_text(json.dumps(result,indent=2)+'\n',encoding='utf-8');print(json.dumps(result))
