from pathlib import Path
import hashlib,json,subprocess,sys
r=Path.cwd().resolve();d=r/'artifacts/root-part318-composed2';c=d/'candidate';out=r/'artifacts/root-part318-main1';assert not out.exists()
h=lambda b:hashlib.sha256(b).hexdigest().upper();read=lambda p:json.loads(p.read_bytes());fp=d/'source-freeze.json';cl=d/'source-resource-closure.json'
assert h(fp.read_bytes())=='1457F4AC6196683BC13BBCDECB73717251F1287684826F45E30E7FFE47AA2CD7'
v=read(fp);assert h(cl.read_bytes())==v['FinalSourceClosureSha256']=='7B8C0913A6E46324BF8A40DBD8EB86105990AA6DA76B35EF9BA294E7A44D0C03'
peer=Path(sys.argv[1]).resolve();assert peer.is_relative_to(r) and h(fp.read_bytes()) in peer.read_text(encoding='utf-8-sig');pv=read(peer);assert pv.get('Status','').startswith('PASS') or pv.get('Passed') is True
assert subprocess.check_output(['git','rev-parse','HEAD']).decode().strip()==v['ParentHead']=='50cee883796a95c11fcde318098ce8a1156685ca'
assert not subprocess.check_output(['git','diff','--name-only']) and not subprocess.check_output(['git','diff','--cached','--name-only'])
owned={e['Path']:e for e in v['Files']};assert len(owned)==20 and sum(n.endswith('.cs') for n in owned)==6
for e in read(cl)['Files']:
 raw=(c/e['Path']).read_bytes();assert len(raw)==e['Bytes'] and h(raw)==e['Sha256'];p=r/e['Path']
 if e['Path'] in owned:
  old=owned[e['Path']];assert old['Added']==(not p.exists());assert (old['Bytes'],old['Sha256'])==(e['Bytes'],e['Sha256'])
  if p.exists():assert h(p.read_bytes())==old['ParentSha256'],e['Path']
 else:assert p.read_bytes()==raw,e['Path']
out.mkdir()
for e in v['Files']:
 p=r/e['Path'];p.parent.mkdir(parents=True,exist_ok=True);p.write_bytes((c/e['Path']).read_bytes())
for e in read(cl)['Files']:assert h((r/e['Path']).read_bytes())==e['Sha256']
(out/'source-resource-closure.json').write_bytes(cl.read_bytes());v.update(Status='ROOT_PROMOTED_EXACT_GROUP_LEADER318_PENDING_FULL',AuthorFinalFreeze={'Path':fp.relative_to(r).as_posix(),'Sha256':h(fp.read_bytes())},IndependentPeer={'Path':peer.relative_to(r).as_posix(),'Sha256':h(peer.read_bytes())},RootPromotionScriptSha256=h(Path(__file__).read_bytes()))
(out/'source-freeze.json').write_bytes((json.dumps(v,indent=2)+'\n').encode());print(json.dumps({'FreezeSha256':h((out/'source-freeze.json').read_bytes()),'ExpectedFullPassed':6668,'ExpectedRegistry':509,'ExpectedErlang':351,'SourcePaths':20}))
