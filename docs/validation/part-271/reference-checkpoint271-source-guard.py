from pathlib import Path
import hashlib,json,subprocess,sys
head=sys.argv[1];baseline='f26e1a15320553eb9d725764ec2260f3c262e1da'
sha=lambda b:hashlib.sha256(b).hexdigest().upper()
guard=json.loads(Path('artifacts/part271-main-import-guard.json').read_text());assert guard['Imported']==len(guard['Files'])==33
for row in guard['Files']:
 working=Path(row['Path']).read_bytes()
 assert sha(working)==row['AfterSha256'],row['Path']
 committed=subprocess.check_output(['git','show',head+':'+row['Path']])
 assert committed.decode('utf-8').replace('\r\n','\n')==working.decode('utf-8').replace('\r\n','\n'),row['Path']
 if row['Path'].endswith('.erl'):assert committed==working,row['Path']
# Current ordinary270 corpus remains reusable: no existing runtime/compiler/serialization
# source, differential case, resource fixture, build-property or pinned-baseline changes.
scopes=['src','tools/Erlang.Differential','tools/Erlang.NodeIdentityContrasts','tests/Erlang.Tests/Resources','Directory.Build.props','global.json','reference-baseline.json']
def tree(ref):
 data=subprocess.check_output(['git','ls-tree','-r','-z',ref,'--']+scopes)
 rows={}
 for row in data.split(b'\0'):
  if not row:continue
  metadata,path=row.split(b'\t',1);rows[path.decode()]=metadata
 return rows
before=tree(baseline);after=tree(head)
for path,blob in before.items():assert after.get(path)==blob,path
workflow=subprocess.check_output(['git','show',head+':.github/workflows/distribution-packet-contrasts.yml']).decode()
assert workflow.count('git clone --depth 1 --branch OTP-29.1.1')==1
assert workflow.count('dotnet run --project tools/Erlang.DistributionPacketContrasts')==1 and workflow.count('dotnet run --project tools/Erlang.MixedMapIdentityContrasts')==1
assert 'tools/Erlang.NodeIdentityContrasts --' not in workflow and 'continue-on-error' not in workflow
assert "steps.packet-tool.outcome == 'success'" in workflow and "steps.mixed-tool.outcome == 'success'" in workflow
print('Committed27133source bindings, shared pinnedbuild/twofreshVMs and unchanged ordinary270 existing corpus inputs verified')
