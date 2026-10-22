from pathlib import Path
import hashlib
import json

ROOT = Path(__file__).resolve().parents[2]
OUT = Path(__file__).resolve().parent
ORIGINAL = OUT / 'original'
ORIGINAL.mkdir(exist_ok=True)

def dump(path, data):
    path.write_text(json.dumps(data, indent=2) + '\n', encoding='utf-8')

cases = []
def add(name, expression, mfa):
    cases.append({'Name': name, 'Expression': expression, 'Mfas': [mfa], 'Scope': 'Whole value, error class, reason, BIF arguments and full caller file/line metadata; no field projection.', 'ExpectedSeededOutcomes': False})

native = 'records:create(bounded_owner,rec,[{value,1}],#{is_exported=>true})'
pid = 'binary_to_term(<<131,88,119,13,"external@host",0,0,0,1,0,0,0,0,0,0,0,0>>)'
ref = 'binary_to_term(<<131,90,0,1,119,13,"external@host",0,0,0,0,0,0,0,1>>)'
for name in ['hd', 'tl']:
    mfa = f'erlang:{name}/1'
    add(f'{name}_guard_accept', f'guard_{name}([sentinel|sentinel])', mfa)
    add(f'{name}_guard_reject', f'guard_{name}([])', mfa)
    add(f'{name}_guard_native_record', f'guard_{name}({native})', mfa)
    add(f'{name}_non_tail_success', f'context_{name}([sentinel|sentinel])', mfa)
    add(f'{name}_non_tail_badarg', f'context_{name}(invalid)', mfa)
    for label, term in [('pid',pid), ('reference',ref), ('function','fun erlang:self/0'), ('tuple','{a,b}')]:
        add(f'{name}_invalid_{label}', f'erlang370_pure_support:invoke({name},[{term}])', mfa)
    add(f'{name}_identity_payload', f'begin P={pid}, R={ref}, F=fun erlang:self/0, erlang370_pure_support:invoke({name},[[{{P,R,F}}|{{P,R,F}}]]) end', mfa)

mfa = 'erlang:tuple_size/1'
for label, expression in [('guard_accept','guard_tuple_size({a,b})'),('guard_reject','guard_tuple_size([])'),('guard_native_record',f'guard_tuple_size({native})'),('non_tail_success','context_tuple_size({a,b})'),('non_tail_badarg','context_tuple_size(invalid)'),('arity_257','erlang370_pure_support:invoke(tuple_size,[erlang:make_tuple(257,payload)])')]:
    add('tuple_size_'+label, expression, mfa)

mfa = 'erlang:is_integer/3'
for label, expression in [('guard_inside','guard_integer(5,0,10)'),('guard_outside','guard_integer(11,0,10)'),('guard_reversed','guard_integer(5,10,0)'),('guard_invalid_min','guard_integer(5,invalid,10)'),('guard_invalid_max','guard_integer(5,0,invalid)'),('guard_both_invalid','guard_integer(atom,invalid,invalid)'),('guard_native_record',f'guard_integer({native},0,10)'),('non_tail_success','context_integer(5,0,10)'),('non_tail_invalid_min','context_integer(atom,invalid,10)'),('non_tail_invalid_max','context_integer(atom,0,invalid)'),('non_tail_both_invalid','context_integer(atom,invalid,invalid)'),('big_equal','erlang370_pure_support:invoke(is_integer,[1 bsl 4096,1 bsl 4096,1 bsl 4096])'),('big_below','erlang370_pure_support:invoke(is_integer,[-(1 bsl 4096),-(1 bsl 4096)+1,1 bsl 4096])'),('big_above','erlang370_pure_support:invoke(is_integer,[1 bsl 4096,-(1 bsl 4096),(1 bsl 4096)-1])')]:
    add('is_integer_'+label, expression, mfa)
for label, term in [('nil','[]'),('cons','[a|tail]'),('tuple','{}'),('map','#{}'),('binary','<<>>'),('bits','<<1:1>>'),('pid',pid),('reference',ref),('function','fun erlang:self/0'),('native_record',native)]:
    add('is_integer_noninteger_'+label, f'erlang370_pure_support:invoke(is_integer,[{term},-(1 bsl 4096),1 bsl 4096])', mfa)

module='erlang371_bounded_contracts'
source = f'-module({module}).\n-export([cases/0,run/0,run_stacks/0,probe/1]).\n'
source += 'cases() -> [' + ','.join(c['Name'] for c in cases) + '].\n'
source += f'run() -> erlang370_pure_support:run({module},cases()).\nrun_stacks() -> erlang370_pure_support:profiles({module},cases()).\n'
source += ';\n'.join('probe('+c['Name']+') -> '+c['Expression'] for c in cases) + '.\n'
source += '''guard_hd(V) when erlang:hd(V) =:= sentinel -> true;
guard_hd(_) -> false.
guard_tl(V) when erlang:tl(V) =:= sentinel -> true;
guard_tl(_) -> false.
guard_tuple_size(V) when erlang:tuple_size(V) >= 0 -> true;
guard_tuple_size(_) -> false.
guard_integer(V,L,U) when erlang:is_integer(V,L,U) -> true;
guard_integer(_,_,_) -> false.
context_hd(V) -> Result=erlang:hd(V), {returned,Result}.
context_tl(V) -> Result=erlang:tl(V), {returned,Result}.
context_tuple_size(V) -> Result=erlang:tuple_size(V), {returned,Result}.
context_integer(V,L,U) -> Result=erlang:is_integer(V,L,U), {returned,Result}.
'''
(ORIGINAL / (module+'.erl')).write_text(source, encoding='utf-8')
old = json.loads((ROOT/'docs/validation/part-370/sources/original-first/tool/profile.json').read_text())
template = next(f for f in old['Fixtures'] if f['Id']=='size_type')
fixture = {k:v for k,v in template.items() if k not in ['Id','Path','Module','Names','Cases','ExpectedNames','CaseCount','Values','Stacks']}
fixture.update(Id='bounded_contracts', Path='original/'+module+'.erl', Module=module, Names=[c['Name'] for c in cases])
for key in list(fixture):
    if isinstance(fixture[key],list) and len(fixture[key])==217:
        fixture[key]=[c['Name'] for c in cases]
fixture['StackModule']=module
fixture['Cases']=[{k:v for k,v in c.items() if k!='Expression'} for c in cases]
dump(OUT/'profile.fragment.json',{'Fixtures':[fixture], 'Mfas':sorted(set(m for c in cases for m in c['Mfas'])), 'ValueCount':len(cases), 'StackCount':len(cases)*5})
dump(OUT/'cases.json',cases)
print(json.dumps({'Cases':len(cases),'Stacks':len(cases)*5,'FixtureKeys':list(fixture),'Sha256':hashlib.sha256((ORIGINAL/(module+'.erl')).read_bytes()).hexdigest().upper()}))
