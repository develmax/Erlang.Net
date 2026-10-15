from pathlib import Path
import hashlib,json,subprocess,sys
r=Path.cwd().resolve();d=r/'artifacts/allocator-public-sys-profile316-composed2';c=d/'candidate';out=r/'artifacts/root-part316-main1';assert not out.exists()
h=lambda b:hashlib.sha256(b).hexdigest().upper();read=lambda p:json.loads(p.read_bytes())
f=d/'formatted-source-freeze.json';cl=d/'formatted-source-resource-closure.json';assert h(f.read_bytes())=='2798591EE8BC8FE77E719B96C2D9535672255C5E772E0FFC30E6E294BFA4372A'
v=read(f);closure=read(cl);assert h(cl.read_bytes())==v['SourceClosureSha256']=='87CC2B5E1C8DDEF229E95DA38875431E73BA715D6EE71275F7DAEA5755E00299'
peer=Path(sys.argv[1]).resolve();assert peer.is_relative_to(r) and read(peer)['Status'].startswith('PASS') and h(f.read_bytes()) in peer.read_text(encoding='utf-8-sig')
head=subprocess.check_output(['git','rev-parse','HEAD']).decode().strip();assert head==v['ParentHead']=='32c394232588c275703e29be444f8d84cb5d27a4'
assert not subprocess.check_output(['git','diff','--name-only']) and not subprocess.check_output(['git','diff','--cached','--name-only'])
owned={e['Path']:e for e in v['Files']};assert len(owned)==25 and sum(n.endswith('.cs') for n in owned)==18 and len(closure['Files'])==2241
for e in closure['Files']:
 raw=(c/e['Path']).read_bytes();assert len(raw)==e['Bytes'] and h(raw)==e['Sha256'];p=r/e['Path']
 if e['Path'] in owned:
  old=owned[e['Path']];assert old['Added']==(not p.exists());assert (old['Bytes'],old['Sha256'])==(e['Bytes'],e['Sha256'])
  if p.exists():assert h(p.read_bytes())==old['ParentSha256']
 else:assert p.read_bytes()==raw,e['Path']
out.mkdir()
for e in v['Files']:
 p=r/e['Path'];p.parent.mkdir(parents=True,exist_ok=True);p.write_bytes((c/e['Path']).read_bytes())
for e in closure['Files']:assert h((r/e['Path']).read_bytes())==e['Sha256']
(out/'source-resource-closure.json').write_bytes(cl.read_bytes())
v.update(Status='ROOT_PROMOTED_EXACT_PUBLIC_ALLOCATOR_PROFILE316_PENDING_FULL',FinalSourceClosureSha256=h(cl.read_bytes()),ExpectedErlangPresence=349,PublicMfaDelta=2,LegacyPlanInputs=1073,LegacyPlanSha256=v['Plans'][0]['Sha256'],OwnPlanInputs=1098,OwnPlanSha256=v['Plans'][1]['Sha256'],AuthorFinalFreeze={'Path':f.relative_to(r).as_posix(),'Sha256':h(f.read_bytes())},IndependentPeer={'Path':peer.relative_to(r).as_posix(),'Sha256':h(peer.read_bytes())},RootPromotionScriptSha256=h(Path(__file__).read_bytes()))
(out/'source-freeze.json').write_bytes((json.dumps(v,indent=2)+'\n').encode());print(json.dumps({'FreezeSha256':h((out/'source-freeze.json').read_bytes()),'ExpectedFullPassed':6654,'ExpectedRegistry':507,'ExpectedErlang':349,'SourcePaths':25}))
