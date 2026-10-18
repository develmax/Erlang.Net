from pathlib import Path
import hashlib,json
ROOT=Path(__file__).resolve().parents[2]
OUT=Path(__file__).resolve().parent
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest().upper()
path=ROOT/'docs/native-infrastructure.json'
data=json.loads(path.read_text())
bindings=[]
def walk(value,pointer='$'):
 if isinstance(value,dict):
  p=value.get('Path');h=value.get('Sha256')
  if isinstance(p,str) and p.startswith('src/') and isinstance(h,str):
   file=ROOT/p
   bindings.append({'Pointer':pointer,'Path':p,'BoundSha256':h,'ActualSha256':sha(file) if file.exists() else None,'Matches':file.exists() and sha(file)==h})
  for k,v in value.items():walk(v,pointer+'.'+k)
 elif isinstance(value,list):
  for i,v in enumerate(value):walk(v,pointer+'['+str(i)+']')
walk(data)
result={'NativeInventorySha256':sha(path),'BoundSrcReferences':len(bindings),'DistinctSrcPaths':len({r['Path'] for r in bindings}),'Mismatches':[r for r in bindings if not r['Matches']],'Bindings':bindings,'NoMainEdit':True,'Conclusion':'No current native infrastructure binding update needed: all recursively located src Path/Sha256 references match actual final Main. Historical plans, scope classifications and Part369 source addendum remain unchanged.' if all(r['Matches'] for r in bindings) else 'Scoped source-equivalence addendum required for listed mismatches; never overwrite historical evidence hashes.'}
(OUT/'native-infrastructure-current-review.json').write_text(json.dumps(result,indent=2)+'\n',encoding='utf-8')
print(json.dumps({'Bindings':len(bindings),'Distinct':result['DistinctSrcPaths'],'Mismatches':result['Mismatches']}))
