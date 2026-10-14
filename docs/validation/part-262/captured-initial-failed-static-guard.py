from pathlib import Path
import argparse,hashlib,importlib.util,json,re,subprocess
HEAD='d2daaab10d53a4988702c5bac4d352cf3cf00fb8'
PIN='ad05823719d77c8faee87348ea39513d4e2f99c5'
ROOT=Path(__file__).resolve().parents[1]
def require(test,message):
    if not test: raise ValueError(message)
def sha(data): return hashlib.sha256(data).hexdigest().upper()
def committed(path): return subprocess.check_output(['git','-C',str(ROOT),'show',HEAD+':'+path])
spec=importlib.util.spec_from_file_location('metadata_decoder',ROOT/'tools/Erlang.BeamFixtures/verify-metadata-mutations.py')
decoder=importlib.util.module_from_spec(spec);spec.loader.exec_module(decoder)
def literal(source):
    tokens=re.findall(r'[a-z][A-Za-z0-9_@]*|-?\d+|[{}\[\],]',source)
    require(''.join(tokens)==re.sub(r'\s+','',source),'Static expected literal syntax'); index=0
    def term():
        nonlocal index
        require(index<len(tokens),'Static term truncated'); token=tokens[index];index+=1
        if token in ('{','['):
            end='}' if token=='{' else ']';items=[]
            if tokens[index]!=end:
                items.append(term())
                while tokens[index]==',':index+=1;items.append(term())
            require(tokens[index]==end,'Static term closing delimiter');index+=1
            return ('tuple' if token=='{' else 'list',tuple(items))
        require(re.fullmatch(r'[a-z][A-Za-z0-9_@]*|-?\d+',token),'Static term token')
        return ('integer',int(token)) if re.fullmatch(r'-?\d+',token) else ('atom',token)
    result=term();require(index==len(tokens),'Static trailing tokens');return result
def static():
    wrapper=committed('tools/Erlang.LoaderContrasts/loader_contrast_wrapper.erl');declbytes=committed('tools/Erlang.LoaderContrasts/predeclared-contracts.json');decl=json.loads(declbytes)
    clauses=dict(re.findall(r'^expected\("([^"]+)"\) -> (.*?)[;.][\r]?$',wrapper.decode(),re.M));require(len(clauses)==11,'Eleven static expected clauses')
    expected={}
    for name,text in clauses.items():
        alias=re.fullmatch(r'expected\("([^"]+)"\)',text)
        expected[name]=literal(clauses[alias[1]]) if alias else literal(text)
    variants=[]
    for row in decl['Scenarios']:
        name=row['Name'];variants.extend([(name+'-raw',name),(name+'-gzip',name)] if name=='wrapper-gzip-and-real-load' else [(name,name)])
    require(len(decl['Scenarios'])==10 and len(variants)==11 and set(expected)=={x[0] for x in variants},'Declared scenario/static clause mismatch')
    require(len(decl['InputBytes'])==5,'Five immutable inputs')
    for row in decl['InputBytes']: require(sha(committed(row['Path']))==row['Sha256'],'Committed input SHA '+row['Path'])
    old_wrapper=subprocess.check_output(['git','-C',str(ROOT),'show','b111d6de5a5d19ec16d0f491c1fa10d406327b73:tools/Erlang.LoaderContrasts/loader_contrast_wrapper.erl']).decode()
    old_clauses=dict(re.findall(r'^expected\("([^"]+)"\) -> (.*?)[;.][\r]?$',old_wrapper,re.M))
    require(len(old_clauses)==11,'Historical eleven static expected clauses')
    changed={name for name in clauses if clauses[name]!=old_clauses[name]}
    require(changed=={'third-generation-not-purged'},'Only explicitly ledgered static expected literal changed')
    require(expected['third-generation-not-purged']==literal('{{not_purged,[beam_fixture_constant]},{ok,42,[alpha,beta]},false,false,false,false,ok}'),'Independently pinned DidKill false')
    helper=committed('tools/Erlang.LoaderContrasts/loader_direct_caller.erl')
    require(re.fullmatch(r'\s*-module\(loader_direct_caller\)\.\s*-export\(\[direct/1\]\)\.\s*direct\(Module\)\s*->\s*erlang:call_on_load_function\(Module\)\.\s*',helper.decode()) is not None,'Minimal genuine direct caller source')
    text=wrapper.decode()
    require('-compile(no_inline).' in text and '?MODULE:dynamic_apply(erlang,call_on_load_function,[M])' in text,'Unknown-target external apply invocation')
    require('dynamic_apply(Module,Function,Arguments) -> erlang:apply(Module,Function,Arguments).' in text,'Exported unconstrained target/argument helper')
    require('exit(loader_direct_caller:direct(M))' in text,'Actual callback invokes tiny caller')
    return wrapper,declbytes,decl,expected,variants

def verify_summary(actual,expected_bytes,wanted):
    require(decoder.decode_exact(actual)==wanted,'Actual projection differs from independently parsed static literal')
    require(decoder.decode_exact(expected_bytes)==wanted,'Expected projection differs from independently parsed static literal')

