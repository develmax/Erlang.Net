"""One fresh original VM; return actual terms and valid-output sentinel observations."""
import shutil
from common import *

def semantic(value):
    # The passive decoder attaches source-file offsets to NEW_FUN_EXT values.
    # Compare all decoded fields while excluding only that file-location metadata.
    if isinstance(value, tuple):
        if value and value[0] == 'new_fun':
            assert value[-1][0] == 'raw_span'
            return tuple(semantic(x) for x in value[:-1])
        return tuple(semantic(x) for x in value)
    return value

def main():
    args = arguments('capture')
    root,tool,plan,identity,owned,decoder = verify(args.repository,args.expected_head,args.manifest_sha256)
    runtime,_ = approve_runtime(args,plan,identity)
    native = Path(args.native_build).resolve()
    proof = native/'native-build-receipt.json'
    assert digest(proof) == args.approved_native_build_receipt_sha256.upper()
    build = read(proof)
    assert build['Status'] == 'PASS_OWNED_NATIVE_ENTRY_FIXTURE_BUILD'
    assert build['SubjectHead'] == args.expected_head and build['PinnedOtpCommit'] == PIN
    assert build['SourceManifestSha256'] == digest(tool/'manifest.json')
    assert build['RuntimeBuildReceiptSha256'] == digest(args.build_receipt)
    assert len(build['Images']) == 1
    for row in build['HeaderInputs']:
        path = Path(row['Path']).resolve()
        assert path.is_relative_to(native) and digest(path) == row['Sha256']
    for row in build['OwnedStages']:
        receipt = Path(row['ReceiptPath']).resolve()
        assert receipt.is_relative_to(native) and digest(receipt) == row['ReceiptSha256']
        identity.require_normal_receipt(read(receipt))
        for stream in ['stdout','stderr']:
            assert digest(receipt.parent/(row['Name']+'.'+stream)) == row[stream.capitalize()+'Sha256']
    image = build['Images'][0]
    assert image['Leaf'] == 'map_size_callbacks' and image['CompilerDefinitions'] == []
    assert image['SourcePath'] == 'source/native/map_size_callback_contract.c'
    assert image['SourceSha256'] == digest(tool/'native/map_size_callback_contract.c')
    library = Path(image['Path']).resolve()
    assert library.is_relative_to(native) and digest(library) == image['Sha256']
    out = Path(args.output).resolve()
    assert not out.exists()
    out.mkdir(parents=True)
    phase = 'inputs'
    stages = []
    try:
        shutil.copyfile(proof,out/'native-build-receipt.json')
        shutil.copyfile(tool/'manifest.json',out/'source-manifest.json')
        shutil.copyfile(tool/'cases.json',out/'cases.json')
        write(out/'inputs.json',{'SubjectHead':args.expected_head,'PinnedOtpCommit':PIN,'SourceManifestSha256':digest(tool/'manifest.json'),'RuntimeBuildReceiptSha256':digest(args.build_receipt),'NativeBuildReceiptSha256':digest(proof),'OriginalOnly':True,'SdkExecuted':False})
        ebin = out/'ebin'
        ebin.mkdir()
        for name in ['entry_observer.erl','map_size_callback_contract.erl']:
            shutil.copyfile(tool/'original'/name,out/name)
        native_dir = out/'native'
        native_dir.mkdir()
        shutil.copyfile(library,native_dir/library.name)
        assert digest(native_dir/library.name) == image['Sha256']
        env = identity.sanitized_environment(os.environ)
        env.update(ENTRY_CAPTURE_ROOT=str(out),ENTRY_EBIN=str(ebin),ENTRY_RUNTIME_ROOT=str(runtime),ENTRY_NATIVE_ROOT=str(native_dir),ERL_CRASH_DUMP=str(out/'erl_crash.dump'))
        begin = 'D=os:getenv("ENTRY_CAPTURE_ROOT"),Ebin=os:getenv("ENTRY_EBIN"),W=fun(N,V)->ok=file:write_file(filename:join(D,N),term_to_binary(V,[{minor_version,2}])) end,ExpectedRoot=filename:absname(os:getenv("ENTRY_RUNTIME_ROOT")),ActualRoot=filename:absname(code:root_dir()),W("runtime-root.etf",{runtime_root,ExpectedRoot,ActualRoot}),ExpectedRoot=ActualRoot,'
        compile_expr = begin.replace('runtime-root.etf','compile-runtime-root.etf')+'R=try Results=[compile:file(filename:join(D,F),[{outdir,Ebin},return_errors])||F<-["entry_observer.erl","map_size_callback_contract.erl"]],W("compile.etf",Results),[case X of {ok,_}->ok;_->erlang:error({compile_failed,X}) end||X<-Results],{returned,compiled} catch C:E:S->W("compile-raised.etf",{C,E,S}),{raised,C,E,S} end,W("compile-outcome.etf",R),case R of {returned,compiled}->halt(0);_->halt(1) end.'
        phase = 'compile'
        stages.append(stage(owned,identity,[runtime/'bin/erl','+S','2:2','-noshell','-noinput','-boot','start_clean','-start_epmd','false','-eval',compile_expr],out,env,out/'compile-vm',90))
        expression = begin+'[{module,M}=code:load_abs(filename:join(Ebin,atom_to_list(M)))||M<-[entry_observer,map_size_callback_contract]],I=fun(Mod)->{Mod,B,P}=code:get_object_code(Mod),{ok,{Mod,MD5}}=beam_lib:md5(B),MD5=Mod:module_info(md5),{Mod,code:which(Mod),P,MD5,erlang:md5(B)} end,W("images.etf",[I(entry_observer),I(map_size_callback_contract)]),W("metadata.etf",entry_observer:metadata()),R=try entry_observer:run() of V->W("returned.etf",V),{returned,V} catch C:E:S->W("raised.etf",{C,E,S}),{raised,C,E,S} end,W("outcome.etf",R),case R of {returned,_}->halt(0);_->halt(1) end.'
        phase = 'observe'
        stages.append(stage(owned,identity,[runtime/'bin/erl','+S','2:2','-noshell','-noinput','-boot','start_clean','-start_epmd','false','-pa',ebin,'-eval',expression],out,env,out/'observe-vm',60))
        returned = decoder.decode_exact((out/'returned.etf').read_bytes())
        assert returned[0] == 'tuple' and len(returned[1]) == 4 and returned[1][0] == A('map_size_callback_contract')
        names = read(tool/'cases.json')['ObservationNames']
        rows = returned[1][1]
        assert rows[0] == 'list' and [x[1][0] for x in rows[1]] == [A(x) for x in names]
        widths = returned[1][2]
        assert widths == ('list',tuple(('integer',x) for x in [4,8,8,40]))
        assert returned[1][3] == T(A('cleanup'),A('deleted_and_purged'))
        assert semantic(decoder.decode_exact((out/'outcome.etf').read_bytes())) == semantic(T(A('returned'),returned))
        assert not (out/'raised.etf').exists()
        verify_images(decoder.decode_exact((out/'images.etf').read_bytes()),{'Module':'map_size_callback_contract'},out)
        root_value = decoder.decode_exact((out/'runtime-root.etf').read_bytes())
        assert root_value == T(A('runtime_root'),root_value[1][1],root_value[1][2]) and text(root_value[1][1]) == str(runtime) == text(root_value[1][2])
        metadata = decoder.decode_exact((out/'metadata.etf').read_bytes())
        assert metadata[0] == 'tuple' and len(metadata[1]) == 6 and metadata[1][0] == A('map_size_callback_metadata')
        assert text(metadata[1][1]) == str(runtime) and text(metadata[1][2]) == '29' and metadata[1][5] == ('integer',8)
        approve_runtime(args,plan,identity)
        assert digest(library) == image['Sha256']
        write(out/'completion.json',{'Status':'PASS_ORIGINAL_MAP_SIZE_CALLBACK_SELECTED_CONTRACTS','SubjectHead':args.expected_head,'PinnedOtpCommit':PIN,'SourceManifestSha256':digest(tool/'manifest.json'),'NativeBuildReceiptSha256':digest(proof),'NativeSourceSha256':image['SourceSha256'],'ErlangSourceSha256':digest(tool/'original/map_size_callback_contract.erl'),'ObservationNames':names,'Widths':[4,8,8,40],'ReturnedSha256':digest(out/'returned.etf'),'OutcomeSha256':digest(out/'outcome.etf'),'MetadataSha256':digest(out/'metadata.etf'),'ImagesSha256':digest(out/'images.etf'),'OwnedStages':stages,'SdkOrInteropComparisonExecuted':False,'NoWholeNifCompatibilityClaim':True})
    except BaseException as error:
        write(out/'failure.json',{'Status':'FAILED_RAW_RETAINED','Phase':phase,'Error':repr(error),'OwnedStages':stages})
        raise

if __name__ == '__main__':
    main()
