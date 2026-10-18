"""Artifact-only source proposal. No SDK build, original execution, network or Main writes."""
from pathlib import Path
import hashlib,json,shutil,subprocess
ROOT=Path(__file__).resolve().parents[2]
OUT=Path(__file__).resolve().parent
BASE=ROOT/'artifacts/erlang-original370-closure1/preparation1/tool'
TOOL=OUT/'tool'
PIN='ad05823719d77c8faee87348ea39513d4e2f99c5'
H=lambda p:hashlib.sha256(p.read_bytes()).hexdigest().upper()
def write(p,v):p.parent.mkdir(parents=True,exist_ok=True);p.write_text(json.dumps(v,indent=2)+'\n',encoding='utf-8')
def main():
    assert not TOOL.exists();TOOL.mkdir()
    for n in ['ci-gates.py','collect-original-corrected.py','passive_etf_family.py','preloaded_provenance.py']:
        shutil.copyfile(BASE/n,TOOL/n)
    shutil.copytree(BASE/'parse-gate',TOOL/'parse-gate',ignore=shutil.ignore_patterns('bin','obj'))
    (TOOL/'original').mkdir()
    for n in ['erlang370_pure_support.erl','erlang370_observation_worker.erl']:shutil.copyfile(BASE/'original'/n,TOOL/'original'/n)
    previous=json.loads((ROOT/'artifacts/erlang-original370-pure1/preparation1/tool/profile.json').read_bytes())
    template=json.loads((BASE/'profile.json').read_bytes())
    groups=[]
    def group(id):
        g=dict(Id=id,Module='erlang371_'+id,Path='original/erlang371_'+id+'.erl',Names=[],Cases=[],Location='tool',Run='run',NamesEntry='cases',StackModule='erlang371_'+id,StackEntry='run_stacks',WorkerModule='erlang370_observation_worker',WorkerEntry='run',ValueAcquisition=template['ValueAcquisition'],ObserveTimeoutSeconds=600,WorkerTimeoutMilliseconds=540000,StackShape='depth_cases',OutcomeSchema='pure-full-values-and-stacks-v1',Companions=['original/erlang370_pure_support.erl','original/erlang370_observation_worker.erl'])
        g['_clauses']=[];groups.append(g);return g
    def case(g,name,function,args,non_tail=False):
        assert name not in g['Names'];g['Names'].append(name)
        mfa='erlang:'+function+'/'+str(len(args));assert mfa in template['TargetMfas'] and mfa not in previous['Mfas']
        g['Cases'].append(dict(Name=name,Mfas=[mfa],Scope='Source-defined numeric/text conversion input; exact returned term and complete raw error stack at all five depths.'))
        expr='erlang370_pure_support:invoke('+function+',['+','.join(args)+'])'
        if non_tail:expr='Result='+expr+',{continued,Result}'
        g['_clauses'].append('probe('+name+') -> '+expr)
    integers=['0','1','-1','255','-255','9223372036854775807','-9223372036854775808','18446744073709551616','12345678901234567890123456789012345678901234567890']
    invalid=['a','1.0','[]','<<>>','{1}']
    g=group('integer_format')
    for f in ['integer_to_list','integer_to_binary']:
        for i,v in enumerate(integers+invalid):case(g,f+'_unary_'+str(i),f,[v])
        for i,v in enumerate(integers):
            for b in [2,3,8,10,16,32,36]:case(g,f+'_value_'+str(i)+'_base_'+str(b),f,[v,str(b)])
        for i,b in enumerate(['1','0','-2','37','256','2.0','a','[]']):case(g,f+'_badbase_'+str(i),f,['42',b],True)
        for i,v in enumerate(invalid):case(g,f+'_badvalue_'+str(i),f,[v,'16'],True)
    g=group('integer_parse')
    texts=['"0"','"-0"','"+0"','"42"','"-42"','"+42"','"00042"','"9223372036854775808"','"-9223372036854775809"','""','" 42"','"42 "','"+"','"-"','"1.0"','"1e2"','"0x10"','[49,0,50]','[49,256]','[49|tail]','[a]']
    for f in ['list_to_integer','binary_to_integer']:
        for i,v in enumerate(texts):
            if f.startswith('binary') and v.startswith('['):continue
            arg='list_to_binary('+v+')' if f.startswith('binary') else v
            case(g,f+'_unary_'+str(i),f,[arg])
        for i,(v,b) in enumerate([('"101010"','2'),('"77"','8'),('"ff"','16'),('"FF"','16'),('"-Ff"','16'),('"z"','36'),('"Z"','36'),('"2"','2'),('"g"','16'),('"42"','1'),('"42"','37'),('"42"','2.0'),('"42"','a'),('""','10')]):
            arg='list_to_binary('+v+')' if f.startswith('binary') else v
            case(g,f+'_base_'+str(i),f,[arg,b],True)
        for i,v in enumerate(['a','42','1.0','{}','<<1:1>>']):
            case(g,f+'_badtype_'+str(i),f,[v],True)
            case(g,f+'_badtype_base_'+str(i),f,[v,'10'],True)
    g=group('float_parse')
    texts=['"0.0"','"-0.0"','"+0.0"','"1.25"','"-1.25"','"1.0e3"','"1.0E+3"','"1.0e-3"','"1.7976931348623157e308"','"2.2250738585072014e-308"','"4.9406564584124654e-324"','"1.0e-9999"','"1.0e309"','""','"1"','"1e2"','".5"','"1."','" 1.0"','"1.0 "','"nan"','"inf"','"-infinity"','"1.0x"','[49,46,48,0]','[49,46,48|tail]','[49,46,256]']
    for f in ['list_to_float','binary_to_float']:
        for i,v in enumerate(texts):
            if f.startswith('binary') and v.startswith('['):continue
            arg='list_to_binary('+v+')' if f.startswith('binary') else v
            case(g,f+'_input_'+str(i),f,[arg],i%2==0)
        for i,v in enumerate(['a','42','1.0','{}','[]','<<1:1>>']):case(g,f+'_badtype_'+str(i),f,[v],True)
    g=group('float_format')
    floats=['0.0','-0.0','1.0','-1.25','1.2345678901234567','1.0e20','1.0e-20','1.7976931348623157e308','2.2250738585072014e-308','erlang370_pure_support:float_bits(1)']
    options=['[]','[short]','[{decimals,0}]','[{decimals,2}]','[{decimals,20}]','[{decimals,253}]','[{decimals,2},compact]','[{scientific,0}]','[{scientific,6}]','[{scientific,20}]','[{scientific,249}]','[compact]','[short,compact]','[{decimals,2},{scientific,4}]','[{scientific,4},{decimals,2}]']
    badopts=['a','[short|tail]','[unknown]','[{decimals,-1}]','[{decimals,254}]','[{scientific,-1}]','[{scientific,250}]','[{decimals,2.0}]','[{scientific,a}]','[{decimals}]','[{decimals,2,3}]','[{}]']
    for f in ['float_to_list','float_to_binary']:
        for i,v in enumerate(floats):
            case(g,f+'_unary_'+str(i),f,[v])
            for j,opt in enumerate(options):case(g,f+'_value_'+str(i)+'_option_'+str(j),f,[v,opt])
        for i,opt in enumerate(badopts):case(g,f+'_badoption_'+str(i),f,['1.25',opt],True)
        for i,v in enumerate(['0','1','-1','a','[]','<<>>','{}']):
            case(g,f+'_badvalue_'+str(i),f,[v],True)
            case(g,f+'_badvalue_options_'+str(i),f,[v,'[short]'],True)
    for g in groups:
        clauses=g.pop('_clauses')
        text='-module('+g['Module']+').\n-export([cases/0,run/0,run_stacks/0,probe/1]).\ncases() -> ['+','.join(g['Names'])+'].\nrun() -> erlang370_pure_support:run('+g['Module']+',cases()).\nrun_stacks() -> erlang370_pure_support:profiles('+g['Module']+',cases()).\n'+';\n'.join(clauses)+'.\n'
        (TOOL/g['Path']).write_text(text,encoding='utf-8')
    profile=dict(template);profile.update(Family='erlang-numeric-conversion14',Fixtures=groups,Mfas=sorted({m for g in groups for c in g['Cases'] for m in c['Mfas']}),Scope='New-only14 MFAs outside preceding57. No compatibility promotion by corpus count alone.')
    profile['ExpectedValues']=sum(len(g['Names']) for g in groups);profile['ExpectedProfiles']=5*profile['ExpectedValues']
    profile['FixtureSources']=[dict(Location='tool',Path=p.relative_to(TOOL).as_posix(),Sha256=H(p)) for p in sorted((TOOL/'original').glob('*.erl'))];profile['ExpectedUniqueSourceCount']=len(profile['FixtureSources'])
    write(TOOL/'profile.json',profile)
    plan=json.loads((BASE/'source-inputs.corrected.json').read_bytes());plan['Status']='ERLANG371_NUMERIC_CONVERSION14_SOURCE_ONLY';plan['ToolInputs']=[dict(Path=p.relative_to(TOOL).as_posix(),Bytes=p.stat().st_size,Sha256=H(p)) for p in sorted(TOOL.rglob('*')) if p.is_file()]
    write(TOOL/'source-inputs.corrected.json',plan)
    sourcefiles=['erts/emulator/beam/bif.c','erts/emulator/beam/erl_bif_binary.c','erts/emulator/beam/erl_bif_lists.c','erts/emulator/beam/erl_term.h']
    authority=[]
    for name in sourcefiles:
        result=subprocess.run(['git','-C',str(ROOT.parent/'otp'),'show',PIN+':'+name],capture_output=True)
        if result.returncode==0:
            path=OUT/'authority'/name;path.parent.mkdir(parents=True,exist_ok=True);path.write_bytes(result.stdout);authority.append(dict(Path=name,Sha256=H(path)))
    write(OUT/'selection.json',dict(PinnedOtpCommit=PIN,SelectedMfas=profile['Mfas'],DisjointPrevious57=True,PreviousMfas=previous['Mfas'],Values=profile['ExpectedValues'],Profiles=profile['ExpectedProfiles'],Sources=authority,PreparedOnly=True))
    print(json.dumps(dict(SelectedMfas=len(profile['Mfas']),Values=profile['ExpectedValues'],Profiles=profile['ExpectedProfiles'],Manifest=H(TOOL/'source-inputs.corrected.json'))))
if __name__=='__main__':main()
