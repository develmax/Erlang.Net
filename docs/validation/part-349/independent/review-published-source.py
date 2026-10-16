from pathlib import Path
import hashlib,json,subprocess
root=Path(__file__).resolve().parents[2]
head='83dde925f0b15707d0406d6028a85b9877764c3c'
pin='ad05823719d77c8faee87348ea39513d4e2f99c5'
out=Path(__file__).resolve().parent
sha=lambda b:hashlib.sha256(b).hexdigest().upper()
def blob(repo,revision,path):
 return subprocess.run(['git','-C',str(repo),'show',revision+':'+path],capture_output=True,check=True).stdout
manifest_path='tools/Erlang.ProcLibRealPeerObservations/source-manifest.json'
manifest_bytes=blob(root,head,manifest_path)
assert sha(manifest_bytes)=='C1B3C8BADDCC519B48E557628522E42E5EB31C2CF4055DC343CCBD436EEECD16'
manifest=json.loads(manifest_bytes)
rows=[]
for row in manifest['Files']+manifest['ExternalDependencies']+[manifest['Workflow']]:
 path=row['Path'] if row in manifest['ExternalDependencies'] or row is manifest['Workflow'] else 'tools/Erlang.ProcLibRealPeerObservations/'+row['Path']
 b=blob(root,head,path)
 assert len(b)==row['Bytes'] and sha(b)==row['Sha256'],path
 rows.append({'Path':path,'Bytes':len(b),'Sha256':sha(b)})
anchors=[]
for row in manifest['PinnedSourceAnchors']:
 b=blob(root.parent/'otp',pin,row['Path'])
 assert len(b)==row['Bytes'] and sha(b)==row['Sha256'],row['Path']
 anchors.append(row)
info=blob(root.parent/'otp',pin,'erts/emulator/beam/erl_bif_info.c')
init=blob(root.parent/'otp',pin,'erts/emulator/beam/erl_init.c')
assert b'val = is_local_fun(funp) ? erts_init_process_id : am_undefined;' in info
assert b'erts_init_process_id = erl_first_process_otp(init, boot_argc, boot_argv);' in init
report={'Status':'PASS_PUBLISHED346_SOURCE_AND_PINNED_ANCHORS','Head':head,'PinnedOtpCommit':pin,'ManifestSha256':sha(manifest_bytes),'PublishedSourceFiles':rows,'PinnedSourceAnchors':anchors,'FunInfoPidSource':{'Path':'erts/emulator/beam/erl_bif_info.c','Line':3943,'Sha256':sha(info),'Contract':'local fun_info(pid) returns erts_init_process_id'},'InitPidSource':{'Path':'erts/emulator/beam/erl_init.c','Line':2540,'Sha256':sha(init),'Contract':'erts_init_process_id assigned init boot process'},'CodeExecuted':False,'Sdk':False,'Network':False}
target=out/'published-source-guard.json'
assert not target.exists()
target.write_text(json.dumps(report,indent=2)+'\n',encoding='utf-8')
print(sha(target.read_bytes()))
