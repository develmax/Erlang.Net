from pathlib import Path
import json,shutil
old=Path('artifacts/library448-probe2');new=Path('artifacts/library448-probe3');assert not new.exists();new.mkdir()
for f in old.iterdir():
 if f.is_file():shutil.copyfile(f,new/f.name)
p=new/'run-root.py';s=p.read_text();assert s.count('library448-driver-package1')==1;s=s.replace('library448-driver-package1','library448-driver-package2');p.write_text(s)
(new/'reviewed-successor.json').write_text(json.dumps(dict(Previous='artifacts/library448-probe2',Reason='Use fresh final child/package after confirmed registered-name send correction; all observer/handshake/worker source files unchanged. Preserve first actual authenticated-but-outgoing0 timeout.'),indent=2)+'\n')
