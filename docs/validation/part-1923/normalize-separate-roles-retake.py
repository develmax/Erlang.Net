from pathlib import Path
import hashlib,json,os,subprocess,sys,time

work,source,evaluator,cache,oracle,batches,isolated=map(Path,sys.argv[1:])
work.mkdir()
text=evaluator.read_text(encoding='utf-8-sig')
identity=hashlib.sha256((evaluator.parent/'serial-reference.eval').read_bytes()).hexdigest().upper()
raw=source.read_text(encoding='utf-8-sig').splitlines()
prefix=text[:text.index('Slots=fun')]+text[text.index('Slots=fun'):text.index('Diff=fun')]
prefix=prefix.replace('[Input,CacheDirectory,EvalIdentity,WorkerText]','[ImagePath,CacheDirectory,EvalIdentity,Output]')
prefix=prefix[:prefix.index('{ok,Data}')]+prefix[prefix.index('Normalize=fun'):]
role=prefix+'''
Body=Read(unicode:characters_to_binary(ImagePath)),
Signature=maps:map(fun(_,Ops)->{term_to_binary(Ops,[deterministic]),Slots(Ops)} end,Body),
ok=file:write_file(Output,term_to_binary(Signature,[deterministic])),
halt().
'''
compare='''
[Left,Middle,Right,Reference]=init:get_plain_arguments(),
Read=fun(P)->{ok,B}=file:read_file(P),binary_to_term(B) end,
R=Read(Left),B=Read(Middle),A=Read(Right),
Distance=fun(X,Y)->lists:sum([abs(L-M)||{L,M}<-lists:zip(X,Y,trim)]) end,
Keys=lists:usort(maps:keys(B)++maps:keys(A)),
Rows=lists:filtermap(fun(Key)->
    RefIs=maps:get(Key,R,missing), BeforeIs=maps:get(Key,B,missing), AfterIs=maps:get(Key,A,missing),
    case {RefIs,BeforeIs,AfterIs} of
        {{X,RB},{Y,BB},{Z,AB}} when Y=/=Z->
            Delta=case {length(RB)=:=length(BB),length(BB)=:=length(AB)} of
                {true,true}->Distance(RB,AB)-Distance(RB,BB); _->unknown
            end,
            {true,{Key,X=:=Y,X=:=Z,Delta}};
        {_,Y,Z} when Y=/=Z->{true,{Key,false,false,unknown}};
        _->false
    end
end,Keys),
io:format("~0p.~n",[{filename:basename(Reference),Rows}]),halt().
'''
(work/'role.eval').write_text(role,encoding='utf-8',newline='\n')
(work/'compare-roles.eval').write_text(compare,encoding='utf-8',newline='\n')
parts=[]
receipts=[]
offset=0
for root,glob in ((batches,'batch-*'),(isolated,'image-*')):
    for folder in sorted(root.glob(glob)):
        record=json.loads((folder/'receipt.json').read_text(encoding='utf-8-sig'))
        if record['ExitCode']!=0:break
        inputs=(folder/'input.tsv').read_text(encoding='utf-8-sig').splitlines()
        assert inputs==raw[offset:offset+len(inputs)]
        terms=folder/'terms'
        contents=terms.read_bytes()
        assert len(contents.splitlines())==len(inputs)
        parts.append(contents)
        receipts.append({'Reused':True,'First':offset,'Count':len(inputs),'File':str(terms),
                         'Sha256':hashlib.sha256(contents).hexdigest()})
        offset+=len(inputs)
reused=offset
def call(script,args,directory,stem):
    with (directory/stem).open('wb') as stdout,(directory/(stem+'.stderr.log')).open('wb') as stderr:
        process=subprocess.run([oracle,'+S','1:1','+SDcpu','1','+SDio','1','+A','1','-noshell','-eval',script,'-extra',*args],
          stdout=stdout,stderr=stderr,env=os.environ|{'ERL_CRASH_DUMP':str(directory/(stem+'.erl_crash.dump'))})
    return process.returncode
def compare_roles(directory,paths,stem):
    images=[]
    for k,path in enumerate(paths):
        item=directory/f'role-{k}.etf'
        code=call(role,[path,cache,identity,item],directory,f'role-{k}.log')
        assert code==0 and item.is_file(),(k,path,code)
        images.append(item)
    assert call(compare,[*images,paths[0]],directory,stem)==0
    return (directory/stem).read_bytes()
verification=work/'verification'
verification.mkdir()
assert compare_roles(verification,raw[0].split('\t'),'terms')==parts[0].splitlines(keepends=True)[0]
started=time.monotonic()
while offset<len(raw):
    directory=work/f'image-{offset:03d}'
    directory.mkdir()
    paths=raw[offset].split('\t')
    began=time.monotonic()
    proof=compare_roles(directory,paths,'terms')
    assert len(proof.splitlines())==1,(offset,proof[:40])
    parts.append(proof)
    receipts.append({'Index':offset,'Seconds':time.monotonic()-began,'Success':True,'SeparateRoles':3})
    offset+=1
data=b''.join(parts)
assert len(data.splitlines())==len(raw)
(work/'normalized.terms').write_bytes(data)
receipt={'Images':len(raw),'ReusedImages':reused,'SeparatedImages':len(raw)-reused,'Workers':1,
         'ControlByteEqual':True,'Seconds':time.monotonic()-started,
         'Evaluator':hashlib.sha256(evaluator.read_bytes()).hexdigest().upper(),'CacheIdentity':identity,'Results':receipts}
(work/'receipt.json').write_text(json.dumps(receipt,indent=2)+'\n',encoding='utf-8')
print(json.dumps({k:v for k,v in receipt.items() if k!='Results'}))
