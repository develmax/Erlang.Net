from pathlib import Path
import json,hashlib,tarfile,importlib.util,sys
sys.dont_write_bytecode=True
r=Path.cwd();a=r/'artifacts';e=a/'pack323-watch-38843451390-terminal/artifact-12022267898';h=lambda b:hashlib.sha256(b).hexdigest().upper();read=lambda p:json.loads(p.read_bytes())
spec=importlib.util.spec_from_file_location('common323',a/'pack323-audit-common.py');m=importlib.util.module_from_spec(spec);spec.loader.exec_module(m)
head='3785a8916b6469c7e7ada4838786b2526dad7f9e';prefix='tools/Erlang.NifReallocationObservations/';names=[prefix+n for n in ['SOURCE-PLAN.json','verify_source_build.py','verify_native_build.py']]+['.github/workflows/nif-reallocation-native.yml','tools/Erlang.NifPendingVarargsObservations/reviewed-source-plan.json'];b=m.blobs(head,names);plan=read(a/'pack323-reallocation-published-passive-contracts'/names[0]);assert b'\r' not in b[names[0]] and not b[names[0]].startswith(b'\xef\xbb\xbf')
assert h(b[names[0]])=='2A31DD136547D9F9395930CF6EC5C02CD28CB426C94BE385757F6696E02D89D5';assert h(b[names[-1]])=='D0E9EF6F2103042BDB62694402E76C5A592514AE843E43701BFEB991BAB421E6';assert len(json.loads(b[names[-1]])['Files'])==1108
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
 path=build['SourceRoot']+'/'+item['Path'];assert full[path]['Bytes']==item['Bytes'] and full[path]['Sha256']==item['Sha256']
old=a/'pack323-reallocation-independent-guard.json';report=read(old);report.update(ParentIndependentGuardSha256=h(old.read_bytes()),RepairedPublishedOwnPlan1133Bound=True,PreAttribute7324PlanNotUsed=True,LegacyPlan1108Sha256=h(b[names[-1]]),CanonicalPublishedPlanUtf8Lf=True,ApprovalVerifierHashesMatchPublishedSource=True,InstalledPinnedHeadersVerified=headers,ConfiguredHeaderMeasuredNotGuessed=True,AllFourLegacyRuntimeRolesMatchCompletePrefix=True,SourceGitBeforeAfterPinnedTrackedClean=True)
p=a/'pack323-reallocation-independent-guard2.json';assert not p.exists();raw=(json.dumps(report,indent=2)+'\n').encode();p.write_bytes(raw);print(h(raw))
