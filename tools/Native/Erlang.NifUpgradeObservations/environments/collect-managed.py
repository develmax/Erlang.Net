"""Execute only an explicitly reviewed current-source driver/runtime; never build."""
import argparse,json,os,shutil,subprocess,sys
from pathlib import Path
from common import *

def checked_row(item):return checked(item['Path'],item['Sha256'])

def main():
    p=argparse.ArgumentParser()
    for key in ['repository','output','driver-bin','dotnet','runtime-manifest','source-freeze','original-receipt']:p.add_argument('--'+key,type=Path,required=True)
    for key in ['expected-head','plan-sha256','approved-original-receipt-sha256','approved-runtime-manifest-sha256','approved-source-freeze-sha256','approved-driver-manifest-sha256']:p.add_argument('--'+key,required=True)
    p.add_argument('--driver-manifest',type=Path,required=True)
    p.add_argument('--bridge-receipt',type=Path);p.add_argument('--approved-bridge-receipt-sha256')
    p.add_argument('--execute',action='store_true')
    a=p.parse_args();repo=a.repository.resolve();head=select_head(repo,a.expected_head);packet=Path(__file__).resolve().parent;linux=sys.platform.startswith('linux')
    plan=guard(repo,packet,a.plan_sha256,'managed-inputs.json');out=a.output.resolve()
    need(not out.exists() and out.is_relative_to(repo/'artifacts/nif-upgrade693'),'Fresh owned managed output')
    retained=checked(a.runtime_manifest,a.approved_runtime_manifest_sha256);current=json.loads(retained.read_bytes())
    need(current['Status']=='ROOT_REVIEWED_CURRENT_RUNTIME_N2' and current['Head']==head and current['WorkingTreeSnapshot'] is True and current['Configuration'] in ['Debug','Release'] and current['BuildExitCode']==0,'Reviewed actual current runtime build')
    freeze=checked(a.source_freeze,a.approved_source_freeze_sha256)
    need(Path(current['SourceFreeze']['Path']).resolve()==freeze and current['SourceFreeze']['Sha256']==a.approved_source_freeze_sha256.upper(),'Actual build/source association')
    source_rows=json.loads(freeze.read_bytes())['Rows'];need(bool(source_rows),'Nonempty source freeze')
    source_inputs=[]
    for item in source_rows:
        path=(repo/item['Path']).resolve();need(path.is_relative_to(repo),'Source containment');source_inputs.append(dict(Path=str(path),Sha256=item['Sha256']))
    files=current['Files'];runtime_root=Path(current['RuntimeDirectory']).resolve();need(bool(files),'Actual runtime closure')
    for item in files:need(Path(item['Path']).resolve().is_relative_to(runtime_root),'Runtime containment')
    def runtime_guard():
        select_head(repo,head)
        for item in files+source_inputs+[current['BuildEvidence']]:checked_row(item)
        checked(retained,a.approved_runtime_manifest_sha256);checked(freeze,a.approved_source_freeze_sha256)
        need({str(p.resolve()) for p in runtime_root.rglob('*') if p.is_file()}=={str(Path(r['Path']).resolve()) for r in files},'Runtime full file set')
        changes=subprocess.check_output(['git','--no-optional-locks','-C',str(repo),'diff',head,'--name-only','-z'],text=True).split('\0')
        others=subprocess.check_output(['git','--no-optional-locks','-C',str(repo),'ls-files','--others','--exclude-standard','-z'],text=True).split('\0')
        relevant={n.replace('\\','/') for n in changes+others if n and (Path(n).suffix in {'.cs','.csproj','.props','.targets'} or n in {'global.json','Directory.Build.props','Directory.Build.targets'})}
        covered={r['Path'].replace('\\','/') for r in source_rows};need(relevant<=covered,'Unfrozen actual source: '+repr(sorted(relevant-covered)))
    runtime_guard()
    driver_receipt=checked(a.driver_manifest,a.approved_driver_manifest_sha256);driver=json.loads(driver_receipt.read_bytes())
    need(driver['Status']=='ENV709_DRIVER_BUILD' and driver['Head']==head and driver['BuildExitCode']==0 and driver['RuntimeManifestSha256']==a.approved_runtime_manifest_sha256.upper(),'Actual driver build explicitly approved by supplied receipt hash')
    driver_files=driver['Files'];need({Path(r['Path']).name for r in driver_files}=={'Env709.dll','Env709.deps.json','Env709.runtimeconfig.json'},'Exact driver artifact set')
    for item in driver_files:need(Path(item['Path']).resolve().parent==a.driver_bin.resolve(),'Driver output containment');checked_row(item)
    for item in driver['SourceFiles']:
        checked_row(item);need(Path(item['Path']).resolve().is_relative_to(packet/'driver'),'Admitted driver sources')
    need({str(Path(r['Path']).resolve()) for r in driver['SourceFiles']}=={str(p.resolve()) for p in (packet/'driver').glob('*') if p.is_file()},'Complete driver source set')
    checked_row(driver['BuildEvidence'])
    need(a.dotnet.resolve()==Path(current['RuntimeHost']['Path']).resolve() and sha(a.dotnet)==current['RuntimeHost']['Sha256'],'Authenticated dotnet host')
    oracle_path=checked(a.original_receipt,a.approved_original_receipt_sha256);oracle=json.loads(oracle_path.read_bytes());original=oracle_path.parent
    need(oracle['Status']=='ENV709_ORIGINAL' and oracle['Pin']==PIN and oracle['Passed'] and oracle['SubjectHead']==head and oracle['OriginalPlanSha256']==plan['OriginalPlanSha256'] and len(oracle['Rows'])==2 and all(r['Passed'] for r in oracle['Rows']),'Complete fresh admitted original')
    original_files=oracle['OutputFiles']
    def original_guard():
        checked(oracle_path,a.approved_original_receipt_sha256)
        for item in original_files:need(Path(item['Path']).resolve().is_relative_to(original),'Original output containment');checked_row(item)
        need({str(p.resolve()) for p in original.rglob('*') if p.is_file()}=={str(Path(r['Path']).resolve()) for r in original_files}|{str(oracle_path)},'Original full output set')
    original_guard()
    native_receipt=checked_row(oracle['NativeBuild']);native=json.loads(native_receipt.read_bytes())
    need(native['Passed'] and native['Status']=='ENV709_NATIVE_BUILD' and native['SubjectHead']==head and native['PlanSha256']==plan['NativeBuildPlanSha256'] and len(native['Images'])==1,'Genuine one-DLL native producer')
    image=native['Images'][0]['Library'];checked_row(image)
    need(Path(image['Path']).resolve().is_relative_to(native_receipt.parent),'Native output containment')
    beams=oracle['Beams']
    for item in beams.values():checked_row(item)
    bridge=None
    if linux:
        need(a.bridge_receipt is not None and a.approved_bridge_receipt_sha256 is not None,'Explicit existing Linux bridge receipt')
        bridge_path=checked(a.bridge_receipt,a.approved_bridge_receipt_sha256);proof=json.loads(bridge_path.read_bytes())
        need(proof['Status']=='VERSION651_LINUX_BRIDGE_BUILD' and proof['Pin']==PIN and proof['SubjectHead']==head and proof['NativeLoaded'] is False,'Reviewed existing Linux bridge')
        need({'enif_release_resource','enif_keep_resource','nif_bridge_register'}<=set(proof['Exports']),'Existing bridge exports')
        checked(proof['BridgePath'],proof['BridgeSha256'])
        bridge=dict(Path=proof['BridgePath'],Sha256=proof['BridgeSha256'],ReceiptPath=str(bridge_path),ReceiptSha256=a.approved_bridge_receipt_sha256.upper())
    if not a.execute:print(json.dumps(dict(Prepared=True,Executed=False,Cases=2,Routes=2)));return 0
    out.mkdir();env=environment();stage=stages(repo,linux,env);decoder=load('env709_etf',repo/'tools/Distribution/Erlang.GuestConnectedSendObservations/passive_etf.py');results=[];errors=[];deployed_rows=[]
    for mode in [0,1]:
        name='accept' if mode==0 else 'veto';golden=original/name
        for route in ['reviewed','ordinary']:
            d=out/(name+'-'+route);d.mkdir();record=dict(Mode=mode,Route=route,Passed=False)
            try:
                runtime_guard();original_guard()
                private=d/'host';shutil.copytree(runtime_root,private)
                for item in files:need(sha(private/Path(item['Path']).resolve().relative_to(runtime_root))==item['Sha256'],'Copied runtime differs')
                for item in driver_files:
                    checked_row(item);target=private/Path(item['Path']).name;need(not target.exists(),'Driver collision');shutil.copyfile(item['Path'],target)
                selected=dict(image,BuildReceipt=str(native_receipt),BuildReceiptSha256=sha(native_receipt))
                inputs=[row(oracle_path),row(retained),row(freeze),row(driver_receipt),current['BuildEvidence'],*source_inputs,*beams.values(),row(native_receipt),image]
                admission=dict(Status='APPROVED_ENV709_CURRENT_SOURCE',Pin=PIN,SubjectHead=head,CurrentSourceReplay701=True,CleanupProtocol='OLD_A_UNLOAD_BEFORE_CURRENT_PURGE_709',SourceFreezeSha256=a.approved_source_freeze_sha256.upper(),Route=route,RuntimeManifest=str(retained),RuntimeManifestSha256=a.approved_runtime_manifest_sha256.upper(),Images=[selected],ObserverBeam=beams['observer']['Path'],Beam1=beams['subject-v1']['Path'],Beam2=beams['subject-v2']['Path'],Mode=mode,Inputs=inputs)
                if bridge is not None:
                    admission['Bridge']=bridge
                    admission['Inputs']+=[dict(Path=bridge['Path'],Sha256=bridge['Sha256']),dict(Path=bridge['ReceiptPath'],Sha256=bridge['ReceiptSha256'])]
                    deployed=private/Path(bridge['Path']).name
                    need(not deployed.is_symlink(),'Private bridge cannot be a symlink')
                    if deployed.exists():need(deployed.is_file() and sha(deployed)==bridge['Sha256'],'Existing private bridge differs')
                    else:shutil.copyfile(bridge['Path'],deployed)
                    need(sha(deployed)==bridge['Sha256'],'Deployed private bridge differs')
                    deployed_row=dict(Path=str(deployed.resolve()),Sha256=bridge['Sha256'])
                    admission['Inputs'].append(deployed_row);deployed_rows.append(deployed_row)
                ap=d/'admission.json';save(ap,admission)
                record['Execution']=stage([a.dotnet,'exec','--fx-version',current['RuntimeHost']['FrameworkVersion'],private/'Env709.dll',ap,sha(ap),d/'capture'],d,'managed',150)
                cp=d/'capture/completion.json';receipt=json.loads(cp.read_bytes())
                need(receipt['Passed'] and receipt['RuntimeClosed'] and receipt['AllAcceptedNativeOwnersUnloaded'] and receipt['Route']==route and receipt['SubjectHead']==head and receipt['RuntimeManifestSha256']==a.approved_runtime_manifest_sha256.upper() and receipt['SourceFreezeSha256']==a.approved_source_freeze_sha256.upper(),'Actual capture/cleanup')
                actual=(d/'capture/observed.etf').read_bytes();expected=(golden/'observed.etf').read_bytes()
                comparison=dict(WholeRawOutcomeEquality=actual==expected,WholeTypedOutcomeEquality=decoder.decode_exact(actual)==decoder.decode_exact(expected),WholeRawEventEquality=(d/'capture/events.log').read_bytes()==(golden/'events.log').read_bytes(),WholeRawOldPhaseEquality=(d/'capture/old-unload-phase.etf').read_bytes()==(golden/'old-unload-phase.etf').read_bytes())
                record['Comparisons']=comparison;record['CaptureReceipt']=row(cp);record['Passed']=all(comparison.values())
                if not record['Passed']:errors.append(dict(Mode=mode,Route=route,Error='Complete raw/typed outcome/stack/events differ; no projection'))
            except Exception as error:record['Error']=repr(error);errors.append(dict(Mode=mode,Route=route,Error=repr(error)))
            results.append(record);save(d/'completion.json',record)
    try:
        runtime_guard();original_guard();guard(repo,packet,a.plan_sha256,'managed-inputs.json')
        checked(driver_receipt,a.approved_driver_manifest_sha256)
        for item in driver_files+driver['SourceFiles']+[driver['BuildEvidence']]:checked_row(item)
        if bridge is not None:
            checked(bridge['Path'],bridge['Sha256']);checked(bridge['ReceiptPath'],bridge['ReceiptSha256'])
        for item in deployed_rows:
            need(not Path(item['Path']).is_symlink(),'Private bridge became a symlink');checked_row(item)
    except Exception as error:errors.append(dict(Phase='final-inputs',Error=repr(error)))
    passed=len(results)==4 and all(r['Passed'] for r in results) and not errors
    save(out/'completion.json',dict(Status='ENV709_MANAGED',Passed=passed,SubjectHead=head,Rows=results,Errors=errors,RawOutcomesRewritten=False,PointerAddressesNormalized=False,WholeApiCredits=0))
    return 0 if passed else 1
if __name__=='__main__':raise SystemExit(main())
