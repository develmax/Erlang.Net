from pathlib import Path
import hashlib, json, runpy, subprocess, sys
sys.dont_write_bytecode = True
r=Path.cwd(); a=r/'artifacts'; sha=lambda b:hashlib.sha256(b).hexdigest().upper()
p=a/'allocator-memory309-original18-proposal1'; f=json.loads((p/'source-freeze.json').read_bytes())
assert f['ExecutionPlanSha256']=='C73AFABAF43F98F81BD94C52788189429F85931C3D7228C8C8346C38FCEDD0F5'
assert (a/'allocator309-beam310-independent-source-review.json').is_file()
w=[]
for row in f['PromotedFiles']:
 target=r/row['Path']; data=(p/'overlay'/row['Path']).read_bytes()
 assert not target.exists() and len(data)==row['Bytes'] and sha(data)==row['Sha256'], row['Path']
 w.append((target,data))
assert len(w)==13
for row in f['UnchangedPublishedHelpers']:
 assert sha((r/row['Path']).read_bytes())==row['Sha256'], row['Path']
for target,data in w:
 target.parent.mkdir(parents=True,exist_ok=True); target.write_bytes(data)
attrs=r/'.gitattributes'; old=attrs.read_bytes()
extra=b'tools/Erlang.AllocatorMemoryObservations/** -text whitespace=trailing-space,space-before-tab,cr-at-eol\n.github/workflows/allocator-memory-original-observations.yml -text whitespace=trailing-space,space-before-tab,cr-at-eol\n'
assert b'tools/Erlang.AllocatorMemoryObservations/' not in old
attrs.write_bytes(old+extra)
plan=r/'tools/Erlang.NifPendingVarargsObservations/reviewed-source-plan.json'; prior=plan.read_bytes()
assert sha(prior)=='64267EDED370D35DEA5AB176E39A02370F461072784678C0A922C507484F5F56'
v=json.loads(prior); helper=runpy.run_path(str(plan.parent/'run-reviewed.py'))
inputs=helper['reviewed_inputs'](r,plan); before={row['Path']:row for row in v['Files']}
assert len(before)==1009 and len(inputs)==1012
assert inputs-before.keys()=={'src/Erlang.Runtime/DistributionStatisticsNames.cs','src/Erlang.Runtime/HostDistributionBufferStorage.cs','src/Erlang.Runtime/RuntimeDistributionPending.Statistics.cs'}
v['Files']=[]
for name in sorted(inputs):
 row=helper['record'](r/name,r,before.get(name,{}).get('HashMode','TextLF')); row['Path']=name; v['Files'].append(row)
data=(json.dumps(v,indent=2)+'\n').encode(); workflow=r/'.github/workflows/nif-pending-native.yml'; b=workflow.read_bytes()
assert b.count(sha(prior).encode())==1
plan.write_bytes(data); workflow.write_bytes(b.replace(sha(prior).encode(),sha(data).encode()))
mapping=json.loads((a/'distribution-owner306-combined-formatted2/promotion-map.json').read_bytes())
names={row['Path'] for row in mapping['Paths']}|{row['Path'] for row in f['PromotedFiles']}|{'.gitattributes',plan.relative_to(r).as_posix(),workflow.relative_to(r).as_posix()}
assert len(names)==38
rows=[]
for name in sorted(names):
 b=(r/name).read_bytes(); prev=subprocess.run(['git','show','HEAD:'+name],capture_output=True)
 rows.append({'Path':name,'Bytes':len(b),'Sha256':sha(b),'ParentGitSha256':sha(prev.stdout) if prev.returncode==0 else None})
out=a/'root-part306-final-source-freeze.json'; assert not out.exists()
record={'Status':'EXACT_SOURCE_FROZEN_FULL_PENDING','ParentHead':'0494375635061b4952d40a3c7667234db321a931','SourcePaths':38,'CSharpPaths':22,'ReviewedSourceInputs':1012,'ReviewedSourcePlanSha256':sha(data),'ExpectedFullPassed':6627,'ExpectedRegistry':504,'ExpectedErlangPresence':346,'AddedHostNames':['runtime/distribution-statistics-accounted-storage-generation','runtime/nif-prepared-image-owner-without-global-exports'],'PublicMfaAdded':'erlang:dist_get_stat/1','PublicLoadNifActivated':False,'AllocatorOriginalExecuted':False,'Files':rows}
out.write_text(json.dumps(record,indent=2)+'\n',encoding='utf-8')
print(json.dumps({'FreezeSha256':sha(out.read_bytes()),'PlanSha256':sha(data),'SourcePaths':len(rows)}))
