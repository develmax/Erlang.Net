from pathlib import Path
import hashlib,importlib.util,json,struct,sys
HERE=Path(__file__).resolve().parent
ROOT=HERE.parents[1]
def load(name,path):
    spec=importlib.util.spec_from_file_location(name,path)
    module=importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module
new=load('new_decoder',HERE/'passive_etf_family.py')
old=load('old_decoder',ROOT/'artifacts/ordsets371-remaining-original-proposal2/tool/passive_etf_family.py')
def atom(value):
    raw=value.encode()
    return bytes([119,len(raw)])+raw
def small(value): return bytes([97,value])
def integer(value): return bytes([98])+struct.pack('>i',value)
def pid(): return bytes([88])+atom('external@host')+struct.pack('>III',1,0,0)
def fun(byte=255,olduniq=1,arity=0,index=0,module=None,oldindex=None,creator=None,free=()):
    body=bytes([arity])+bytes([byte])*16+struct.pack('>II',index,len(free))+(module if module is not None else atom('ordsets371_map_key_stub'))+(oldindex if oldindex is not None else small(0))+small(olduniq)+(creator if creator is not None else pid())+b''.join(free)
    return bytes([112])+struct.pack('>I',len(body)+4)+body
def mapping(pairs): return bytes([131,116])+struct.pack('>I',len(pairs))+b''.join(k+v for k,v in pairs)
checks=[]
def check(name,predicate):
    if not predicate: raise AssertionError(name)
    checks.append({'Name':name,'Status':'PASS'})
def reject(name,data):
    try: new.decode_exact(data)
    except (ValueError,UnicodeError):
        checks.append({'Name':name,'Status':'PASS_REJECTED'})
    else: raise AssertionError(name)

A,B=fun(),fun(0,2)
forward=mapping([(A,small(0)),(B,small(1))])
reverse=mapping([(B,small(1)),(A,small(0))])
value,spans=new.decode_exact_with_fun_spans(forward)
other,other_spans=new.decode_exact_with_fun_spans(reverse)
check('physical_pair_order_independent_semantics',value==other)
check('all_fun_map_keys_present',len(value[1])==2 and all(k[0]=='new_fun' and len(k)==10 for k,v in value[1]))
check('two_structural_source_spans',len(spans)==2)
for i,span in enumerate(spans):
    raw=forward[span['Start']:span['Start']+span['Length']]
    check('span_'+str(i)+'_byte_hash',hashlib.sha256(raw).hexdigest().upper()==span['Sha256'])
    check('span_'+str(i)+'_declared_size',raw[0]==112 and int.from_bytes(raw[1:5],'big')==span['DeclaredSize'] and len(raw)==span['DeclaredSize']+1)
check('spans_are_outside_semantic_tuple',all(len(k)==10 for k,v in value[1]))
for name,data in [('integer',bytes([131])+integer(-3)),('list',bytes([131,108])+struct.pack('>I',2)+small(1)+small(2)+bytes([106])),('map',mapping([(atom('k'),small(3))])),('float',bytes([131,70])+struct.pack('>d',2.0))]:
    check('baseline_unchanged_'+name,new.decode_exact(data)==old.decode_exact(data))
old_value=old.decode_exact(bytes([131])+A)
new_value=new.decode_exact(bytes([131])+A)
check('all_new_fun_semantic_fields_retained',old_value[:-1]==new_value and old_value[-1][0]=='raw_span')
for name,changed in [('new_uniq',fun(254)),('old_uniq',fun(255,2)),('arity',fun(255,1,1)),('index',fun(255,1,0,1)),('module',fun(module=atom('different_stub'))),('old_index',fun(oldindex=small(1))),('creator',fun(creator=bytes([88])+atom('external@host')+struct.pack('>III',2,0,0)))]:
    check('strict_fun_field_'+name,new.decode_exact(bytes([131])+changed)!=new_value)
reject('duplicate_new_fun_keys',mapping([(A,small(0)),(A,small(1))]))
reject('free_environment_new_fun_key',mapping([(fun(free=(small(1),)),small(0))]))
reject('negative_old_index_key',mapping([(fun(oldindex=integer(-1)),small(0))]))
reject('wrong_module_type',mapping([(fun(module=small(1)),small(0))]))
reject('wrong_old_index_type',mapping([(fun(oldindex=atom('bad')),small(0))]))
reject('wrong_creator_type',mapping([(fun(creator=atom('bad')),small(0))]))
for trim in [1,2,5,16,30]: reject('truncated_fun_'+str(trim),bytes([131])+A[:-trim])
bad_size=bytearray(A)
bad_size[1:5]=(len(A)+100).to_bytes(4,'big')
reject('declared_size_exceeds_transport',bytes([131])+bad_size)
bad_size[1:5]=(4).to_bytes(4,'big')
reject('declared_size_underflows_fields',bytes([131])+bad_size)
reject('trailing_data',forward+b'\x00')
reject('unsupported_compressed',bytes([131,80])+b'\x00'*8)
reject('unsupported_old_fun',bytes([131,117])+b'\x00'*8)
reject('aggregate_map_count_budget',bytes([131,116])+b'\xff'*4)
reject('malformed_bit_padding',bytes([131,77])+struct.pack('>I',1)+bytes([1,255]))
report={'Status':'PASS','Checks':len(checks),'DecoderSha256':hashlib.sha256((HERE/'passive_etf_family.py').read_bytes()).hexdigest().upper(),'Results':checks,'RawCapturePolicy':'Source ETF bytes unchanged; no execution/evaluation, identity substitution or generic normalization.'}
out=Path(sys.argv[1]) if len(sys.argv)>1 else None
if out:
    if out.exists(): raise FileExistsError(out)
    out.write_text(json.dumps(report,indent=2)+'\n',encoding='utf-8')
print(json.dumps({'Status':report['Status'],'Checks':len(checks),'DecoderSha256':report['DecoderSha256']}))
