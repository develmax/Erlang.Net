from pathlib import Path
import hashlib
import json
import struct
import sys
out,reference,*candidates=map(Path,sys.argv[1:])
def chunks(p):
    raw=p.read_bytes()
    sections={}
    assert raw[:4]==b"FOR1" and raw[8:12]==b"BEAM"
    for i in positions(raw):
        name,size=struct.unpack_from(">4sI",raw,i)
        sections[name.decode("ascii")]=raw[i+8:i+8+size]
    return sections
def positions(raw):
    i=12
    while i < len(raw):
        yield i
        size=struct.unpack_from(">I",raw,i+4)[0]
        i+=8+((size+3)//4)*4
    assert i==len(raw)
def sha(p):
    return hashlib.sha256(p.read_bytes()).hexdigest()
expected=chunks(reference)
info=[]
for p in candidates:
    observed=chunks(p)
    assert p.is_file()
    diffs={name:{"PinnedSize":len(expected.get(name,b"")),"GeneratedSize":len(observed.get(name,b""))}
           for name in expected.keys()|observed.keys() if expected.get(name)!=observed.get(name)}
    info.append({"Reference":sha(reference),"Generated":sha(p),"Image":p.as_posix(),"Sections":diffs})
out.write_text(json.dumps(info,indent=2)+"\n",encoding="utf-8")
print(json.dumps(info,indent=2))

