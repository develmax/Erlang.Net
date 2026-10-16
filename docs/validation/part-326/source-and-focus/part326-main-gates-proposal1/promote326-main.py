"""Root-executable promotion proposal. Author does not execute this Main-writing script."""
from pathlib import Path
import argparse, hashlib, json, subprocess
p=argparse.ArgumentParser()
for key in ('freeze','source-peer','focus-guard','red-guard'):
    p.add_argument('--'+key,required=True,type=Path);p.add_argument('--'+key+'-sha256',required=True)
p.add_argument('--closure-sha256',required=True)
p.add_argument('--focus-source-freeze-sha256',required=True)
p.add_argument('--red-source-freeze-sha256',required=True)
a=p.parse_args();r=Path.cwd().resolve();out=r/'artifacts/root-part326-main1';fp=a.freeze.resolve();cl=fp.parent/'source-resource-closure.json';candidate=fp.parent/'candidate'
sha=lambda z:hashlib.sha256(z).hexdigest().upper()
read=lambda path:json.loads(path.read_bytes())
def exact(path,digest):
    z=path.read_bytes();assert sha(z)==digest.upper(),str(path);return z
assert not out.exists() and fp.is_relative_to(r/'artifacts') and a.source_peer.resolve().is_relative_to(r/'artifacts')
head='2bd148150ddf2cca49dd2c797d05de3260bf663c'
assert subprocess.check_output(['git','--no-optional-locks','rev-parse','HEAD']).decode().strip()==head
for command in (['git','--no-optional-locks','diff','--name-only'],['git','--no-optional-locks','diff','--cached','--name-only']):
    assert not subprocess.check_output(command).decode().strip(),'Published Main must be clean before promotion'
parentcl=r/'artifacts/root-part325-main1/source-resource-closure.json'
exact(parentcl,'44C983D68C6C2BBAF910E1369E23050AF42686FF1EFB6E4BA15C0C34D3FD3848')
parent={x['Path']:x for x in read(parentcl)['Files']};assert len(parent)==2366
for x in parent.values():
    assert len(exact(r/x['Path'],x['Sha256']))==x['Bytes']
exact(fp,a.freeze_sha256);exact(cl,a.closure_sha256);f=read(fp);rows=read(cl)['Files'];owned={x['Path']:x for x in f['Files']}
assert (f['SourcePaths'],f['CSharpPaths'],f['SourceClosurePaths'],len(rows))==(13,8,2370,2370)
assert f['ParentHead']==head and f['FinalSourceClosureSha256']==a.closure_sha256.upper()
assert len(owned)==13 and sum(k.endswith('.cs') for k in owned)==8
exact(a.source_peer,a.source_peer_sha256);peer=read(a.source_peer)
assert str(peer.get('Status','')).startswith('PASS') or peer.get('Passed') is True
assert a.freeze_sha256.upper() in json.dumps(peer) and a.closure_sha256.upper() in json.dumps(peer)
assert sorted(x['Inputs'] for x in f['Plans'])==[1109,1134]
assert set(owned)-set(parent)=={x['Path'] for x in f['Files'] if x['Added']}
# Independently approved actual focus and semantic RED are mandatory before promotion.
for path in (a.focus_guard,a.red_guard):assert path.resolve().is_relative_to(r/'artifacts')
exact(a.focus_guard,a.focus_guard_sha256);fg=read(a.focus_guard)
assert fg['Passed'] is True and fg['Status']=='PASS_ACTUAL326_TARGET_FOURTEEN_GENERATED_AND_SIX_HOST_GROUPS'
assert fg['GreenSourceFreezeSha256']==a.focus_source_freeze_sha256.upper() and fg['SourceClosureFiles']==2370
assert fg['TargetBuildPassed'] is True and (fg['GeneratedCasesPassed'],fg['HostGroupsPassed'])==(14,6)
exact(a.red_guard,a.red_guard_sha256);rg=read(a.red_guard)
assert rg['Passed'] is True and rg['Status']=='PASS_ACTUAL326_IDENTICAL_HELPER_HISTORICAL325_VALID_OPTION_REJECTION'
assert rg['RedSourceFreezeSha256']==a.red_source_freeze_sha256.upper() and rg['ActualFailure']=='public_spawn_opt_badarg'
for key in ('HistoricalProductionAndProgramExactPublished325','SameFormattedHostHelperRaw','TargetBuildPassed','OwnedProcessSettled'):assert rg[key] is True
for proof in (fg,rg):
 for binding in proof['Bindings']:exact(r/binding['Path'],binding['Sha256'])
for x in rows:
    assert len(exact(candidate/x['Path'],x['Sha256']))==x['Bytes']
    if x['Path'] not in owned:assert x==parent[x['Path']],x['Path']
for path,x in owned.items():
    assert x['Added']==(path not in parent)
    assert x['ParentSha256']==(parent[path]['Sha256'] if path in parent else None)
manifest=r/'docs/supported-mfas.json';prior=read(manifest)
assert len(prior)==509 and sum(x['Module']=='erlang' for x in prior)==351
# All source, peer, parent and registry preflight checks settle before first Main write.
out.mkdir();(out/'registry-before-full.json').write_bytes(manifest.read_bytes())
for path in owned:
    target=r/path;target.parent.mkdir(parents=True,exist_ok=True);target.write_bytes((candidate/path).read_bytes())
for x in rows:assert len(exact(r/x['Path'],x['Sha256']))==x['Bytes']
(out/'source-resource-closure.json').write_bytes(cl.read_bytes())
f.update(Status='ROOT_PROMOTED_EXPLICIT_SPAWN_ASYNC_DIST326_PENDING_ACTUAL_FULL',CandidateDirectory=str(r),ExpectedFullPassed=6708,
    AuthorFinalFreeze=dict(Path=fp.relative_to(r).as_posix(),Sha256=sha(fp.read_bytes())),
    IndependentPeer=dict(Path=a.source_peer.resolve().relative_to(r).as_posix(),Sha256=sha(a.source_peer.read_bytes())),
    IndependentPeerRole='FINAL_SOURCE_COMPOSITION_PASSIVE_REVIEW',ActualFocusGuard=dict(Path=a.focus_guard.resolve().relative_to(r).as_posix(),Sha256=sha(a.focus_guard.read_bytes())),ActualHistoricalRedGuard=dict(Path=a.red_guard.resolve().relative_to(r).as_posix(),Sha256=sha(a.red_guard.read_bytes())),RootPromotionScriptSha256=sha(Path(__file__).read_bytes()))
(out/'source-freeze.json').write_bytes((json.dumps(f,indent=2)+'\n').encode())
print(json.dumps(dict(MainFreezeSha256=sha((out/'source-freeze.json').read_bytes()),SourceClosureSha256=a.closure_sha256.upper(),ExpectedFullPassed=6708)))
