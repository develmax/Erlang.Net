from collections import Counter, defaultdict
from pathlib import Path
import json
import struct
import sys
import zlib

word = struct.Struct(">I")
int_word = struct.Struct(">i")
def chunks(blob):
    if blob[:4]!=b"FOR1" or blob[8:12]!=b"BEAM":
        raise ValueError("Unexpected object")
    index = 12
    parts = {}
    while index < len(blob):
        name = blob[index:index+4].decode("ascii")
        size = word.unpack_from(blob,index+4)[0]
        offset = index + 8
        parts[name] = blob[offset:offset+size]
        index = offset + ((size+3)&~3)
    if index != len(blob):
        raise ValueError("Chunk bounds")
    return parts

def unsigned(blob,pos):
    tag = blob[pos]
    assert tag & 7 == 0
    if tag & 8 == 0:
        return tag >> 4,pos+1
    if tag & 16 == 0:
        return ((tag & 224)<<3) | blob[pos+1],pos+2
    length=(tag>>5)+2
    assert length<9
    return int.from_bytes(blob[pos+1:pos+1+length],"big"),pos+1+length

def atoms(parts):
    data=parts["AtU8"]
    size=int_word.unpack_from(data)[0]
    pos=4
    values=[]
    for _ in range(abs(size)):
        if size<0:
            length,pos=unsigned(data,pos)
        else:
            length=data[pos]
            pos+=1
        values.append(data[pos:pos+length])
        pos+=length
    assert pos==len(data)
    return values

def imports(parts, names):
    data=parts["ImpT"]
    n=word.unpack_from(data)[0]
    triples=[struct.unpack_from(">III",data,offset) for offset in range(4,len(data),12)]
    assert len(triples)==n
    return [(names[m-1],names[f-1],a) for m,f,a in triples]

def literals(parts):
    data=parts.get("LitT")
    if data is None:
        return []
    size=word.unpack_from(data)[0]
    data=zlib.decompress(data[4:]) if size else data[4:]
    assert not size or len(data)==size
    pos=4
    values=[]
    for _ in range(word.unpack_from(data)[0]):
        length=word.unpack_from(data,pos)[0]
        values.append(data[pos+4:pos+4+length])
        pos+=4+length
    assert pos==len(data)
    return values

def types(parts):
    data = parts.get("Type")
    if data is None:
        return []
    version, count = struct.unpack_from(">II", data)
    assert version == 4
    offset = 8
    result = []
    for _ in range(count):
        flags = struct.unpack_from(">H", data, offset)[0]
        size = 2 + 8 * bool(flags & 0x2000) + 8 * bool(flags & 0x4000) + bool(flags & 0x8000)
        result.append(data[offset:offset + size])
        offset += size
    assert offset == len(data)
    return result


def relationship(reference, candidate):
    if reference==candidate:
        return "EqualEntries"
    before,after=Counter(reference),Counter(candidate)
    if before==after:
        return "Permutation"
    if before<after:
        return "CandidateSuperset"
    if after<before:
        return "CandidateSubset"
    return "DifferentEntries"

def main(installed, frozen, output):
    reference_count = 0
    groups=defaultdict(list)
    raw_counts=Counter()
    for report in sorted(frozen.glob("shard-*/otp-corpus.json")):
        entries=json.loads(report.read_text(encoding="utf-8-sig"))["Modules"]
        for entry in entries:
            if entry["Status"]!="Different":
                continue
            reference_count+=1
            name=entry["Beam"]
            old=chunks((installed/name).read_bytes())
            new=chunks((report.with_suffix(".json.generated")/name).read_bytes())
            raw_counts.update(chunk for chunk in old.keys()|new.keys() if old.get(chunk)!=new.get(chunk))
            a,b=atoms(old),atoms(new)
            for family,left,right in [("AtU8",a,b),("ImpT",imports(old,a),imports(new,b)),("LitT",literals(old),literals(new)),("Type",types(old),types(new))]:
                groups[(family,relationship(left,right))].append(name)
    assert reference_count==962
    digest=dict(Different=reference_count,ChangedRawSections=dict(sorted(raw_counts.items())),
        Families=[dict(Section=s,Relationship=r,Count=len(m),Modules=sorted(m)) for (s,r),m in sorted(groups.items())])
    output.write_text(json.dumps(digest,indent=2)+"\n",encoding="utf-8")
    print(json.dumps({"RawSections":digest["ChangedRawSections"],"Families":[(s,r,len(m)) for (s,r),m in sorted(groups.items())]}))


if __name__ == "__main__":
    main(*map(Path, sys.argv[1:]))
