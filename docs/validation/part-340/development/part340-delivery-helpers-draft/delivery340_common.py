from pathlib import Path
import hashlib, json
PARENT = '36e6da6dfc2696adce6bff8808ca96bbd46596b6'
INTEGRATIONS = {'Readiness':'Passed','PackagedReadiness':'Passed','Build':'Passed','Tests':'Passed','OracleUnavailableReport':'Expected failure recorded','HybridOutput':'Hello World','Incremental':'Passed','DisabledPreprocessing':'Expected failure','CleanRebuild':'Passed','PackageConsumer':'Passed','LocalTool':'Passed'}
DELTA = {('rpc','call',4),('rpc','call',5),('erpc','call',4),('erpc','call',5),('erpc','execute_call',4),('proc_lib','spawn',2),('proc_lib','spawn',4),('proc_lib','spawn_link',2),('proc_lib','spawn_link',4),('proc_lib','spawn_opt',3),('proc_lib','spawn_opt',5)}
def read(p): return json.loads(Path(p).read_bytes())
def sha(p): return hashlib.sha256(Path(p).read_bytes()).hexdigest().upper()
def rows(p):
 v=read(p)
 return v['Files'] if isinstance(v,dict) else v
def file_at(root,name):
 p=(root/name).resolve(strict=True);assert p.is_relative_to(root) and p.is_file(),name
 return p
def exact(root,manifest):
 seen=set()
 for r in rows(manifest):
  assert r['Path'] not in seen;seen.add(r['Path']);p=file_at(root,r['Path'])
  assert p.stat().st_size==r['Bytes'] and sha(p)==r['Sha256'],r['Path']
 return seen
def save(p,value):
 with p.open('x',encoding='utf-8',newline='\n') as f:json.dump(value,f,indent=2);f.write('\n')
def completed(c,d,guard_sha):
 assert sha(d/'completion-guard.json')==guard_sha.upper()
 g=read(d/'completion-guard.json');f=read(d/'source-freeze.json')
 # Draft producer contract: reconcile these exact fields with final Rec schema BEFORE execution.
 assert g['Status']=='PASS_ACTUAL_ISOLATED340_FULL' and g['SingleUninterruptedFullPass'] is True
 assert f.get('ParentHead',f.get('ParentSha'))==PARENT
 assert g['SourceFreezeSha256']==sha(d/'source-freeze.json') and g['SourceClosureSha256']==sha(d/'source-resource-closure.json')
 e=read(d/'full-exit.json');assert e['ExitCode']==0 and e['Settled'] and not e['TimedOut']
 assert g['FullExitSha256']==sha(d/'full-exit.json') and g['TestReportSha256']==sha(d/'tests.json')
 t=read(d/'tests.json');names=[r['Name'] for r in t['Tests']]
 assert t['Failed']==0 and t['Passed']==len(names)==len(set(names)) and all(r['Status']=='Passed' for r in t['Tests'])
 assert g['Passed']==t['Passed'] and g['Failed']==0 and read(d/'integration-results.json')==g['IntegrationResults']==INTEGRATIONS
 assert g['Registry']==587 and g['ProcLibMfas']==39
 assert g['PackageResourceManifestSha256']==sha(d/'package-resources.json') and len(rows(d/'package-resources.json'))==6
 exact(c,d/'source-freeze.json');exact(c,d/'source-resource-closure.json');exact(c,d/'package-resources.json')
 return g,f,t