def verify(directory):
    wrapper,declbytes,decl,expected,variants=static();report=json.loads((directory/'loader-contrasts.json').read_text());require(report['SourceCommit']==PIN and report['SourceTag']=='OTP-29.1.1','Pinned source identity')
    require(report['Complete'] and report['CaseCount']==10 and report['FreshVmExecutions']==11 and report['ExpectedFreshVmExecutions']==11 and report['Passed']==11,'Complete native report')
    require(report['MfaRegistrations']==0 and report['SdkExecuted'] is False,'Separate native scope')
    require(report['PredeclaredSha256']==sha(declbytes) and report['WrapperSourceSha256']==sha(wrapper),'Committed declarations/wrapper hashes')
    require((directory/'predeclared-contracts.json').read_bytes()==declbytes and (directory/'loader_contrast_wrapper.erl').read_bytes()==wrapper,'Captured committed source bytes')
    require(sha((directory/'loader_contrast_wrapper.beam').read_bytes())==report['WrapperBeamSha256'],'Compiled wrapper hash')
    for row in decl['InputBytes']:require(sha((directory/'inputs'/row['Path']).read_bytes())==row['Sha256'],'Captured genuine input')
    caller=committed('tools/Erlang.LoaderContrasts/loader_direct_caller.erl')
    require((directory/'loader_direct_caller.erl').read_bytes()==caller and report['CallerSourceSha256']==sha(caller),'Captured committed tiny caller')
    require(report['CallerBeamSha256']==sha((directory/'loader_direct_caller.beam').read_bytes()),'Actual native tiny caller BEAM hash')
    dis=(directory/'wrapper-disassembly.txt').read_text()
    start=dis.find('{function,dynamic_apply,3,');require(start>=0,'Actual exported dynamic_apply/3 disassembly')
    end=dis.find('{function,',start+1);block=dis[start:] if end<0 else dis[start:end]
    require(re.search(r'\{call_ext(?:_only|_last)?,\s*3,\s*\{extfunc,erlang,apply,3\}',block) is not None,'Actual generic apply/3 instruction')
    require('call_on_load_function' not in block,'Dynamic helper cannot retain fixed callback instruction')
    direct=(directory/'caller-disassembly.txt').read_text()
    require('{function,direct,1,' in direct and '{extfunc,erlang,call_on_load_function,1}' in direct,'Actual tiny direct instruction')
    rows=report['Results'];require(len(rows)==11,'Eleven result rows');retained=[]
    for row,(name,scenario) in zip(rows,variants):
        require(row['Name']==name and row['Scenario']==scenario,'Ordered native scenario');folder=directory/name
        require(row['ExitCode']==0 and row['TimedOut'] is False and row['RawComplete'] is True and row['Stage']=='completed' and row['Passed'] is True,'Actual success/complete stage '+name)
        listed={x['Name']:x for x in row['Files']};require(len(listed)==len(row['Files']),'No duplicate file rows')
        actualfiles={x.name for x in folder.iterdir() if x.is_file()};require(set(listed)==actualfiles,'Every raw case file retained')
        for filename,item in listed.items():
            data=(folder/filename).read_bytes();require(len(data)==item['Length'] and sha(data)==item['Sha256'],'Raw case digest '+name+'/'+filename)
        require((folder/'stage.txt').read_text()=='completed' and (folder/'process.exit').read_text().strip()=='0' and (folder/'process.raw-complete').read_text().strip()=='true','Physical sidecars '+name)
        evaluation=(folder/'evaluation.erl').read_text()
        match=re.fullmatch(r'loader_contrast_wrapper:run("([^"\n]+)","([^"\n]+)","([^"\n]+)").\n',evaluation)
        require(match is not None and match[1]==name,'Source evaluation shape '+name)
        captured_inputs,captured_case=match[2],match[3]
        require(captured_inputs.endswith('/inputs') and captured_case==captured_inputs[:-len('inputs')]+name,'Captured evaluation paths '+name)
        if retained:
            require(captured_inputs==invoked_inputs,'Stable captured input root')
        invoked_inputs=captured_inputs
        verify_summary((folder/'actual.etf').read_bytes(),(folder/'expected.etf').read_bytes(),expected[name])
        retained.extend({'Path':name+'/'+fn,'Sha256':item['Sha256']} for fn,item in listed.items() if fn.endswith('.etf'))
    return {'Status':'StaticIndependentNativeSummariesVerified','PublishedHead':HEAD,'NativeExecutions':11,'StaticDeclarations':10,'RawEtfDigests':retained}

def main():
    parser=argparse.ArgumentParser();parser.add_argument('--native-output',type=Path);parser.add_argument('--report',type=Path,required=True);args=parser.parse_args();wrapper,declbytes,decl,expected,variants=static()
    rejected=False
    try:verify_summary(bytes([131,119,5])+b'wrong',bytes([131,119,5])+b'wrong',expected[variants[0][0]])
    except ValueError:rejected=True
    require(rejected,'Actual==expected wrong corruption must fail static expected')
    result=verify(args.native_output.resolve()) if args.native_output else {'Status':'StaticDeclarationsVerifiedNativePending','PublishedHead':HEAD,'NativeExecuted':False,'DeclarationCount':10,'VariantCount':11,'WrapperSourceSha256':sha(wrapper),'DeclarationSha256':sha(declbytes),'FiveInputs':decl['InputBytes'],'Variants':[x[0] for x in variants]}
    result['EqualWrongActualExpectedRejected']=True;args.report.write_text(json.dumps(result,indent=2)+'\n');print(result['Status'])
if __name__=='__main__':main()
