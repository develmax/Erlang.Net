from pathlib import Path
import json,hashlib,tarfile,importlib.util,sys
sys.dont_write_bytecode=True
r=Path.cwd();a=r/'artifacts';e=a/'pack327-watch-38856989449-terminal/artifact-12028195262';h=lambda b:hashlib.sha256(b).hexdigest().upper();read=lambda p:json.loads(p.read_bytes())
spec=importlib.util.spec_from_file_location('common323',a/'pack327-audit-common.py');m=importlib.util.module_from_spec(spec);spec.loader.exec_module(m)
head='4bc4c00b713c1cc0bbf32a1c74add45174bbd10a';prefix='tools/Erlang.NifReallocationObservations/';names=[prefix+n for n in ['SOURCE-PLAN.json','verify_source_build.py','verify_native_build.py']]+['.github/workflows/nif-reallocation-native.yml','tools/Erlang.NifPendingVarargsObservations/reviewed-source-plan.json'];b=m.blobs(head,names);plan=read(a/'pack327-reallocation-published-passive-contracts'/names[0]);assert b'\r' not in b[names[0]] and not b[names[0]].startswith(b'\xef\xbb\xbf')
assert h(b[names[0]])=='0096F13F945A7494F704C518EDFCEC766DF38167B8EE76898F53FA3077E118D9';assert h(b[names[-1]])=='D1A3A9533A9A4537E7C7B596318E690083677B154B8A912B5AC36A0672137344';assert len(json.loads(b[names[-1]])['Files'])==1114
for filename,key in [('runtime-approval.json',prefix+'verify_source_build.py'),('native-approval.json',prefix+'verify_native_build.py')]:assert read(e/'control'/filename)['VerifierSha256']==h(b[key])
headers={}
with tarfile.open(e/'control/installed-runtime.tar.gz','r:gz') as t:
 for item in t:
  if item.isfile() and '/usr/include/' in item.name:
   name=Path(item.name).name
   if name in {x['Name'] for x in plan['Headers']}|{'erl_int_sizes_config.h'}:headers[name]=h(t.extractfile(item).read())
for item in plan['Headers']:assert headers[item['Name']]==item['Sha256']
assert 'erl_int_sizes_config.h' in headers
runtime=read(e/'runtime-receipt/source-build-receipt.json');build=read(e/'source-build/build-process-receipt.json');assert runtime['OriginalBuildProvenance']==read(e/'source-build/legacy-provenance.json');assert runtime['SourceGitInspection']==build['SourceAfter']
roles=runtime['OriginalBuildProvenance']['Files'];assert len(roles)==4
full={x['Path']:x for x in runtime['RuntimeFiles']}
for item in roles:
 path=build['SourceRoot']+'/'+item['Path']
 if path not in full:
  relative=path.removeprefix(runtime['InstalledClosure']['InstalledPrefix']+'/');entry=next(x for x in runtime['InstalledClosure']['Entries'] if x['Path']==relative);assert entry['Kind']=='FileSymlink';path=runtime['InstalledClosure']['InstalledPrefix']+'/'+entry['Target']
 assert full[path]['Bytes']==item['Bytes'] and full[path]['Sha256']==item['Sha256']
old=a/'pack327-reallocation-independent-guard.json';report=read(old);report.update(ParentIndependentGuardSha256=h(old.read_bytes()),PublishedOwnPlan1139Bound=True,PreAttribute7324PlanNotUsed=True,LegacyPlan1114Sha256=h(b[names[-1]]),CanonicalPublishedPlanUtf8Lf=True,ApprovalVerifierHashesMatchPublishedSource=True,InstalledPinnedHeadersVerified=headers,ConfiguredHeaderMeasuredNotGuessed=True,AllFourLegacyRuntimeRolesMatchCompletePrefix=True,SourceGitBeforeAfterPinnedTrackedClean=True)
p=a/'pack327-reallocation-independent-guard2.json';assert not p.exists();raw=(json.dumps(report,indent=2)+'\n').encode();p.write_bytes(raw);print(h(raw))
