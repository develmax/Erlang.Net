from pathlib import Path
import json,subprocess,hashlib
p=Path('artifacts');load=lambda s:json.loads(Path(s).read_text(encoding='utf-8-sig'));sha=lambda b:hashlib.sha256(b).hexdigest().upper()
a=load('artifacts/part-255-differential/differential.json')['ModuleResults'][3605];b=load('docs/validation/part-243/differential.json')['ModuleResults'][3605]
assert a['Passed'] and b['Passed'] and a['StandardStreamsMatch'] and b['StandardStreamsMatch']
assert a['ExpectedStandardOutputHex']==b['ExpectedStandardOutputHex']=='237B613D3E7B307D2C623D3E2241227D0D0A'
assert a['ExpectedStandardOutputAlternatives']==b['ExpectedStandardOutputAlternatives']==['237B623D3E2241222C613D3E7B307D7D0D0A']
allowed=[a['ExpectedStandardOutputHex']]+a['ExpectedStandardOutputAlternatives']
assert all(x['NativeStandardOutputHex'] in allowed and x['SdkStandardOutputHex'] in allowed and x['NativeStandardErrorHex']==x['SdkStandardErrorHex']==x['ExpectedStandardErrorHex']=='' for x in [a,b])
files=[]
for path in ['tools/Erlang.Differential/TermDisplayCases.cs','tools/Erlang.Differential/CompiledStandardOutputContract.cs','tools/Erlang.Differential/Program.cs']:
 raw=subprocess.check_output(['git','show','babb093864c1025d3a9dd8a1d1327d381fc5d7dd:'+path]); files.append({'Head':'babb093864c1025d3a9dd8a1d1327d381fc5d7dd','Path':path,'Sha256':sha(raw)})
 if path.endswith('TermDisplayCases.cs'): assert b'ExpectedStandardOutputAlternatives' in raw and b'cases[^1]' in raw
 if path.endswith('CompiledStandardOutputContract.cs'): assert b'fixture.ExpectedStandardOutputAlternatives?.Contains(outputHex, StringComparer.Ordinal)' in raw
 if path.endswith('Program.cs'): assert b'bool passed = actual.Equals(expected) && expected.Equals(fixture.Expected) && streamsMatch;' in raw
pin='ad05823719d77c8faee87348ea39513d4e2f99c5'
assert subprocess.check_output(['git','-C','../otp','rev-parse','OTP-29.1.1']).decode().strip()==pin
for path in ['erts/emulator/beam/erl_utils.h','erts/emulator/beam/erl_map.c','erts/emulator/beam/erl_printf_term.c']:
 raw=subprocess.check_output(['git','-C','../otp','show','OTP-29.1.1:'+path]); files.append({'Head':pin,'Path':path,'Sha256':sha(raw)})
 if path.endswith('erl_utils.h'): assert b'erts_cmp_flatmap_keys' in raw and b'return key_a - key_b;' in raw
 if path.endswith('erl_map.c'): assert b'erts_validate_and_sort_flatmap' in raw and b'erts_cmp_flatmap_keys(ks[jx],ks[jx-1])' in raw
 if path.endswith('erl_printf_term.c'): assert b'ks = flatmap_get_keys(head);' in raw
out={'Status':'PASS','ObservedRows':{'Historical243':b,'Published255':a},'FixturePolicyPredatesCurrentObservation':True,'BothRawNativeOutputsWithinUnchangedSourceDeclaredAlternatives':True,'NoRawOutputNormalization':True,'AllHistoricalRowsExactlyEqual':False,'Sources':files,'NativeSourceContext':'Flatmap keys use internal raw atom term ordering (erl_utils.h:267), validate/sort with that comparator (erl_map.c:3187), and printer traverses stored flatmap keys (erl_printf_term.c MAP_DEF).','PhysicalCauseLimit':'These two executions did not capture atom table IDs or BEAM disassembly, so the precise reason for the changed atom insertion order remains unproved.'}
(p/'part255-map-print-observation-policy-audit.json').write_text(json.dumps(out,indent=2)+'\n')
print('PASS unchanged predeclared map-print alternatives and pinned native storage/printing context; precise atom-order cause unproved')
