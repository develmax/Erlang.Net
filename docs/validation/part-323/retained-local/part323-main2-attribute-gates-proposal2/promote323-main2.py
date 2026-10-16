"""Root-only revision-aware attr promotion; source proposal, author does not execute."""
from pathlib import Path
import argparse,hashlib,json,subprocess,re
p=argparse.ArgumentParser()
for key in ('freeze','source-peer','attribute-proof'):
    p.add_argument('--'+key,required=True,type=Path);p.add_argument('--'+key+'-sha256',required=True)
p.add_argument('--closure-sha256',required=True)
a=p.parse_args();r=Path.cwd().resolve();out=r/'artifacts/root-part323-main2';old=r/'artifacts/root-part323-main1'
sha=lambda b:hashlib.sha256(b).hexdigest().upper();read=lambda p:json.loads(p.read_bytes());encode=lambda x:(json.dumps(x,indent=2)+'\n').encode()
def exact(path,h):
    z=path.read_bytes();assert sha(z)==h.upper(),str(path);return z
assert not out.exists()
fp=a.freeze.resolve();cl=fp.parent/'source-resource-closure.json';candidate=fp.parent/'candidate'
for path in (fp,a.source_peer.resolve(),a.attribute_proof.resolve()):assert path.is_relative_to(r/'artifacts')
exact(old/'source-freeze.json','E0DB86F611D1DC8FCF9004F1163398C6C13F3AA3198F2B76F39CEF84F48D1E28')
exact(old/'source-resource-closure.json','B395DACAD8CEC82498A91B59342CC1217A66FE4F4ACA18AF1B8BC8EF63F6ABB7')
exact(old/'full-guard.json','ADD6EB861985721B45AB819E9314D67F2A49A1F90F5B05DBF7D088936AE46204')
native=r/'artifacts/root-part323-main-windows-native1/guard.json'
exact(native,'D1AE74EF1EA498F1376986156665798F8532614D9D1A04D09B63D218FDAE4556')
oldf=read(old/'source-freeze.json');oldrows=read(old/'source-resource-closure.json')['Files'];oldmap={x['Path']:x for x in oldrows}
assert len(oldmap)==2349 and read(old/'full-guard.json')['Passed']==6685 and read(native)['FullPassed']==6685
assert read(old/'full-exit.json')['ExitCode']==0
for x in oldrows:assert len(exact(r/x['Path'],x['Sha256']))==x['Bytes']
git=lambda *args:subprocess.check_output(['git','--no-optional-locks',*args],cwd=r)
assert git('rev-parse','HEAD').decode().strip()=='e2cb95a9658584a0985b4e8c037241264d576aa7'==oldf['ParentHead']
allowed={x['Path'] for x in oldf['Files']}|{'docs/supported-mfas.json'}
assert set(git('diff','--name-only').decode().splitlines())<=allowed
assert not git('diff','--cached','--name-only').strip()
exact(fp,a.freeze_sha256);exact(cl,a.closure_sha256);exact(a.source_peer,a.source_peer_sha256);exact(a.attribute_proof,a.attribute_proof_sha256)
f=read(fp);rows=read(cl)['Files'];owned={x['Path']:x for x in f['Files']}
assert (f['SourcePaths'],f['CSharpPaths'],f['SourceClosurePaths'],len(rows))==(30,13,2349,2349)
assert f['ParentHead']==oldf['ParentHead'] and f['FinalSourceClosureSha256']==a.closure_sha256.upper()
assert set(owned)=={x['Path'] for x in oldf['Files']} and {x['Path'] for x in rows}==set(oldmap)
peer=read(a.source_peer);assert peer.get('Passed') is True or str(peer.get('Status','')).startswith('PASS')
assert a.freeze_sha256.upper() in json.dumps(peer) and a.closure_sha256.upper() in json.dumps(peer)
changed=[]
for x in rows:
    assert len(exact(candidate/x['Path'],x['Sha256']))==x['Bytes']
    if x!=oldmap[x['Path']]:changed.append(x['Path'])
allowed_delta={'.gitattributes','tools/Erlang.NifReallocationObservations/SOURCE-PLAN.json','.github/workflows/nif-reallocation-native.yml'}
assert set(changed)==allowed_delta and len(changed)==3
for x in f['Files']:
    prior=next(y for y in oldf['Files'] if y['Path']==x['Path'])
    assert (x['Added'],x['ParentSha256'])==(prior['Added'],prior['ParentSha256'])
