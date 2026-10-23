from pathlib import Path
import shutil,json
out=Path('artifacts/library449-probe7');assert not out.exists();shutil.copytree('artifacts/library449-probe6',out)
p=out/'run-root.py';s=p.read_text();assert s.count('library448-driver-package2')==1;s=s.replace('library448-driver-package2','library449-driver-package1');p.write_text(s)
(out/'reviewed-successor.json').write_text(json.dumps({'Previous':'artifacts/library449-probe6','Reason':'Fresh final current-HEAD rebuild has changed assembly provenance/hash while production source is unchanged; rebind fresh actual native host package. Observer/worker/handshake/criteria unchanged. Recheck all4 profiles on final binaries.'},indent=2)+'\n')
print('Successor7 prepared')
