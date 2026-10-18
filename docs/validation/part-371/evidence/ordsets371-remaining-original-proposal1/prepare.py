"""Source-only supplementary original fixtures for the remaining thirteen ordsets exports."""
from pathlib import Path
import hashlib,json,subprocess
HERE=Path(__file__).resolve().parent;ROOT=HERE.parents[1]
H=lambda p:hashlib.sha256(p.read_bytes()).hexdigest().upper()
def main():
    base=json.loads((ROOT/'artifacts/next-api371-proposal1/tool/profile.json').read_bytes());template=base['Fixtures'][0]
    groups=[];fixtures=HERE/'original';fixtures.mkdir(exist_ok=False)
    def group(id):
        g=dict(template);g.update(Id='ordsets_'+id,Path='original/ordsets371_'+id+'.erl',Module='ordsets371_'+id,StackModule='ordsets371_'+id,Names=[],Cases=[]);g['_clauses']=[];groups.append(g);return g
    def case(g,name,f,args,expression=None):
        assert name not in g['Names'];g['Names'].append(name)
        g['Cases'].append(dict(Name=name,Mfas=['ordsets:'+f+'/'+str(len(args))],Scope='Source-defined ordered-set value/type/traversal boundary; entire value and complete five-depth error stack retained.'))
        if expression is None:expression='erlang:apply(ordsets,'+f+',['+','.join(args)+'])'
        g['_clauses'].append('probe('+name+') -> '+expression)
    g=group('set_operations')
    lists=['[]','[a]','[1,2,3]','[3,2,1]','[1,1,2]','[1,1.0,2]','[1.0,1,2]','[1,a,{x},#{},[],<<>>]','[a|tail]','tail','{}','<<>>']
    for i,s in enumerate(lists):case(g,'from_list_'+str(i),'from_list',[s])
    descending='['+','.join(str(i) for i in range(64,-1,-1))+']';case(g,'from_list_merge_boundary','from_list',[descending])
    case(g,'from_list_numeric_stability','from_list',['[2.0,1.0,2,1,-0.0,0]'])
    for i,s in enumerate(lists):
        for j,e in enumerate(['0','1.0','a','z']):case(g,'del_element_'+str(i)+'_'+str(j),'del_element',[e,s])
    pairs=[('[]','[]'),('[1,3,5]','[2,4,6]'),('[1,2,3]','[2,3,4]'),('[2,3,4]','[1,2,3]'),('[1]','[1.0]'),('[1.0]','[1]'),('[1,1,2]','[1,2,2]'),('[3,2,1]','[1,2,3]'),('[]','tail'),('tail','[]'),('tail','[a]'),('[a]','tail'),('[1|tail]','[0]'),('[1|tail]','[2]'),('[1]','[0|tail]'),('[a|tail]','[a|other]'),('[#{1=>a}]','[#{1.0=>a}]'),('[{1},[1],<<1>>]','[{1.0},[1.0],<<2>>]')]
    for f in ['union','intersection','is_disjoint','subtract','is_subset']:
        for i,(a,b) in enumerate(pairs):case(g,f+'_pair_'+str(i),f,[a,b])
    outer=['[]','[[]]','[[1,2,3]]','[[1,3],[2,4]]','[[1],[1.0]]','[[1.0],[1]]','[[1,2],[2,3],[2,4]]','[[],tail]','[tail,[]]','[tail]','[tail,[a]]','[[a],tail]','[[1|tail],[2]]','[[1],[0|tail]]','[[1,2]|tail]','tail','{}']
    for f in ['union','intersection']:
        for i,s in enumerate(outer):case(g,f+'_outer_'+str(i),f,[s])
    g=group('callbacks')
    module=g['Module']
    callback='fun '+module+':'
    sets=['[]','[1,2,3,4,5,6]','[6,5,4,3,2,1]','[1,1.0,2]','[a]','[1|tail]','tail','{}']
    for i,s in enumerate(sets):
        case(g,'fold_order_'+str(i),'fold',[callback+'fold_callback/2','[]',s])
        case(g,'filter_even_'+str(i),'filter',[callback+'filter_even/1',s])
        case(g,'map_reverse_'+str(i),'map',[callback+'map_reverse/1',s])
        case(g,'filtermap_mixed_'+str(i),'filtermap',[callback+'filtermap_mixed/1',s])
    for f in ['fold','filter','map','filtermap']:
        arity=2 if f=='fold' else 1
        for i,fun in enumerate(['invalid','fun erlang:self/0','fun erlang:abs/1','fun erlang:'+'/2']):
            for j,s in enumerate(['[]','[1]','[1,2]','[1|tail]']):
                args=[fun,'initial',s] if f=='fold' else [fun,s]
                case(g,f+'_callback_kind_'+str(i)+'_set_'+str(j),f,args)
        for kind in ['error','throw','exit','invalid_return']:
            name=kind+('_two' if arity==2 else '_one')
            args=[callback+name+'/'+str(arity),'initial','[1,2]'] if f=='fold' else [callback+name+'/'+str(arity),'[1,2]']
            case(g,f+'_'+kind,f,args)
    case(g,'map_deduplicates','map',[callback+'map_zero/1','[1,2,3]'])
    case(g,'filtermap_deduplicates','filtermap',[callback+'filtermap_zero/1','[1,2,3]'])
    case(g,'filtermap_keep_identity','filtermap',[callback+'keep_identity/1','[1,1.0,2]'])
    # External function references have stable serialized identity; local FUN image hashes are not projected away.
    helpers='''
fold_callback(E,A) -> [{visited,E}|A].
filter_even(X) -> X rem 2 =:= 0.
map_reverse(X) -> -X.
map_zero(_) -> 0.
filtermap_zero(_) -> {true,0}.
keep_identity(_) -> true.
filtermap_mixed(X) -> case X rem 3 of 0 -> true; 1 -> false; 2 -> {true,-X} end.
error_one(X) -> erlang:error({callback_failure,X}).
throw_one(X) -> erlang:throw({callback_failure,X}).
exit_one(X) -> erlang:exit({callback_failure,X}).
invalid_return_one(_) -> invalid_return.
error_two(X,A) -> erlang:error({callback_failure,X,A}).
throw_two(X,A) -> erlang:throw({callback_failure,X,A}).
exit_two(X,A) -> erlang:exit({callback_failure,X,A}).
invalid_return_two(_,_) -> invalid_return.
'''
    exports='fold_callback/2,filter_even/1,map_reverse/1,map_zero/1,filtermap_zero/1,keep_identity/1,filtermap_mixed/1,error_one/1,throw_one/1,exit_one/1,invalid_return_one/1,error_two/2,throw_two/2,exit_two/2,invalid_return_two/2'
    for g in groups:
        clauses=g.pop('_clauses');extra='-export(['+exports+']).\n' if g['Id']=='ordsets_callbacks' else ''
        text='-module('+g['Module']+').\n-export([cases/0,run/0,run_stacks/0,probe/1]).\n'+extra+'cases() -> ['+','.join(g['Names'])+'].\nrun() -> ordsets_core_support:run('+g['Module']+',cases()).\nrun_stacks() -> ordsets_core_support:profiles('+g['Module']+',cases()).\n'+';\n'.join(clauses)+'.\n'
        if extra:text+=helpers
        (HERE/g['Path']).write_text(text,encoding='utf-8')
    mfas=sorted({m for g in groups for c in g['Cases'] for m in c['Mfas']});assert len(mfas)==13 and not(set(mfas)&set(base['Mfas']))
    counts=sum(len(g['Names']) for g in groups)
    profile=dict(Fixtures=groups,SelectedMfas=mfas,ExpectedValues=counts,ExpectedProfiles=counts*5,ExpectedUniqueAdditionalSourceCount=2,SeededOutcomes=False,Scope='Remaining13 plus existing8 cover21 exports; compare full fields before complete-contract classification.')
    (HERE/'profile.fragment.json').write_text(json.dumps(profile,indent=2)+'\n',encoding='utf-8')
    raw=subprocess.check_output(['git','-C',str(ROOT.parent/'otp'),'show','ad05823719d77c8faee87348ea39513d4e2f99c5:lib/stdlib/src/ordsets.erl']);(HERE/'pinned-ordsets.erl').write_bytes(raw)
    (HERE/'README.md').write_text('Source-only remaining13 fixture supplement. Merge Fixtures into existing ordsets core8 profile; recompute all Mfas, FixtureSources, unique source count5, values and profiles, tool manifest. Keep original8 fixture/support/worker exact. Two independent cohorts preserve full failures and matched workers. Callback external fun references avoid image-dependent local FUN identities without hiding any stack fields. Malformed inputs intentionally follow actual native clauses, including empty-set shortcuts. No code, build, runtime or network executed.\n',encoding='utf-8')
    seal=dict(Status='PREPARED_SOURCE_ONLY',Files=[dict(Path=p.relative_to(HERE).as_posix(),Bytes=p.stat().st_size,Sha256=H(p)) for p in sorted(HERE.rglob('*')) if p.is_file() and p.name!='source-seal.json'])
    (HERE/'source-seal.json').write_text(json.dumps(seal,indent=2)+'\n',encoding='utf-8')
    print(json.dumps(dict(AdditionalMfas=len(mfas),Values=counts,Profiles=counts*5,Full21Values=112+counts,Full21Profiles=560+counts*5,Seal=H(HERE/'source-seal.json'))))
if __name__=='__main__':main()
