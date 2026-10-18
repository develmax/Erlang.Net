"""Passive profile controls only. This executes no SDK, native image or OTP VM."""
from pathlib import Path
import importlib.util,struct
T=Path(__file__).resolve().parent
s=importlib.util.spec_from_file_location('family_decoder_control',T/'passive_etf_family.py');m=importlib.util.module_from_spec(s);s.loader.exec_module(m)
f=lambda x:b'\x46'+struct.pack('>d',x)
i=lambda x:bytes([97,x])
a=lambda s:bytes([119,len(s)])+s.encode('ascii')
map_etf=lambda pairs:b'\x83\x74'+len(pairs).to_bytes(4,'big')+b''.join(k+v for k,v in pairs)
controls=[]
def control(name,body):body();controls.append(name)
def invalid(data):
    try:m.decode_exact(data)
    except ValueError:return
    raise AssertionError('Invalid/out-of-profile data accepted')
def distinct():
    result=m.decode_exact(map_etf([(i(1),a('integer')),(f(1.0),a('float'))]));assert result[0]=='map' and len(result[1])==2
control('integer1_and_float1_distinct',distinct)
control('other_finite_float_admitted',lambda:m.decode_exact(map_etf([(f(2.5),a('ok'))])))
control('truncated_float8_rejected',lambda:invalid(b'\x83\x46'+b'\x00'*7))
control('duplicate_float_map_key_rejected',lambda:invalid(map_etf([(f(1.0),a('a')),(f(1.0),a('b'))])))
control('nonfinite_float_map_key_rejected',lambda:invalid(map_etf([(f(float('inf')),a('a'))])))
control('negative_zero_key_explicitly_outside_profile',lambda:invalid(map_etf([(f(-0.0),a('a'))])))
control('positive_zero_key_admitted',lambda:m.decode_exact(map_etf([(f(0.0),a('a'))])))
# Syntax compilation is passive; no collector main entrypoint is executed.
for path in T.glob('*.py'):compile(path.read_bytes(),str(path),'exec')
print('PASS_PASSIVE_DECODER_CONTROLS '+str(len(controls)))
# Additional passive comparison admission controls.
s=importlib.util.spec_from_file_location('family_comparison_control',T/'compare.py');c=importlib.util.module_from_spec(s);s.loader.exec_module(c)
def must_reject(body):
    try:body()
    except (AssertionError,ValueError):return
    raise AssertionError('Malformed row admission accepted')
control('row_count_mismatch_rejected',lambda:must_reject(lambda:c.named_rows(('list',()),['a'])))
control('wrong_case_name_rejected',lambda:must_reject(lambda:c.named_rows(('list',(('tuple',(('atom','b'),('atom','done'))),)),['a'])))
def order_policy():
    a=('tuple',(('atom','ok'),('list',(('integer',1),('integer',2)))))
    b=('tuple',(('atom','ok'),('list',(('integer',2),('integer',1)))))
    assert not c.equal(a,b,'exact') and c.equal(a,b,'unordered_list')
control('explicit_unordered_policy_only',order_policy)
print('PASS_ALL_PASSIVE_CONTROLS '+str(len(controls)))
