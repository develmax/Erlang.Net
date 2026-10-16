"""Promote reviewed corrected321 onto exact failed Main1; source-only authoring, Root executes later."""
from pathlib import Path
import argparse,hashlib,json,subprocess
p=argparse.ArgumentParser()
for name in ['freeze','source-peer']:p.add_argument('--'+name,required=True,type=Path)
for name in ['freeze-sha256','closure-sha256','source-peer-sha256']:p.add_argument('--'+name,required=True)
a=p.parse_args();r=Path.cwd().resolve();old=r/'artifacts/root-part321-main1';out=r/'artifacts/root-part321-main2';fp=a.freeze.resolve();cl=fp.parent/'source-resource-closure.json';c=fp.parent/'candidate';peer=a.source_peer.resolve();sha=lambda z:hashlib.sha256(z).hexdigest().upper();read=lambda p:json.loads(p.read_bytes());encode=lambda x:(json.dumps(x,indent=2)+'\n').encode()
def exact(path,expected):
 z=path.read_bytes();assert len(expected)==64 and sha(z)==expected.upper(),str(path);return z
assert not out.exists() and fp.is_relative_to(r/'artifacts') and peer.is_relative_to(r/'artifacts')
oldfp=old/'source-freeze.json';oldcl=old/'source-resource-closure.json';exact(oldfp,'872916DEFB7170CD988197A03EBB4B88859D682A7EFB42A784E7CEBEB1FF7D93');exact(oldcl,'E70A3810D719536D6B94D3D92C1E5236C41AE86B858A12A5901A5BE8C86F4FD7')
failed=old/'first-full-failure-receipt.json';exact(failed,'DB948C4DC131A6AE35B542FAB720A5D67E770DDFCD1B853C256C53BBE714DFE4');failure=read(failed);assert (failure['Passed'],failure['Failed'],failure['SourceRowsRehashed'])==(6677,1,2316)
for item in [failure['SourceFreeze'],failure['SourceClosure'],failure['FullLog'],failure['FullExit'],failure['ImmutableTests'],failure['RawRegeneratedMfas']]:
 z=exact(r/item['Path'],item['Sha256']);assert len(z)==item['Bytes']
assert read(old/'full-exit.json')['ExitCode']==1
ft=read(old/'first-full-results-snapshot/tests.json');assert (ft['Passed'],ft['Failed'])==(6677,1)
assert [x['Name'] for x in ft['Tests'] if x['Status']!='Passed']==['runtime/distribution-raw-signal-link-unlink-exit-owned-process-signals']
oldfreeze=read(oldfp);oldrows=read(oldcl)['Files'];assert len(oldrows)==2316;oldmap={x['Path']:x for x in oldrows};oldowned={x['Path']:x for x in oldfreeze['Files']};assert len(oldowned)==24
for row in oldrows:
 z=exact(r/row['Path'],row['Sha256']);assert len(z)==row['Bytes']
manifest=r/'docs/supported-mfas.json';rawmanifest=(old/'first-full-results-snapshot/raw-supported-mfas.json').read_bytes();assert manifest.read_bytes()==rawmanifest,'Only preserved raw regenerated post-failure registry is permitted'
assert subprocess.check_output(['git','--no-optional-locks','rev-parse','HEAD']).decode().strip()==oldfreeze['ParentHead']=='ab2e7b5c213460188e80e45111068a55bd4b4d69'
allowed=set(oldowned)|{'docs/supported-mfas.json'}
for command in [['git','--no-optional-locks','diff','--name-only'],['git','--no-optional-locks','diff','--cached','--name-only']]:assert set(subprocess.check_output(command).decode().splitlines())<=allowed
exact(fp,a.freeze_sha256);exact(cl,a.closure_sha256);f=read(fp);rows=read(cl)['Files'];owned={x['Path']:x for x in f['Files']}
assert (f['SourcePaths'],f['CSharpPaths'],f['SourceClosurePaths'],len(rows))==(24,9,2316,2316) and f['ParentHead']==oldfreeze['ParentHead'];assert f['FinalSourceClosureSha256']==a.closure_sha256.upper();assert set(owned)==set(oldowned) and {x['Path'] for x in rows}==set(oldmap)
exact(peer,a.source_peer_sha256);v=read(peer);assert str(v.get('Status','')).startswith('PASS') or v.get('Passed') is True
assert a.freeze_sha256.upper() in json.dumps(v) and a.closure_sha256.upper() in json.dumps(v)
permitted={'src/Erlang.Runtime/ProcessRuntime.DistributionLinks.cs','tests/Erlang.Tests/DistributionGuestLinkHostChecks.cs','tools/Erlang.NifPendingVarargsObservations/reviewed-source-plan.json','.github/workflows/nif-pending-native.yml','tools/Erlang.NifReallocationObservations/SOURCE-PLAN.json','.github/workflows/nif-reallocation-native.yml'};changed=[]
for row in rows:
 z=exact(c/row['Path'],row['Sha256']);assert len(z)==row['Bytes']
 if row['Path'] in owned:
  item=owned[row['Path']];before=oldowned[row['Path']];assert (item['Added'],item['ParentSha256'])==(before['Added'],before['ParentSha256']),row['Path']
 if row!=oldmap[row['Path']]:assert row['Path'] in permitted;changed.append(row['Path'])
assert {'src/Erlang.Runtime/ProcessRuntime.DistributionLinks.cs','tests/Erlang.Tests/DistributionGuestLinkHostChecks.cs'}<=set(changed)
assert sorted(x['Inputs'] for x in f['Plans'])==[1103,1128]
# All failure/old-source/new-source/parent/peer/registry checks settle before any Main write.
out.mkdir();(out/'raw-registry-before-main2.json').write_bytes(rawmanifest)
for item in f['Files']:
 path=r/item['Path'];path.parent.mkdir(parents=True,exist_ok=True);path.write_bytes((c/item['Path']).read_bytes())
for row in rows:assert len((r/row['Path']).read_bytes())==row['Bytes'] and sha((r/row['Path']).read_bytes())==row['Sha256']
(out/'source-resource-closure.json').write_bytes(cl.read_bytes());f.update(Status='ROOT_PROMOTED_CORRECTED_CONNECTED_LINK321_AFTER_PRESERVED_MAIN1_FAILURE_PENDING_FULL',CandidateDirectory=str(r),ExpectedFullPassed=6678,SourceClosurePaths=2316,AuthorFinalFreeze={'Path':fp.relative_to(r).as_posix(),'Sha256':sha(fp.read_bytes())},IndependentPeer={'Path':peer.relative_to(r).as_posix(),'Sha256':sha(peer.read_bytes())},IndependentPeerRole='FINAL_SOURCE_COMPOSITION_PASSIVE_REVIEW',PreviousFailedMain1={'Path':failed.relative_to(r).as_posix(),'Sha256':sha(failed.read_bytes())},PreviousFailedMain1FreezeSha256=sha(oldfp.read_bytes()),PreviousFailedMain1ClosureSha256=sha(oldcl.read_bytes()),RevisionChangedSourcePaths=changed,RootPromotionScriptSha256=sha(Path(__file__).read_bytes()))
(out/'source-freeze.json').write_bytes(encode(f));print(json.dumps({'Main2FreezeSha256':sha((out/'source-freeze.json').read_bytes()),'ExpectedFullPassed':6678,'SourceClosurePaths':2316,'PreviousFailureSha256':sha(failed.read_bytes())}))
