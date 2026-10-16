from pathlib import Path
import ast,json,hashlib
r=Path.cwd();d=r/'artifacts/part329-remote-new-review';rows=[]
for name in ['passive-common.py','review-ci.py','review-ordinary.py','review-allocator-profile.py','review-memory-profile.py']:
 p=d/name;ast.parse(p.read_text());rows.append(dict(Path=name,Bytes=p.stat().st_size,Sha256=hashlib.sha256(p.read_bytes()).hexdigest().upper()))
p=d/'passive-readers-source-freeze.json';p.write_bytes((json.dumps(dict(Status='SOURCE_ONLY_PASSIVE_READERS_PREPARED_BEFORE_DOWNLOAD',PublishedHead='d83ed875d0c77939e20049afcb46fa297fee82dd',NoDownloadedCodeExecution=True,Files=rows),indent=2)+'\n').encode());print(hashlib.sha256(p.read_bytes()).hexdigest().upper())
