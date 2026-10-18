from pathlib import Path
import json,hashlib
r=Path.cwd();o=r/'artifacts/erlang371-retained-recipes1';o.mkdir(exist_ok=True);sha=lambda p:hashlib.sha256(p.read_bytes()).hexdigest().upper()
candidates=[('erlang-old','erlang','artifacts/erlang-integration370-final-old1/main-capture','artifacts/erlang-original370-pure1/preparation1/tool',1597,7985,'erlang57-capture',1200),('erlang-new','erlang','artifacts/erlang-integration370-final-new1/main-capture','artifacts/erlang-original370-closure1/preparation1/tool',44,220,'erlang57-capture',1200),('binary-primary','binary','artifacts/binary-integration369-final2/main-capture','artifacts/binary-original369-original1/tool',585,2925,'binary31-capture',540),('binary-supplement','binary','artifacts/binary-integration369-supplement-final/main-capture','artifacts/binary-original369-supplement1/tool',36,180,'binary31-capture',540),('binary-closure','binary','artifacts/binary-integration369-closure/main-capture','artifacts/binary-original369-closure1/tool',14,70,'binary31-capture',540),('binary-insert','binary','artifacts/binary-integration369-insert/main-capture','artifacts/binary-original369-insert1/tool',7,35,'binary31-capture',540)]
rows=[]
for name,module,harness,tool,values,profiles,stage,timeout in candidates:
 h=r/harness;t=r/tool;b=h/'bin/Debug/net10.0';assert (b/'MainCapture.dll').is_file(),str(b)
 p=json.loads((t/'profile.json').read_bytes());assert p['SubjectModule']==module and sum(len(x['Names']) for x in p['Fixtures'])==values
 sources=[{'Path':q.relative_to(r).as_posix(),'Bytes':q.stat().st_size,'Sha256':sha(q)} for q in sorted(h.iterdir()) if q.is_file()]
 bins=[{'Path':q.relative_to(b).as_posix(),'Bytes':q.stat().st_size,'Sha256':sha(q)} for q in sorted(b.rglob('*')) if q.is_file()]
 # Original source inventory remains the source-only manifest authority; producer GIT is separately admitted.
 m=json.loads((t/'source-inputs.corrected.json').read_bytes())
 for x in m['ToolInputs']:
  q=t/x['Path'];assert q.stat().st_size==x['Bytes'] and sha(q)==x['Sha256'],(name,x['Path'])
 rows.append({'Name':name,'Module':module,'Harness':harness,'Tool':tool,'Values':values,'Profiles':profiles,'Stage':stage,'Timeout':timeout,'ManifestSha256':sha(t/'source-inputs.corrected.json'),'ProfileSha256':sha(t/'profile.json'),'ObserverSha256':sha(b/'MainCapture.dll'),'HarnessSources':sources,'CachedBinFiles':bins})
(o/'inputs.json').write_text(json.dumps({'OwnedHelper':'artifacts/spawn-priority351-preparation/execute-root.py','OwnedHelperSha256':sha(r/'artifacts/spawn-priority351-preparation/execute-root.py'),'Stages':rows},indent=2)+'\n',newline='\n')
print('Source/hash admission passes for',len(rows),'existing own harnesses; no builds/execution')
