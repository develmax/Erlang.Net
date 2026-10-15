from pathlib import Path
import hashlib,json,subprocess
r=Path.cwd();d=r/'artifacts/root-part319-composed2';c=d/'candidate';assert not d.exists();c.mkdir(parents=True)
h=lambda b:hashlib.sha256(b).hexdigest().upper();read=lambda p:json.loads(p.read_bytes());write=lambda p,v:p.write_bytes((json.dumps(v,indent=2)+'\n').encode())
base=r/'artifacts/root-part318-main1/source-resource-closure.json';assert h(base.read_bytes())=='7B8C0913A6E46324BF8A40DBD8EB86105990AA6DA76B35EF9BA294E7A44D0C03'
assert subprocess.check_output(['git','rev-parse','HEAD']).decode().strip()=='f7ef36d1ddba1af2e52f77b7536f34e5db71a04f'
base_rows=read(base)['Files']
for e in base_rows:
 raw=(r/e['Path']).read_bytes();assert len(raw)==e['Bytes'] and h(raw)==e['Sha256'],e['Path'];p=c/e['Path'];p.parent.mkdir(parents=True,exist_ok=True);p.write_bytes(raw)
a=r/'artifacts/guest-connected-send319-candidate2';fp=a/'source-freeze.json';assert h(fp.read_bytes())=='B1A467C64762070A0948AAA216D3CC7163D2F5284CC7CCEC226E8721DF906EDA';f=read(fp);owned=f['OwnedCSharp'];assert len(owned)==17
for e in owned:
 p=c/e['Path'];assert (not p.exists()) if e['Added'] else h(p.read_bytes())==e['ParentSha256'];raw=(a/'overlay'/e['Path']).read_bytes();assert len(raw)==e['Bytes'] and h(raw)==e['Sha256'];p.parent.mkdir(parents=True,exist_ok=True);p.write_bytes(raw)
write(d/'token-inputs.json',[str((c/e['Path']).resolve()) for e in owned]);write(d/'pre-format-source-freeze.json',{'ParentHead':'f7ef36d1ddba1af2e52f77b7536f34e5db71a04f','SourcePaths':17,'CSharpPaths':17,'SourceClosurePaths':2282,'Files':owned,'AuthorFreezeSha256':h(fp.read_bytes()),'NativePlansIntentionallyStale':True})
print('Composed verified318 parent plus319 sixteen CSharp files; source-only awaiting formatting/build.')
