from collections import Counter
from pathlib import Path
import json
import re
import struct
import sys

release, frozen, root = map(Path, sys.argv[1:])
from beam_chunk_inventory import chunks, literals

arity={int(n):int(a) for n,a in re.findall(r"^([0-9]+):\s*\S+/([0-9]+)",(root/"pinned-genop.tab").read_text(encoding="utf-8-sig"),re.M)}
word=struct.Struct(">I")

def operand(data,start):
    value=data[start]
    tag=value & 7
    if value & 8 == 0:
        return tag,value>>4,start+1
    if value & 16 == 0:
        return tag,((value&224)<<3)|data[start+1],start+2
    length=(value>>5)+2
    start+=1
    if length==9:
        length_tag,encoded,start=operand(data,start)
        assert length_tag==0
        length=encoded+9
    return tag,None,start+length

def unsigned(data,start):
    tag,value,end=operand(data,start)
    assert tag==0
    if value is None:
        pos=start+1
        if data[start]>>5==7:
            _,_,pos=operand(data,pos)
        value=int.from_bytes(data[pos:end],"big")
    return value,end

def literal_operands(code):
    pos=word.unpack_from(code)[0]+4
    while pos<len(code):
        opcode=code[pos]
        count=arity[opcode]
        pos+=1
        for _ in range(count):
            tag,extension,pos=operand(code,pos)
            if tag!=7:
                continue
            if extension==1:
                assert _==count-1
                pending,pos=unsigned(code,pos)
                break
            pos=yield from extended(code,extension,pos)
        else:
            pending=0
        for _ in range(pending):
            tag,extension,pos=operand(code,pos)
            if tag==7:
                pos=yield from extended(code,extension,pos)
        if opcode==3:
            assert pos==len(code)
            return
    raise ValueError("Missing end of code")

def extended(code,extension,pos):
    if extension==4:
        literal,end=unsigned(code,pos)
        yield pos,end,literal
        return end
    if extension==2:
        return unsigned(code,pos)[1]
    if extension==5:
        _,_,pos=operand(code,pos)
        return unsigned(code,pos)[1]
    if extension==3:
        count,pos=unsigned(code,pos)
        for _ in range(count*2):
            _,pos=unsigned(code,pos)
        return pos
    raise ValueError(f"Unsupported extended operand: {extension}")

def encode_index(index):
    if index<16:
        return bytes([index<<4])
    if index<2048:
        return bytes([((index>>8)<<5)|8,index&255])
    n=max(2,(index.bit_length()+7)//8)
    return bytes([((n-2)<<5)|24])+index.to_bytes(n,"big")

def replay(code,terms):
    mapping={}
    modified=[]
    new_terms=[]
    copied=0
    for start,end,old in literal_operands(code):
        if old not in mapping:
            mapping[old]=len(mapping)
            new_terms.append(terms[old])
        new=mapping[old]
        if old!=new:
            modified.extend((code[copied:start],encode_index(new)))
            copied=end
    new_code=b"".join([*modified,code[copied:]]) if copied else code
    return new_code,new_terms

summary=Counter()
details=[]
for path in sorted(frozen.glob("shard-*/otp-corpus.json")):
    for row in json.loads(path.read_text(encoding="utf-8-sig"))["Modules"]:
        name=row["Beam"]
        summary["Compared"]+=1
        ref=chunks((release/name).read_bytes())
        if row["Status"]=="Equal":
            gen=ref
        else:
            gen=chunks((path.with_suffix(".json.generated")/name).read_bytes())
        try:
            ref_tables=literals(ref)
            gen_tables=literals(gen)
            ref_code,ref_rebuilt=replay(ref["Code"],ref_tables)
            gen_code,gen_rebuilt=replay(gen["Code"],gen_tables)
        except (IndexError,ValueError,AssertionError,KeyError) as error:
            summary["Unreadable"]+=1
            details.append(dict(Module=name,Error=repr(error)))
            continue
        if ref_code!=ref["Code"] or ref_rebuilt!=ref_tables:
            summary["ReferenceWouldChange"]+=1
            details.append(dict(Module=name,ReferenceCanonical=False))
        if gen_code!=gen["Code"] or gen_rebuilt!=gen_tables:
            summary["GeneratedWouldChange"]+=1
            r=dict(Module=name,RemovedLiterals=len(gen_tables)-len(gen_rebuilt),
                   LitTGain=gen_tables!=ref_tables and gen_rebuilt==ref_tables,
                   LitTLoss=gen_tables==ref_tables and gen_rebuilt!=ref_tables,
                   CodeGain=gen["Code"]!=ref["Code"] and gen_code==ref["Code"])
            details.append(r)
            summary.update(key for key in ("LitTGain","LitTLoss","CodeGain") if r[key])
            if r["RemovedLiterals"]:
                summary["GeneratedRemovedLiterals"]+=1
    sys.stdout.flush()
(root/"offline-litt-replay.json").write_text(json.dumps({"Summary":summary,"Details":details},indent=2)+"\n",encoding="utf-8")
print(json.dumps(summary))
