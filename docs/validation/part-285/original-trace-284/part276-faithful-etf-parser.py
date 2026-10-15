import struct
def decode(data):
    pos=0
    def take(n):
        nonlocal pos
        assert n>=0 and pos+n<=len(data)
        b=data[pos:pos+n]; pos+=n; return b
    def number(n):return int.from_bytes(take(n),'big')
    assert number(1)==131
    def term():
        tag=number(1)
        if tag in (97,98):return {'Tag':tag,'Type':'Integer','Value':number(1) if tag==97 else int.from_bytes(take(4),'big',signed=True)}
        if tag in (100,115,118,119):
            n=number(2 if tag in (100,118) else 1)
            return {'Tag':tag,'Type':'Atom','Value':take(n).decode('utf-8' if tag in (118,119) else 'latin1')}
        if tag in (104,105):return {'Tag':tag,'Type':'Tuple','Values':[term() for _ in range(number(1 if tag==104 else 4))]}
        if tag==106:return {'Tag':tag,'Type':'Nil'}
        if tag==107:
            b=take(number(2)); return {'Tag':tag,'Type':'String','BytesHex':b.hex(),'Values':list(b)}
        if tag==108:
            values=[term() for _ in range(number(4))]; return {'Tag':tag,'Type':'List','Values':values,'Tail':term()}
        if tag==109:return {'Tag':tag,'Type':'Binary','BytesHex':take(number(4)).hex()}
        if tag==116:return {'Tag':tag,'Type':'Map','Entries':[[term(),term()] for _ in range(number(4))]}
        if tag in (110,111):
            n=number(1 if tag==110 else 4); sign=number(1); assert sign in (0,1)
            return {'Tag':tag,'Type':'Integer','Value':int.from_bytes(take(n),'little')*(-1 if sign else 1)}
        if tag in (88,103):return {'Tag':tag,'Type':'Pid','Node':term(),'Id':number(4),'Serial':number(4),'Creation':number(4 if tag==88 else 1)}
        if tag in (90,114):
            n=number(2); node=term(); creation=number(4 if tag==90 else 1)
            return {'Tag':tag,'Type':'Reference','Node':node,'Creation':creation,'Ids':[number(4) for _ in range(n)]}
        raise ValueError(('Unexpected ETF tag',tag,pos))
    value=term(); assert pos==len(data); return value
# Projections are only for explicit term comparisons; wire tags/full typed values and raw bytes remain saved.
def project(v):
    if v['Type'] in ('Atom','Integer'):return v['Value']
    if v['Type']=='Tuple':return tuple(project(x) for x in v['Values'])
    if v['Type']=='Map':return [('MapEntry',project(k),project(val)) for k,val in v['Entries']]
    if v['Type']=='Nil':return []
    if v['Type']=='String':return v['Values']
    if v['Type']=='List':assert v['Tail']['Type']=='Nil'; return [project(x) for x in v['Values']]
    return v