resource='tests/Erlang.Tests/Resources/Beam/guest_connected_bang322.provenance.json'
before=(r/'.gitattributes').read_bytes();after=(candidate/'.gitattributes').read_bytes()
assert after.startswith(before) and after[len(before):].strip()==(resource+' binary').encode()
assert (resource+' binary').encode() not in before.splitlines()
assert sum(line.strip()==(resource+' binary').encode() for line in after.splitlines())==1
assert (candidate/resource).read_bytes()==(r/resource).read_bytes()
assert sorted(x['Inputs'] for x in f['Plans'])==[1108,1133]
legacy=next(x for x in f['Plans'] if x['Inputs']==1108)
assert legacy==next(x for x in oldf['Plans'] if x['Inputs']==1108)
assert (candidate/legacy['Path']).read_bytes()==(r/legacy['Path']).read_bytes()
own=next(x for x in f['Plans'] if x['Inputs']==1133)
previous=read(r/own['Path']);updated=read(candidate/own['Path'])
assert {k:v for k,v in previous.items() if k!='Files'}=={k:v for k,v in updated.items() if k!='Files'}
oldinputs={x['Path']:x for x in previous['Files']};newinputs={x['Path']:x for x in updated['Files']}
assert len(newinputs)==1133 and set(oldinputs)==set(newinputs)
assert [k for k in oldinputs if oldinputs[k]!=newinputs[k]]==['.gitattributes']
for x in updated['Files']:
    z=(candidate/x['Path']).read_bytes();assert x['HashMode'] in ('TextLF','BinaryExact')
    if x['HashMode']=='TextLF':z=z.replace(b'\r\n',b'\n')
    assert len(z)==x['Bytes'] and sha(z)==x['Sha256']
raw=(candidate/own['Path']).read_bytes();assert sha(raw)==own['Sha256'] and b'\r' not in raw
beforeworkflow=(r/own['Workflow']).read_bytes()
expectedworkflow,n=re.subn(rb'(?m)^([ \t]*REVIEWED_REALLOCATION_PLAN_SHA256: )[A-F0-9]{64}[ \t]*\r?$',lambda m:m[1]+own['Sha256'].encode(),beforeworkflow)
assert n==1 and (candidate/own['Workflow']).read_bytes()==expectedworkflow
tests=read(r/'artifacts/tests.json');assert tests['Passed']==6685 and tests['Failed']==0
assert len(read(r/'artifacts/integration-results.json'))==11
# Preserve successful pre-attribute evidence before any subsequent full run overwrites shared reports.
out.mkdir();snapshot=out/'successful-pre-attribute-main1';snapshot.mkdir()
for source,name in [(r/'artifacts/tests.json','tests.json'),(r/'artifacts/integration-results.json','integration-results.json'),(r/'docs/supported-mfas.json','derived-supported-mfas.json')]:
    (snapshot/name).write_bytes(source.read_bytes())
(out/'pre-attribute-success.json').write_bytes(encode(dict(Status='PRESERVED_SUCCESSFUL_MAIN1_PRE_ATTRIBUTE_NOT_FAILURE',FullGuardSha256=sha((old/'full-guard.json').read_bytes()),NativeGuardSha256=sha(native.read_bytes()),SourceFreezeSha256=sha((old/'source-freeze.json').read_bytes()),SourceClosureSha256=sha((old/'source-resource-closure.json').read_bytes()),Passed=6685,Integrations=11,Snapshot=[dict(Path=x.relative_to(r).as_posix(),Bytes=x.stat().st_size,Sha256=sha(x.read_bytes())) for x in sorted(snapshot.iterdir())])))
for path in changed:(r/path).write_bytes((candidate/path).read_bytes())
for x in rows:assert len(exact(r/x['Path'],x['Sha256']))==x['Bytes']
(out/'source-resource-closure.json').write_bytes(cl.read_bytes())
f.update(Status='ROOT_PROMOTED323_MAIN2_PROVENANCE_BINARY_ATTRIBUTE_ONLY_PENDING_FRESH_FULL',CandidateDirectory=str(r),ExpectedFullPassed=6685,
    AuthorFinalFreeze=dict(Path=fp.relative_to(r).as_posix(),Sha256=sha(fp.read_bytes())),
    IndependentPeer=dict(Path=a.source_peer.resolve().relative_to(r).as_posix(),Sha256=sha(a.source_peer.read_bytes())),IndependentPeerRole='FINAL_SOURCE_COMPOSITION_PASSIVE_REVIEW',
    AttributeProof=dict(Path=a.attribute_proof.resolve().relative_to(r).as_posix(),Sha256=sha(a.attribute_proof.read_bytes())),
    PreviousSuccessfulMain1PreAttribute=dict(Path=(out/'pre-attribute-success.json').relative_to(r).as_posix(),Sha256=sha((out/'pre-attribute-success.json').read_bytes())),
    AttributeFixAndDerivedSealMetadataOnlyDelta=True,Other2346RowsAndAll13CompiledCSharpRawExact=True,RootPromotionScriptSha256=sha(Path(__file__).read_bytes()))
(out/'source-freeze.json').write_bytes(encode(f));print(json.dumps(dict(Main2FreezeSha256=sha((out/'source-freeze.json').read_bytes()),ClosureSha256=a.closure_sha256.upper(),OnlyChanged=changed)))
