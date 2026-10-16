from pathlib import Path
import json,hashlib
r=Path.cwd();s=r/'artifacts/non-tail-stack329-root-candidate6';d=r/'artifacts/non-tail-stack329-root-candidate7';assert not d.exists();h=lambda p:hashlib.sha256(p.read_bytes()).hexdigest().upper();j=lambda p:json.loads(p.read_bytes());assert h(s/'source-freeze.json')=='FDB76D1C844B15C1D4F5011AE9C23D38B89FF2C962749D5FEFF590F593113C34';f=j(s/'source-freeze.json');rows=j(s/'source-resource-closure.json')['Files'];owned={x['Path'] for x in f['Files']};d.mkdir();c=d/'candidate';c.mkdir();changes=[]
for x in rows:
 q=s/'candidate'/x['Path'];assert h(q)==x['Sha256'];raw=q.read_bytes();dest=c/x['Path'];dest.parent.mkdir(parents=True,exist_ok=True)
 if x['Path'] in owned:
  after=raw.replace(b'\r\n',b'\n')
  if raw!=after:changes.append(x['Path'])
  raw=after
 dest.write_bytes(raw)
new=[dict(Path=x['Path'],Bytes=(c/x['Path']).stat().st_size,Sha256=h(c/x['Path'])) for x in rows];cl=d/'source-resource-closure.json';cl.write_bytes((json.dumps({'Files':new},indent=2)+'\n').encode());f.update(Status='SOURCE_ONLY_EXPLICIT_OWNED_CSHARP_LF_BEFORE_LEXICAL_FORMAT_GUARD',SourceClosureSha256=h(cl),Files=[x for x in new if x['Path'] in owned],PreviousAuthorFreezeSha256=h(s/'source-freeze.json'),ExplicitOwnedCrLfToLfPaths=changes,PreviousFocus4StoppedBeforeBuild=True,TestRawLiteralLineEndingsChangedIntentionally=True);(d/'source-freeze.json').write_bytes((json.dumps(f,indent=2)+'\n').encode());print(json.dumps({'FreezeSha256':h(d/'source-freeze.json'),'ClosureSha256':h(cl),'CrLfToLfPaths':changes}))
