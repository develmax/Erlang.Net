"""Passive review only: published verifier scripts parse downloaded raw evidence."""
from pathlib import Path
import hashlib,json,shutil,subprocess,sys
r=Path.cwd();a=r/'artifacts';head='e829c53ef4323ea8cdd0948cd77c243efa787abd';rid=38766554804;d=a/f'pack309-watch-{rid}-terminal'
j=lambda p:json.loads(p.read_bytes());h=lambda b:hashlib.sha256(b).hexdigest().upper();blob=lambda p:subprocess.check_output(['git','cat-file','blob',head+':'+p])
envelope=j(a/f'pack309-terminal-{rid}-envelope-guard.json');assert envelope['Head']==head and envelope['Conclusion']=='success'
run=j(d/'run.json');jobs=j(d/'jobs.json')['jobs'];items=j(d/'artifacts.json')['artifacts'];assert run['id']==rid and run['run_attempt']==1 and len(jobs)==len(items)==1
job,item=jobs[0],items[0];assert job['id']==116531167319 and job['conclusion']=='success' and item['name']=='beam-fixtures' and all(s['conclusion'] in ['success','skipped'] for s in job['steps'])
ex=d/f'artifact-{item["id"]}';parents=[p.parent for p in ex.rglob('manifest.json') if (p.parent/'load-nif-opcodes/load-nif-opcode-manifest.json').is_file()];assert len(parents)==1;raw=parents[0]
reportrel='load-nif-opcodes/load-nif-opcode-verifier-results.json';remote=j(raw/reportrel);assert remote['Status']=='ACTUAL_ORIGINAL_COMPILE_DISASSEMBLY_THREE_ROLES_PASS'
replay=a/'pack309-beam-passive-replay1';assert not replay.exists();replay.mkdir();tree=replay/'evidence';shutil.copytree(raw,tree);(tree/reportrel).unlink()
tools=replay/'published-review-tools';tools.mkdir();stage=j(a/'root-part309-stage-index-guard.json');indexed={x['Path']:x for x in stage['Records']};sources=[]
for name in ['verify-load-nif-opcodes.py','load-nif-opcode-contract.json','verify-metadata-mutations.py','metadata-mutation-declarations.json','verify-on-load-observers.py']:
 path='tools/Erlang.BeamFixtures/'+name;data=blob(path);(tools/name).write_bytes(data)
 if path in indexed:assert h(data)==indexed[path]['StagedSha256']
 sources.append({'PublishedPath':path,'PublishedBlobSha256':h(data),'Bytes':len(data)})
attempts=[]
for script,args in [('verify-on-load-observers.py',[]),('verify-metadata-mutations.py',['--expected-head',head,'--expected-run',str(rid),'--expected-attempt','1']),('verify-load-nif-opcodes.py',['--expected-head',head,'--expected-run',str(rid),'--expected-attempt','1'])]:
 command=[sys.executable,'-B',str(tools/script),str(tree),*args];p=subprocess.run(command,capture_output=True,text=True);record={'Script':script,'ExitCode':p.returncode,'Stdout':p.stdout,'Stderr':p.stderr,'NativeExecuted':False};attempts.append(record);(replay/(script+'.attempt.json')).write_text(json.dumps(record,indent=2)+'\n');assert p.returncode==0 and p.stderr=='',record
local=j(tree/reportrel);assert local==remote and local['OriginalCompilationPerformed'] and local['OpcodeShapesVerified'] and not local['OriginalBodyExecutionPerformed'] and not local['NativeLoadingExecutionPerformed']
assert {x['ActualOpcode'] for x in local['Roles']}=={'call_ext','call_ext_last','call_ext_only'} and local['RetainedCompilerFixtures']==6 and local['RetainedBodyExecutions']==5
mutation='metadata-mutations/metadata-mutation-verifier-results.json'
assert j(tree/mutation)==j(raw/mutation)
result={'Status':'PASS_EXACT_HEAD309_ORIGINAL_COMPILE_DISASSEMBLY_THREE_ROLES_INDEPENDENT_PASSIVE_REPLAY','Head':head,'Run':rid,'Job':job['id'],'Artifact':item['id'],'Attempt':1,'OriginalCompilationPerformedByRemoteCollector':True,'NewOpcodeShapesVerified':True,'NewFixtureBodyExecuted':False,'NewNativeLoadingExecuted':False,'RetainedCompilerFixtures':6,'RetainedBodyExecutions':5,'RetainedMetadataMutationCases':24,'PublishedVerifierScripts':sources,'ActualOpcodeRoles':local['Roles'],'RawReportSha256':h((raw/reportrel).read_bytes()),'PassiveReplayReportSha256':h((tree/reportrel).read_bytes()),'ReportsParsedExact':True,'RawZipSha256':h((d/f'artifact-{item["id"]}.zip').read_bytes()),'NativeExecutedByVerifier':False,'OriginalCompilerExecutedByVerifier':False,'MainEdited':False,'PostExecuted':False}
out=a/'pack309-beam-independent-guard.json';assert not out.exists();out.write_text(json.dumps(result,indent=2)+'\n');print(json.dumps(result))
