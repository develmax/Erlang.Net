"""Eleven fresh original NIF VMs; raw observations never seeded from SDK."""
from pathlib import Path
import shutil
from common import *

def main():
 args=arguments('capture');root,tool,plan,identity,owned,decoder=verify(args.repository,args.expected_head,args.manifest_sha256)
 runtime,_=approve_runtime(args,plan,identity);native=Path(args.native_build).resolve();proof=native/'native-build-receipt.json';assert digest(proof)==args.approved_native_build_receipt_sha256.upper();build=read(proof)
 assert build['Status']=='PASS_OWNED_NATIVE_ENTRY_FIXTURE_BUILD' and build['SubjectHead']==args.expected_head and build['PinnedOtpCommit']==PIN and build['SourceManifestSha256']==digest(tool/'manifest.json') and build['RuntimeBuildReceiptSha256']==digest(args.build_receipt)
 assert len(build['Images'])==plan['NativeImageCount']==len({x['Leaf'] for x in build['Images']})
 for row in build['HeaderInputs']:assert Path(row['Path']).resolve().is_relative_to(native) and digest(row['Path'])==row['Sha256']
 for item in build['OwnedStages']:
  p=Path(item['ReceiptPath']);assert p.resolve().is_relative_to(native) and digest(p)==item['ReceiptSha256'];identity.require_normal_receipt(read(p))
  for stream in ['stdout','stderr']:assert digest(p.parent/(item['Name']+'.'+stream))==item[stream.capitalize()+'Sha256']
 images={x['Leaf']:x for x in build['Images']}
 cases=read(tool/'cases.json')['Cases'];out=Path(args.output).resolve();assert not out.exists();out.mkdir(parents=True);observations=[];phase='inputs'
 try:
  write(out/'inputs.json',{'SubjectHead':args.expected_head,'PinnedOtpCommit':PIN,'SourceManifestSha256':digest(tool/'manifest.json'),'RuntimeBuildReceiptSha256':digest(args.build_receipt),'NativeBuildReceiptSha256':digest(proof),'CaseNames':[x['Name'] for x in cases],'WorkflowRunId':os.environ.get('GITHUB_RUN_ID'),'WorkflowRunAttempt':os.environ.get('GITHUB_RUN_ATTEMPT'),'OriginalOnly':True})
  for case in cases:
   directory=out/case['Name'];directory.mkdir();ebin=directory/'ebin';ebin.mkdir();phase='source-copy-'+case['Name']
   fixture=tool/case['ErlangSource'].replace('source/','',1);assert digest(fixture)==case['ErlangSourceSha256'];shutil.copyfile(fixture,directory/fixture.name);shutil.copyfile(tool/'original/entry_observer.erl',directory/'entry_observer.erl')
   image=images[case['NativeLeaf']];assert image['SourcePath']==case['NativeSource'] and image['CompilerDefinitions']==case['CompilerDefinitions']
   src=tool/image['SourcePath'].replace('source/','',1);assert image['SourceSha256']==digest(src)
   lib=Path(image['Path']);assert lib.resolve().is_relative_to(native) and digest(lib)==image['Sha256'] and lib.stat().st_size==image['Bytes']
   native_dir=directory/'native';native_dir.mkdir();shutil.copyfile(lib,native_dir/lib.name);assert digest(native_dir/lib.name)==image['Sha256']
   env=identity.sanitized_environment(os.environ);env.update(ENTRY_CAPTURE_ROOT=str(directory),ENTRY_EBIN=str(ebin),ENTRY_RUNTIME_ROOT=str(runtime),ENTRY_NATIVE_ROOT=str(native_dir),ENTRY_MODULE_SOURCE=fixture.name,ENTRY_MODULE_NAME=case['Module'],ENTRY_CASE=case['Name'],ERL_CRASH_DUMP=str(directory/'erl_crash.dump'))
   begin='D=os:getenv("ENTRY_CAPTURE_ROOT"),Ebin=os:getenv("ENTRY_EBIN"),W=fun(N,V)->ok=file:write_file(filename:join(D,N),term_to_binary(V,[{minor_version,2}])) end,ExpectedRoot=filename:absname(os:getenv("ENTRY_RUNTIME_ROOT")),ActualRoot=filename:absname(code:root_dir()),W("runtime-root.etf",{runtime_root,ExpectedRoot,ActualRoot}),ExpectedRoot=ActualRoot,'
   compile_expression=begin.replace('runtime-root.etf','compile-runtime-root.etf')+'R=try Results=[compile:file(filename:join(D,F),[{outdir,Ebin},return_errors])||F<-["entry_observer.erl",os:getenv("ENTRY_MODULE_SOURCE")]],W("compile.etf",Results),[case X of {ok,_}->ok;_->erlang:error({compile_failed,X}) end||X<-Results],{returned,compiled} catch C:E:S->W("compile-raised.etf",{C,E,S}),{raised,C,E,S} end,W("compile-outcome.etf",R),case R of {returned,compiled}->halt(0);_->halt(1) end.'
   phase='erlang-compile-'+case['Name'];stage(owned,identity,[runtime/'bin/erl','+S','2:2','-noshell','-noinput','-boot','start_clean','-start_epmd','false','-eval',compile_expression],directory,env,directory/'compile-vm',90)
   expression=begin+'{module,entry_observer}=code:load_abs(filename:join(Ebin,"entry_observer")),M=list_to_atom(os:getenv("ENTRY_MODULE_NAME")),{module,M}=code:load_abs(filename:join(Ebin,atom_to_list(M))),I=fun(Mod)->{Mod,B,P}=code:get_object_code(Mod),{ok,{Mod,MD5}}=beam_lib:md5(B),MD5=Mod:module_info(md5),{Mod,code:which(Mod),P,MD5,erlang:md5(B)} end,W("images.etf",[I(entry_observer),I(M)]),W("metadata.etf",entry_observer:metadata()),Case=list_to_atom(os:getenv("ENTRY_CASE")),R=try entry_observer:run(Case) of V->W("returned.etf",V),{returned,V} catch C:E:S->W("raised.etf",{C,E,S}),{raised,C,E,S} end,W("outcome.etf",R),case R of {returned,_}->halt(0);_->halt(1) end.'
   phase='observe-'+case['Name'];receipt=stage(owned,identity,[runtime/'bin/erl','+S','2:2','-noshell','-noinput','-boot','start_clean','-start_epmd','false','-pa',ebin,'-eval',expression],directory,env,directory/'observe-vm',45)
   root_term=decoder.decode_exact((directory/'runtime-root.etf').read_bytes());assert root_term[0]=='tuple' and root_term[1][0]==A('runtime_root') and text(root_term[1][1])==str(runtime)==text(root_term[1][2])
   verify_images(decoder.decode_exact((directory/'images.etf').read_bytes()),case,directory)
   result=decoder.decode_exact((directory/'returned.etf').read_bytes());verify_return(result,case);assert decoder.decode_exact((directory/'outcome.etf').read_bytes())==T(A('returned'),result)
   assert not (directory/'raised.etf').exists();metadata=decoder.decode_exact((directory/'metadata.etf').read_bytes());assert metadata[0]=='tuple' and metadata[1][0]==A('entry_runtime_metadata') and len(metadata[1])==6 and text(metadata[1][1])==str(runtime) and text(metadata[1][2])=='29' and metadata[1][5]==('integer',8)
   approve_runtime(args,plan,identity);assert digest(lib)==image['Sha256']
   observations.append({'Name':case['Name'],'Module':case['Module'],'SourceSha256':digest(fixture),'NativeImageSha256':image['Sha256'],'ReturnedSha256':digest(directory/'returned.etf'),'OutcomeSha256':digest(directory/'outcome.etf'),'ImagesSha256':digest(directory/'images.etf'),'ProcessReceiptSha256':receipt['ReceiptSha256'],'Status':'OBSERVED_SOURCE_CONTRACT_AND_NORMAL_CLEANUP'})
   write(out/('observations-'+str(len(observations)).zfill(2)+'.json'),observations)
  assert len(observations)==11;write(out/'completion.json',{'Status':'PASS_ORIGINAL_NATIVE_ENTRY_ELEVEN_SOURCE_CONTRACT_OBSERVATIONS','Cases':observations,'OrdinaryCorpusModified':False,'SdkOrInteropComparisonExecuted':False,'NoWholeNifCompatibilityClaim':True})
 except BaseException as error:
  write(out/'failure.json',{'Status':'FAILED_ORIGINAL_CAPTURE_RAW_RETAINED','Phase':phase,'Error':repr(error),'CompletedObservations':observations,'NoCompleteElevenClaim':True});raise
if __name__=='__main__':main()
