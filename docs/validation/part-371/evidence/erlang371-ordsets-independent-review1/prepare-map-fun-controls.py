from pathlib import Path
import hashlib,json
ROOT=Path(__file__).resolve().parents[2]
OUT=ROOT/'artifacts/ordsets371-map-fun-control-proposal1'
(OUT/'original').mkdir(parents=True,exist_ok=True)
def sha(p): return hashlib.sha256(p.read_bytes()).hexdigest().upper()
cases=[
 ('fun_map_is_set','is_set',1,'ordsets:is_set([First,Second])'),
 ('fun_map_is_element','is_element',2,'ordsets:is_element(Second,[First,Second])'),
 ('fun_map_add_element','add_element',2,'ordsets:add_element(Second,[First])'),
 ('fun_map_del_element','del_element',2,'ordsets:del_element(Second,[First,Second])'),
 ('fun_map_from_list','from_list',1,'ordsets:from_list([Second,First])'),
 ('fun_map_union_two','union',2,'ordsets:union([First],[Second])'),
 ('fun_map_union_many','union',1,'ordsets:union([[First],[Second]])'),
 ('fun_map_intersection_two','intersection',2,'ordsets:intersection([First,Second],[Second])'),
 ('fun_map_intersection_many','intersection',1,'ordsets:intersection([[First,Second],[Second]])'),
 ('fun_map_is_disjoint','is_disjoint',2,'ordsets:is_disjoint([First,Second],[Second])'),
 ('fun_map_subtract','subtract',2,'ordsets:subtract([First,Second],[Second])'),
 ('fun_map_is_subset','is_subset',2,'ordsets:is_subset([Second],[First,Second])'),
 ('fun_map_map','map',2,'ordsets:map(fun(X)->X end,[First,Second])'),
 ('fun_map_filtermap','filtermap',2,'ordsets:filtermap(fun(X)->{true,X} end,[First,Second])'),
]
module='ordsets371_map_fun_keys'
source=f'-module({module}).\n-export([cases/0,run/0,run_stacks/0,probe/1]).\n'
source+='cases() -> ['+','.join(c[0] for c in cases)+'].\n'
source+=f'run() -> ordsets_core_support:run({module},cases()).\nrun_stacks() -> ordsets_core_support:profiles({module},cases()).\n'
source+=';\n'.join('probe('+n+') -> {First,Second}=maps_pair(), '+e for n,f,a,e in cases)+'.\n'
source+='''maps_pair() ->
    A=local_stub(255,1),
    B=local_stub(0,2),
    {uniq,1}=erlang:fun_info(A,uniq),
    {uniq,2}=erlang:fun_info(B,uniq),
    true=(A =/= B),
    {#{A=>0,B=>1},#{A=>1,B=>0}}.
local_stub(Byte,OldUnique) ->
    Unique=binary:copy(<<Byte>>,16),
    <<131,Module/binary>>=erlang:term_to_binary(ordsets371_map_key_stub),
    <<131,OldIndex/binary>>=erlang:term_to_binary(0),
    <<131,OldUniq/binary>>=erlang:term_to_binary(OldUnique),
    Owner = <<88,119,13,"external@host",0,0,0,1,0,0,0,0,0,0,0,0>>,
    Body=<<0:8,Unique/binary,0:32,0:32,Module/binary,OldIndex/binary,OldUniq/binary,Owner/binary>>,
    Size=byte_size(Body)+4,
    erlang:binary_to_term(<<131,112,Size:32,Body/binary>>).
'''
# Erlang's token =<< is ambiguous with =<; use the same lexical spacing as reviewed370.
source=source.replace('=<<','= <<')
fixturepath=OUT/'original'/f'{module}.erl'
fixturepath.write_text(source,encoding='utf-8')
profile=json.loads((ROOT/'artifacts/ordsets371-remaining-original-proposal2/tool/profile.json').read_text())
old=profile['Fixtures'][0]
fixture={k:v for k,v in old.items() if k not in ['Id','Path','Module','StackModule','Names','Cases']}
fixture.update(Id='map_fun_keys',Path='original/'+module+'.erl',Module=module,StackModule=module,Names=[c[0] for c in cases],Cases=[{'Name':n,'Mfas':[f'ordsets:{f}/{a}'],'Scope':'Whole actual result/error and full stack; portable external creator PID retained literally; no generic FUN/creator normalization.','ExpectedSeededOutcomes':False} for n,f,a,e in cases])
(OUT/'profile.fragment.json').write_text(json.dumps({'Fixtures':[fixture],'ValueCount':len(cases),'StackCount':len(cases)*5,'Mfas':sorted(set(f'ordsets:{f}/{a}' for n,f,a,e in cases))},indent=2)+'\n',encoding='utf-8')
notes={'Status':'SOURCE_COUNTEREXAMPLE_CANDIDATE_NOT_EXECUTED','SourceReason':'MapTerm CanonicalKeyStorage compares NEW_UNIQ, whereas pinned native map ordering uses source FUN key comparison OLD_UNIQ. Same two keys with reversed order fields and swapped associated numeric values isolate map key enumeration from direct FUN ordering. Actual original execution decides this witness; no native expected values are seeded.','FixtureSha256':sha(fixturepath),'CreatorProtocol':'Owner is a literal valid NEW_PID_EXT external@host id1/serial0/creation0 in every NEW_FUN_EXT. These are legal decoded code-entry stub values, not borrowed host/native pointers or fabricated runtime process IDs. Full raw FUN payloads and map/list results remain in capture.','EntryPreconditions':'Distinct NEW_UNIQ values, same actual own module/index/arity, fun_info OldUnique1/2 asserted after canonical interning. No repeated same module/index/NEW_UNIQ with conflicting old fields.','NoMainBuildNetwork':True}
(OUT/'review.json').write_text(json.dumps(notes,indent=2)+'\n',encoding='utf-8')
(OUT/'source-freeze.json').write_text(json.dumps({'Files':[{'Path':str(p.relative_to(OUT)).replace('\\','/'),'Sha256':sha(p),'Bytes':p.stat().st_size} for p in sorted(OUT.rglob('*')) if p.is_file() and p.name!='source-freeze.json']},indent=2)+'\n',encoding='utf-8')
print(json.dumps({'Cases':len(cases),'Stacks':len(cases)*5,'FixtureSha256':sha(fixturepath),'FreezeSha256':sha(OUT/'source-freeze.json')}))
