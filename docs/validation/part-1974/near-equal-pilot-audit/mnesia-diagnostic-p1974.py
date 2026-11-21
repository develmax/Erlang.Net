from pathlib import Path
import hashlib
import json
import struct
import sys

source,decoded,diagnostic,candidate,reference,out=map(Path,sys.argv[1:])
def sections(p):
    raw=p.read_bytes()
    assert raw[:4]==b"FOR1" and raw[8:12]==b"BEAM"
    output=[]
    offset=12
    while offset<len(raw):
        tag,size=struct.unpack_from(">4sI",raw,offset)
        data=raw[offset+8:offset+8+size]
        padding=raw[offset+8+size:offset+8+((size+3)//4)*4]
        output.append((tag,data,padding))
        offset+=8+((size+3)//4)*4
    assert offset==len(raw)
    return output

def code(parts):
    return next(data for tag,data,_ in parts if tag==b"Code")
def sha(raw):return hashlib.sha256(raw).hexdigest()

original=code(sections(source))
derived=code(sections(diagnostic))
payload_offset=4+struct.unpack_from(">I",original)[0]
assert original[:payload_offset]==derived[:payload_offset]
ops=json.loads(decoded.read_bytes())["Instructions"]
start=payload_offset+ops[0]["Start"]
stop=payload_offset+next(x["End"] for x in ops if x["Op"]=="badrecord")
new_stop=stop+len(derived)-len(original)
assert original[:start]==derived[:start] and original[stop:]==derived[new_stop:]
before,after=original[start:stop],derived[start:new_stop]
assert before!=after
candidate_parts=sections(candidate)
current=code(candidate_parts)
assert current.count(before)==1,{"SourceWindow":len(before),"ExactMatches":current.count(before)}
replacement=current.replace(before,after)
forj=b"BEAM"
for tag,payload,padding in candidate_parts:
    content=replacement if tag==b"Code" else payload
    pad=b"\x00"*((-len(content))%4) if tag==b"Code" else padding
    forj+=tag+struct.pack(">I",len(content))+content+pad
result=b"FOR1"+struct.pack(">I",len(forj))+forj
out.write_bytes(result)

# Reference input is first opened after the diagnostic output has been written.
ref=reference.read_bytes()
identity={
    "Status":"Equal" if result==ref else "Different",
    "Current":sha(candidate.read_bytes()),
    "Diagnostic":sha(result),
    "Pinned":sha(ref),
    "OriginalFunctionBytes":len(before),
    "DiagnosticFunctionBytes":len(after),
    "OnlyCandidateCodeRewritten":True,
}
print(json.dumps(identity,indent=2))

