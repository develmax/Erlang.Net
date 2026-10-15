import importlib.util,json,sys
from pathlib import Path
p=Path(__file__).with_name('pack310-audit-common.py');s=importlib.util.spec_from_file_location('audit',p);m=importlib.util.module_from_spec(s);s.loader.exec_module(m)
kind=sys.argv[1];mapping,d,run,job,items,rows=m.load(kind)
result={'Status':'AUTHENTICATED_EXACT_HEAD310_TERMINAL_BYTES_VERIFIED','Head':mapping['Head'],'Run':mapping['Run'],'Job':job['id'],'Attempt':1,'Workflow':run['name'],'Conclusion':run['conclusion'],'JobConclusion':job['conclusion'],'FailedSteps':[x['name'] for x in job['steps'] if x['conclusion']=='failure'],'RawJobSha256':m.sha((d/f'job-{job["id"]}.log').read_bytes()),'Artifacts':[{'Id':x['id'],'ApiDigest':x['digest'],'Sha256':m.sha((d/f'artifact-{x["id"]}.zip').read_bytes())} for x in items],'RawFilesVerified':len(rows),'MainEdited':False,'PostExecuted':False,'NativeExecutedByVerifier':False}
out=m.A/f'pack310-{kind}-terminal-envelope-guard.json';assert not out.exists();out.write_text(json.dumps(result,indent=2)+'\n');print(json.dumps(result))
