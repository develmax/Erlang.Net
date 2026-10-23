from pathlib import Path
import shutil,json
out=Path('artifacts/library449-probe3');assert not out.exists();shutil.copytree('artifacts/library449-probe2',out)
p=out/'Program.cs';s=p.read_text();s=s.replace('Term.A("true")))));','Term.A("true"))));');p.write_text(s)
p=out/'run-root.py';s=p.read_text();line=" stage([str(dotnet),'build',str(project),'-c','Release','--nologo','-m:1','-nr:false','-p:UseSharedCompilation=false'],'compile-consumer')\n";assert s.count(line)==1;s=s.replace(line,'');s=s.replace("try:\n scan('before')","try:\n"+line+" scan('before')");s=s.replace(" try:scan('after')"," try:\n  if scans:scan('after')");p.write_text(s)
(out/'reviewed-successor.json').write_text(json.dumps({'Previous':'artifacts/library449-probe2','Reason':'Remove one preparation parenthesis; compile independent managed observer before expensive original inventory and original compiler. No runtime or observation criteria changed.'},indent=2)+'\n')
print('Successor3 prepared')
