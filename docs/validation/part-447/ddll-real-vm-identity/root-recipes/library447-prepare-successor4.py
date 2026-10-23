from pathlib import Path
import hashlib,json,shutil
old=Path('artifacts/library447-probe3');new=Path('artifacts/library447-probe4');assert not new.exists();new.mkdir()
for f in old.iterdir():
 if f.is_file():shutil.copyfile(f,new/f.name)
f=new/'Program.cs';s=f.read_text();assert s.count('frames.Length==Math.Min(depth,2)')==1;s=s.replace('frames.Length==Math.Min(depth,2)','frames.Length==Math.Clamp(depth,1,3)');f.write_text(s)
rows=[{'Path':p.name,'Sha256':hashlib.sha256(p.read_bytes()).hexdigest().upper()} for p in sorted(new.iterdir()) if p.is_file() and p.name!='reviewed-successor.json']
(new/'reviewed-successor.json').write_text(json.dumps({'Previous':'artifacts/library447-probe3','Reason':'Correct only observer cardinality assumption from retained actual original and managed raw stacks: depth0 has one native frame, depth8 has native+demon+actor. Whole frames and values remain unchanged. Retain successful depth1/2; rerun only0/8. No product delta.','Files':rows},indent=2)+'\n')
