"""Extract immutable C# data from the byte-identical pinned OTP-generated module.

No Erlang evaluator or runtime code is emitted. Algorithms live in C#.
Derived from OTP29.1.1 gen_unicode_mod.escript, Apache-2.0, Ericsson AB 2017-2026.
"""
import argparse,gzip,hashlib,json,re
from pathlib import Path

PINNED='82CDA59A022D642B61A858407043E06B775D9602CEDAFC3EDF58577622F79870'
MAXIMUM=0x10ffff

def literal(text):
    tokens=re.findall(r'\d+|[a-zA-Z_][a-zA-Z_0-9]*|[\[\]{},]',text)
    position=0
    def value():
        nonlocal position
        token=tokens[position];position+=1
        if token in ('[','{'):
            end=']' if token=='[' else '}';items=[]
            while tokens[position]!=end:
                items.append(value())
                if tokens[position]==',':position+=1
            position+=1
            return items
        return int(token) if token.isdigit() else token
    result=value()
    assert position==len(tokens),text
    return result

def span(head):
    match=re.search(r'is_integer\(CP, (\d+), (\d+)\)',head)
    if match:return int(match[1]),int(match[2])
    match=re.search(r'\((\d+)\)',head)
    return (int(match[1]),int(match[1])) if match else None

def disjoint(ranges):
    result=[]
    for first,last in sorted(ranges):
        if result and first<=result[-1][1]+1:result[-1][1]=max(result[-1][1],last)
        else:result.append([first,last])
    assert all(result[i-1][1]<result[i][0] for i in range(1,len(result)))
    return result

def main():
    parser=argparse.ArgumentParser(description=__doc__)
    parser.add_argument('source',type=Path);parser.add_argument('output',type=Path)
    args=parser.parse_args();raw=args.source.read_bytes()
    assert hashlib.sha256(raw).hexdigest().upper()==PINNED,'Wrong generated OTP source'
    lines=raw.decode().splitlines();cases={};entries={};pairs=[]
    for line in lines:
        match=re.fullmatch(r'case_table\((\d+)\) -> (.*);',line)
        if match:cases[int(match[1])]=literal(match[2])
        match=re.fullmatch(r'unicode_table\((\d+)\) -> (.*);',line)
        if match:entries[int(match[1])]=literal(match[2])
        match=re.fullmatch(r'compose_pair\((\d+),(\d+)\) -> (\d+);',line)
        if match:pairs.append([int(x) for x in match.groups()])
    def case(cp):
        v=cases.get(cp,[cp,cp])
        return v if len(v)==4 else [v[0],v[1],v[0],v[1]]
    def subcategory(cp):
        u,l,t,f=case(cp)
        return ['letter','other' if u==cp and l==cp else 'uppercase' if u==cp else 'lowercase' if l==cp else 'titlecase' if t==cp else 'other']
    sub=[None]*(MAXIMUM+1)
    categories=[None]*(MAXIMUM+1)
    for function,target in [('subcat_letter',sub),('lookup_category',categories)]:
        for line in lines:
            if not line.startswith(function+'(') or ' -> 'not in line:continue
            head,body=line.split(' -> ',1);bounds=span(head)
            if bounds is None:continue
            value=None if body.startswith('subcat_letter(') else literal(body[:-1])
            for cp in range(bounds[0],bounds[1]+1):
                if target[cp] is None:target[cp]=value if value is not None else sub[cp] or subcategory(cp)
    for cp in range(MAXIMUM+1):
        categories[cp]=categories[cp] or ['other','not_assigned']
    for cp,row in entries.items():
        if row[3]!='lookup_category':categories[cp]=row[3]
    category_ranges=[];start=0
    for cp in range(1,MAXIMUM+2):
        if cp>MAXIMUM or categories[cp]!=categories[start]:
            category_ranges.append([start,cp-1,*categories[start]])
            start=cp
    properties={}
    for name in ['is_control','is_extend','is_ext_pict','is_indic_consonant','is_indic_linker','is_wide_cp']:
        ranges=[]
        for line in lines:
            if not line.startswith(name+'(')or ' -> true;'not in line:continue
            bounds=span(line.split(' -> ')[0]);assert bounds,line;ranges.append(list(bounds))
        properties[name]=disjoint(ranges)
    for name,action in [('prepend','gc_prepend'),('hangul_l','gc_h_L'),('hangul_v','gc_h_V'),('hangul_t','gc_h_T')]:
        ranges=[]
        for line in lines:
            if not line.startswith('gc_1(')or ' -> '+action+'('not in line:continue
            match=re.search(r', (\d+)=CP',line)
            bounds=(int(match[1]),int(match[1]))if match else span(line)
            assert bounds,line;ranges.append(list(bounds))
        properties[name]=disjoint(ranges)
    document=dict(Version=[17,0],SourceSha256=PINNED,Categories=category_ranges,
        Entries=[[cp,*row[:3]]for cp,row in sorted(entries.items())],
        Cases=[[cp,*case(cp)]for cp in sorted(cases)],Pairs=pairs,Properties=properties)
    data=json.dumps(document,separators=(',',':')).encode()
    args.output.parent.mkdir(parents=True,exist_ok=True)
    args.output.write_bytes(gzip.compress(data,mtime=0))
    print(json.dumps(dict(SourceSha256=PINNED,OutputSha256=hashlib.sha256(args.output.read_bytes()).hexdigest().upper(),Bytes=len(args.output.read_bytes()),CategoryRanges=len(category_ranges),Entries=len(entries),Cases=len(cases),CompositionPairs=len(pairs)),indent=2))

if __name__=='__main__':main()
