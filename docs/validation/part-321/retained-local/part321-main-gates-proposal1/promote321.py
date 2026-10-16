from pathlib import Path
import argparse,hashlib,json,subprocess
p=argparse.ArgumentParser();p.add_argument('--freeze',required=True,type=Path);p.add_argument('--freeze-sha256',required=True);p.add_argument('--closure-sha256',required=True);p.add_argument('--source-peer',required=True,type=Path);p.add_argument('--source-peer-sha256',required=True);a=p.parse_args()
r=Path.cwd().resolve();fp=a.freeze.resolve();d=fp.parent;c=d/'candidate';cl=d/'source-resource-closure.json';out=r/'artifacts/root-part321-main1';h=lambda b:hashlib.sha256(b).hexdigest().upper();read=lambda p:json.loads(p.read_bytes())
assert fp.is_relative_to(r/'artifacts') and not out.exists() and h(fp.read_bytes())==a.freeze_sha256.upper();assert a.freeze_sha256.upper()=='F36D5712337293908866C52328623D8EB71B7FCE935AAFD846FC1906D1DE5DB1';assert a.closure_sha256.upper()=='E70A3810D719536D6B94D3D92C1E5236C41AE86B858A12A5901A5BE8C86F4FD7';f=read(fp);assert h(cl.read_bytes())==f['FinalSourceClosureSha256']==a.closure_sha256.upper();rows=read(cl)['Files'];owned={e['Path']:e for e in f['Files']};assert (len(owned),sum(n.endswith('.cs') for n in owned),len(rows))==(24,9,2316)
peer=a.source_peer.resolve();assert peer.is_relative_to(r/'artifacts') and h(peer.read_bytes())==a.source_peer_sha256.upper();v=read(peer);assert v.get('Status','').startswith('PASS') or v.get('Passed') is True;assert a.freeze_sha256.upper() in json.dumps(v) and a.closure_sha256.upper() in json.dumps(v)
assert subprocess.check_output(['git','--no-optional-locks','rev-parse','HEAD']).decode().strip()==f['ParentHead'];assert not subprocess.check_output(['git','--no-optional-locks','diff','--name-only']) and not subprocess.check_output(['git','--no-optional-locks','diff','--cached','--name-only'])
for e in rows:
 raw=(c/e['Path']).read_bytes();assert len(raw)==e['Bytes'] and h(raw)==e['Sha256'];target=r/e['Path']
 if e['Path'] in owned:
  before=owned[e['Path']];assert before['Added']==(not target.exists()) and (before['Bytes'],before['Sha256'])==(e['Bytes'],e['Sha256'])
  if target.exists():assert h(target.read_bytes())==before['ParentSha256'],e['Path']
 else:assert target.read_bytes()==raw,e['Path']
out.mkdir()
for e in f['Files']:
 target=r/e['Path'];target.parent.mkdir(parents=True,exist_ok=True);target.write_bytes((c/e['Path']).read_bytes())
for e in rows:assert h((r/e['Path']).read_bytes())==e['Sha256']
(out/'source-resource-closure.json').write_bytes(cl.read_bytes());f.update(Status='ROOT_PROMOTED_EXACT_CONNECTED_LINK321_PENDING_FULL',AuthorFinalFreeze={'Path':fp.relative_to(r).as_posix(),'Sha256':h(fp.read_bytes())},IndependentPeer={'Path':peer.relative_to(r).as_posix(),'Sha256':h(peer.read_bytes())},RootPromotionScriptSha256=h(Path(__file__).read_bytes()),ExpectedFullPassed=6678,SourceClosurePaths=2316,CandidateDirectory=str(r),IndependentPeerRole='FINAL_SOURCE_COMPOSITION_PASSIVE_REVIEW');(out/'source-freeze.json').write_bytes((json.dumps(f,indent=2)+'\n').encode());print(json.dumps({'MainFreezeSha256':h((out/'source-freeze.json').read_bytes()),'ExpectedFullPassed':6678,'Registry':509,'ErlangMfas':351}))
