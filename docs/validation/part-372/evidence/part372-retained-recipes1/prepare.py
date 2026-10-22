from pathlib import Path
import json,hashlib,ast
O=Path(__file__).resolve().parent;R=O.parents[1]
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest().upper()
def out(n,x):(O/n).write_text(json.dumps(x,indent=2)+'\n',encoding='utf-8')
old=json.loads((R/'artifacts/erlang371-retained-recipes1/inputs.json').read_text());stages=old['Stages'][:]
for name,module,harness,tool,count in [('erlang371','erlang','artifacts/erlang371-main-final1/main-capture','artifacts/erlang371-next-original-proposal1/tool',751),('ordsets371','ordsets','artifacts/ordsets371-full-main-fixed2/main-capture','artifacts/ordsets371-remaining-original-proposal3/tool',436)]:
 h=R/harness;t=R/tool;profile=json.loads((t/'profile.json').read_text());assert sum(len(f['Names']) for f in profile['Fixtures'])==count
 sources=[{'Path':p.relative_to(R).as_posix(),'Bytes':p.stat().st_size,'Sha256':sha(p)} for p in sorted(h.iterdir()) if p.is_file() and p.suffix in ('.cs','.csproj')]
 binary=h/'bin/Debug/net10.0';cached=[{'Path':p.relative_to(binary).as_posix(),'Bytes':p.stat().st_size,'Sha256':sha(p)} for p in sorted(binary.rglob('*')) if p.is_file()]
 assert (binary/'MainCapture.dll').exists()
 stages.append({'Name':name,'Module':module,'Harness':harness,'Tool':tool,'Values':count,'Profiles':count*5,'Stage':'erlang57-capture','Timeout':1200,'ManifestSha256':sha(t/'source-inputs.corrected.json'),'ProfileSha256':sha(t/'profile.json'),'ObserverSha256':sha(binary/'MainCapture.dll'),'HarnessSources':sources,'CachedBinFiles':cached})
out('inputs.json',dict(old,Stages=stages))
s=(R/'artifacts/erlang371-retained-recipes1/capture-cached.py').read_text()
s=s.replace('D8CC20A89C523C11CEBFF70A57B906716CEBF016D3E5D831A4BE641D03BF987D',sha(O/'inputs.json'))
s=s.replace("ap.add_argument('--only',nargs='+');a=ap.parse_args()", "ap.add_argument('--only',nargs='+');ap.add_argument('--source-bindings',type=Path,required=True);ap.add_argument('--source-bindings-sha256',required=True);a=ap.parse_args()")
anchor=" m=J(B/'inputs.json');helper="
insert=""" assert H(a.source_bindings)==a.source_bindings_sha256.upper()
 source_bindings=J(a.source_bindings);assert len(source_bindings)>=499 and len({r['Path'] for r in source_bindings})==len(source_bindings)
 for row in source_bindings:assert H(R/row['Path'])==row['Sha256'],row['Path']
 save(out/'source-guard-input.json',{'Path':str(a.source_bindings.resolve()),'Sha256':H(a.source_bindings),'Count':len(source_bindings)})
"""
assert anchor in s;s=s.replace(anchor,insert+anchor)
s=s.replace(" unchanged=all(H(R/x['Path'])==x['Sha256'] for x in closure)", " unchanged=all(H(R/x['Path'])==x['Sha256'] for x in closure) and all(H(R/x['Path'])==x['Sha256'] for x in source_bindings)")
(O/'capture-cached.py').write_text(s,encoding='utf-8')
for p in O.glob('*.py'):ast.parse(p.read_text(),filename=str(p))
out('freeze.json',{'Status':'FROZEN_NO_BUILD_REPLAY_RECIPE_NOT_EXECUTED','Files':[{'Path':p.name,'Bytes':p.stat().st_size,'Sha256':sha(p)} for p in sorted(O.iterdir()) if p.is_file() and p.name!='freeze.json']})
print(sha(O/'freeze.json'))
