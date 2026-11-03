"""Replay original698 genuine VM worker on an explicitly reviewed current working-source/runtime snapshot; no source/runtime builds."""
import argparse,hashlib,importlib.util,json,os,shutil,subprocess,sys
from pathlib import Path
sys.dont_write_bytecode=True
def sha(path):return hashlib.sha256(Path(path).read_bytes()).hexdigest().upper()
def need(value,why):
    if not value:raise RuntimeError(why)
def save(path,value):Path(path).write_bytes((json.dumps(value,indent=2)+'\n').encode())
def load(name,path):
    spec=importlib.util.spec_from_file_location(name,path);module=importlib.util.module_from_spec(spec);spec.loader.exec_module(module);return module
def checked(row):
    path=Path(row['Path']);need(path.is_file() and sha(path)==row['Sha256'],'Changed admitted input '+str(path));return path

def body(term):
    need(term[0]=='tuple' and len(term[1])==2 and term[1][1]==('atom','normal'),'Compiled monitor envelope')
    returned=term[1][0];need(returned[0]=='tuple' and len(returned[1])==2 and returned[1][0]==('atom','returned'),'Actual compiled worker returned')
    value=returned[1][1];need(value[0]=='tuple' and len(value[1])==6,'Six-part genuine observation')
    need(value[1][0]==('tuple',(('atom','value'),('atom','ok'))),'Initial genuine A NIF load must succeed before interpreting upgrade RED')
    return value[1]

def main():
    parser=argparse.ArgumentParser()
    for name in ['repository','driver-bin','output','dotnet','runtime-manifest','source-freeze']:parser.add_argument('--'+name,type=Path,required=True)
    parser.add_argument('--plan-sha256',required=True)
    parser.add_argument('--inputs',type=Path,required=True)
    parser.add_argument('--original-receipt',type=Path,required=True)
    parser.add_argument('--approved-original-receipt-sha256',required=True)
    parser.add_argument('--approved-runtime-manifest-sha256',required=True)
    parser.add_argument('--approved-source-freeze-sha256',required=True)
    parser.add_argument('--expected-head',required=True)
    parser.add_argument('--execute',action='store_true');a=parser.parse_args()
    repo=a.repository.resolve();packet=Path(__file__).resolve().parent;out=a.output.resolve()
    need(sha(a.inputs)==a.plan_sha256.upper(),'Reviewed managed baseline plan differs')
    plan=json.loads(a.inputs.read_bytes());need(plan['Pin']=='ad05823719d77c8faee87348ea39513d4e2f99c5' and plan['ExpectedHeadMode']=='exact-current-argument','Source boundary')
    need(len(a.expected_head)==40 and all(c in '0123456789abcdef' for c in a.expected_head),'Exact current head')
    need(subprocess.check_output(['git','--no-optional-locks','-C',str(repo),'rev-parse','HEAD'],text=True).strip()==a.expected_head,'Current checkout differs')
    plan['ScopeHead']=a.expected_head
    def static_guard():
        for row in plan['Inputs']:
            root=packet if row['Root']=='Tool' else repo
            path=(root/row['Path']).resolve();need(path.is_relative_to(root),'Escaping portable source input')
            checked(dict(Path=str(path),Sha256=row['Sha256']))
    static_guard()
    retained=checked(dict(Path=str(a.runtime_manifest.resolve()),Sha256=a.approved_runtime_manifest_sha256.upper()))
    current=json.loads(retained.read_bytes())
    need(current['Status']=='ROOT_REVIEWED_CURRENT_RUNTIME_N2' and current['Head']==plan['ScopeHead']
         and current['WorkingTreeSnapshot'] is True and current['Configuration'] in ['Release','Debug']
         and current['BuildExitCode']==0,'Actual reviewed current build/working snapshot required, not retained07B7')
    freeze=checked(dict(Path=str(a.source_freeze.resolve()),Sha256=a.approved_source_freeze_sha256.upper()))
    need(Path(current['SourceFreeze']['Path']).resolve()==freeze.resolve()
         and current['SourceFreeze']['Sha256']==a.approved_source_freeze_sha256.upper(),'Build/source freeze association differs')
    build_evidence=current['BuildEvidence'];checked(build_evidence)
    source_rows=json.loads(freeze.read_bytes())['Rows'];need(bool(source_rows),'Empty actual working-source freeze')
    source_inputs=[]
    for row in source_rows:
        path=(repo/row['Path']).resolve();need(path.is_relative_to(repo),'Escaping source snapshot')
        source_inputs.append(dict(Path=str(path),Sha256=row['Sha256']))
    retained_root=Path(current['RuntimeDirectory']).resolve();need(retained_root.is_dir(),'Current reviewed runtime directory absent')
    runtime_files=current['Files'];need(bool(runtime_files),'Empty reviewed runtime closure')
    for row in runtime_files:need(Path(row['Path']).resolve().is_relative_to(retained_root),'Escaping runtime file snapshot')
    def overlay_guard():
        need(subprocess.check_output(['git','--no-optional-locks','-C',str(repo),'rev-parse','HEAD'],text=True).strip()==a.expected_head,'Current checkout changed during capture')
        # Coverage/hash of actual working inputs proves this overlay, not Git HEAD alone.
        changed=subprocess.check_output(['git','--no-optional-locks','-C',str(repo),'diff',plan['ScopeHead'],'--name-only','-z'],text=True).split('\0')
        untracked=subprocess.check_output(['git','--no-optional-locks','-C',str(repo),'ls-files','--others','--exclude-standard','-z'],text=True).split('\0')
        relevant={name.replace('\\','/') for name in changed+untracked if name and
            (Path(name).suffix in {'.cs','.csproj','.props','.targets'} or name in {'global.json','Directory.Build.props','Directory.Build.targets'})}
        covered={row['Path'].replace('\\','/') for row in source_rows}
        need(relevant<=covered,'Unfrozen compiled working overlay: '+repr(sorted(relevant-covered)))
    def guard():
        static_guard()
        for row in runtime_files+source_inputs+[build_evidence,dict(Path=str(retained),Sha256=a.approved_runtime_manifest_sha256.upper()),dict(Path=str(freeze),Sha256=a.approved_source_freeze_sha256.upper())]:checked(row)
        actual={str(path.resolve()) for path in retained_root.rglob('*') if path.is_file()}
        need(actual=={str(Path(row['Path']).resolve()) for row in runtime_files},'Runtime closure set changed')
        overlay_guard()
    guard()
    # Derived in memory ONLY after explicit reviewed manifest and linked source hashes pass; never saved as automatic admission.
    plan['RetainedManifest']=dict(Path=str(retained),Sha256=a.approved_runtime_manifest_sha256.upper())
    plan['RetainedFiles']=runtime_files
    need(a.original_receipt.is_file(),'Actual fresh original receipt must be explicitly reviewed in sealed inputs, not auto-admitted')
    plan['OriginalReceipt']=dict(Path=str(a.original_receipt.resolve()),Sha256=a.approved_original_receipt_sha256.upper())
    original_receipt=checked(plan['OriginalReceipt']);original=original_receipt.parent;oracle=json.loads(original_receipt.read_bytes())
    need(oracle['Passed'] and oracle['Status']=='UPGRADE693_ORIGINAL_BASELINE' and oracle['Count']==12 and oracle['FreshOriginalCompilation'] and oracle.get('CleanupProtocol')=='OLD_A_UNLOAD_BEFORE_CURRENT_PURGE_709' and oracle.get('OriginalProducerPlanSha256')==plan['OriginalPlanSha256'] and oracle.get('SubjectHead')==a.expected_head and oracle.get('Platform')==('linux-x64-lp64' if sys.platform.startswith('linux') else 'windows-x64-llp64'),'Original genuine baseline not complete')
    original_rows=oracle['OutputFiles'];need(bool(original_rows),'Fresh original producer lacks output seals')
    for row in original_rows:need(Path(row['Path']).resolve().is_relative_to(original),'Escaping produced original output')
    def original_guard():
        for row in original_rows:checked(row)
        checked(plan['OriginalReceipt'])
        need({str(p.resolve()) for p in original.rglob('*') if p.is_file()}=={str(Path(row['Path']).resolve()) for row in original_rows}|{str(original_receipt.resolve())},'Fresh original output set changed')
    original_guard()
    prep=json.loads((original/'preparation/completion.json').read_bytes());beams=prep['Beams'];need(prep['Passed'],'Fresh original BEAM preparation')
    native=json.loads((original/'native-admissions.json').read_bytes());images=native['Images'];cases=[row['Case'] for row in oracle['Rows']]
    need(len(cases)==12 and all(row['Passed'] for row in oracle['Rows']),'All original cases acquired')
    driver_files=[dict(Path=str(a.driver_bin/name),Sha256=sha(a.driver_bin/name)) for name in ['Upgrade709.dll','Upgrade709.deps.json','Upgrade709.runtimeconfig.json']]
    need(a.dotnet.is_file() and sha(a.dotnet)==current['RuntimeHost']['Sha256'] and a.dotnet.resolve()==Path(current['RuntimeHost']['Path']).resolve(),'Reviewed dotnet host')
    need(not out.exists() and out.is_relative_to(repo/'artifacts/nif-upgrade693'),'Fresh managed output')
    if not a.execute:print(json.dumps(dict(Prepared=True,Executed=False,Cases=12,Routes=2,RetainedFiles=len(plan['RetainedFiles']))));return
    out.mkdir(parents=True)
    decoder=load('upgrade697_etf',repo/'tools/Distribution/Erlang.GuestConnectedSendObservations/passive_etf.py')
    original_common=load('upgrade697_common',packet/'common.py');env=original_common.environment();linux=sys.platform.startswith('linux')
    for key in ['DOTNET_STARTUP_HOOKS','DOTNET_ADDITIONAL_DEPS']:env.pop(key,None)
    stage=original_common.stages(repo,linux,env)
    errors=[];rows=[]
    def phase(argv,d):
        try:return dict(Passed=True,Receipt=stage(argv,d,'managed',150))
        except Exception as error:
            errors.append(dict(Directory=str(d),Phase='managed',Error=repr(error)));return dict(Passed=False,Error=repr(error))
    try:
        for case in cases:
            d=out/case['Id'];d.mkdir();entry=dict(Id=case['Id'],Case=case,Routes={},Comparisons={})
            golden=original/case['Id'];original_body=body(decoder.decode_exact((golden/'observed.etf').read_bytes()))
            for route in ['reviewed','ordinary']:
                rd=d/route;rd.mkdir()
                try:
                    private=rd/'host';shutil.copytree(retained_root,private)
                    for file in plan['RetainedFiles']:
                        rel=Path(file['Path']).resolve().relative_to(retained_root);need(sha(private/rel)==file['Sha256'],'Copied retained binary differs')
                    for file in driver_files:
                        target=private/Path(file['Path']).name;need(not target.exists(),'Driver collision');shutil.copyfile(file['Path'],target)
                    selected=[]
                    for key in ['a42',case['NativeB']]:
                        item=dict(images[key]);proof=native['WrongEntryBuild'] if key=='n1_wrong_name' else native['GenuineBuild']
                        item.update(BuildReceipt=proof['Path'],BuildReceiptSha256=proof['Sha256']);selected.append(item)
                    beam2=beams['subject-v1' if case['BeamChecksum']=='same' else 'subject-v2']['Path']
                    inputs=[plan['OriginalReceipt'],plan['RetainedManifest'],dict(Path=str(freeze),Sha256=a.approved_source_freeze_sha256.upper()),build_evidence,*source_inputs,native['GenuineBuild'],native['WrongEntryBuild'],beams['subject-v1'],beams['subject-v2'],beams['observer'],*selected]
                    admission=dict(SubjectHead=a.expected_head,CleanupProtocol='OLD_A_UNLOAD_BEFORE_CURRENT_PURGE_709',Status='APPROVED_PORTABLE_N2_CURRENT_SOURCE',CurrentSourceReplay701=True,SourceFreezeSha256=a.approved_source_freeze_sha256.upper(),Pin=plan['Pin'],Case=case['Id'],Route=route,
                        RuntimeManifest=str(retained),RuntimeManifestSha256=plan['RetainedManifest']['Sha256'],Images=selected,
                        ObserverBeam=beams['observer']['Path'],Beam1=beams['subject-v1']['Path'],Beam2=beam2,
                        Mode=case['Mode'],ReloadOnly=case['ReloadOnly'],Inputs=inputs)
                    if linux:
                        wrong_build=json.loads(Path(native['WrongEntryBuild']['Path']).read_bytes());bridge=wrong_build['Bridge'];proof=json.loads(checked(bridge['BuildReceipt']).read_bytes())
                        need(proof['Status']=='VERSION651_LINUX_BRIDGE_BUILD' and proof['Pin']==plan['Pin'] and proof['SubjectHead']==a.expected_head and proof['BridgePath']==bridge['Library']['Path'] and proof['BridgeSha256']==bridge['Library']['Sha256'],'Existing Linux bridge identity')
                        admission['Bridge']=dict(Path=bridge['Library']['Path'],Sha256=bridge['Library']['Sha256'],ReceiptPath=bridge['BuildReceipt']['Path'],ReceiptSha256=bridge['BuildReceipt']['Sha256'])
                        admission['Inputs'] += [bridge['Library'],bridge['BuildReceipt']]
                        deployed=private/Path(bridge['Library']['Path']).name
                        need(not deployed.is_symlink(),'Private bridge cannot be a symlink')
                        if deployed.exists():need(deployed.is_file() and sha(deployed)==bridge['Library']['Sha256'],'Existing private bridge differs')
                        else:shutil.copyfile(bridge['Library']['Path'],deployed)
                        need(sha(deployed)==bridge['Library']['Sha256'],'Deployed private bridge differs')
                        admission['Inputs'].append(dict(Path=str(deployed.resolve()),Sha256=bridge['Library']['Sha256']))
                    ap=rd/'admission.json';save(ap,admission)
                    result=phase([a.dotnet,'exec','--fx-version',current['RuntimeHost']['FrameworkVersion'],private/'Upgrade709.dll',ap,sha(ap),rd/'capture'],rd)
                    cp=rd/'capture/completion.json'
                    if cp.is_file():
                        receipt=json.loads(cp.read_bytes());result['CaptureReceipt']=receipt
                        if not (receipt['Passed'] and receipt.get('PortableN2') is True and receipt.get('SubjectHead')==a.expected_head and receipt['RuntimeClosed'] and receipt['AllAcceptedNativeOwnersUnloaded'] and receipt['Route']==route
                            and receipt['RuntimeManifestSha256']==plan['RetainedManifest']['Sha256'] and receipt['CurrentSourceReplay701'] and receipt.get('CleanupProtocol')=='OLD_A_UNLOAD_BEFORE_CURRENT_PURGE_709'
                            and receipt['SourceFreezeSha256']==a.approved_source_freeze_sha256.upper()):result['Passed']=False
                    else:result['Passed']=False
                    entry['Routes'][route]=result
                    observed=rd/'capture/observed.etf';events=rd/'capture/events.log'
                    if observed.is_file() and events.is_file():
                        source=(golden/'observed.etf').read_bytes();actual=observed.read_bytes();a_term=decoder.decode_exact(source);b_term=decoder.decode_exact(actual)
                        comparison=dict(WholeRawOutcomeEquality=source==actual,WholeTypedOutcomeEquality=a_term==b_term,
                            WholeRawEventEquality=(golden/'events.log').read_bytes()==events.read_bytes(),OriginalSha256=sha(golden/'observed.etf'),ManagedSha256=sha(observed),
                            OriginalEventsSha256=sha(golden/'events.log'),ManagedEventsSha256=sha(events),
                            WholeRawOldPhaseEquality=(golden/'old-unload-phase.etf').read_bytes()==(rd/'capture/old-unload-phase.etf').read_bytes())
                        entry['Comparisons'][route]=comparison
                        managed_body=body(b_term)
                        comparison['InitialGenuineLoadPassed']=True
                        comparison['ActualUpgrade']=repr(managed_body[2]);comparison['OriginalUpgrade']=repr(original_body[2])
                        cleanup=rd/'capture/code-cleanup.etf'
                        if cleanup.is_file():comparison['ManagedHostCleanupArtifactSha256']=sha(cleanup)  # Different controller APIs: deliberately not cross-compared.
                        if not all(comparison[k] for k in ['WholeRawOutcomeEquality','WholeTypedOutcomeEquality','WholeRawEventEquality','WholeRawOldPhaseEquality']):
                            errors.append(dict(Case=case['Id'],Route=route,Error='Complete original value/stack or semantic event stream differs; no normalization'))
                    else:errors.append(dict(Case=case['Id'],Route=route,Error='Missing complete captured streams'))
                except Exception as error:
                    entry['Routes'].setdefault(route,{})['Passed']=False;entry['Routes'][route].setdefault('Errors',[]).append(repr(error));errors.append(dict(Case=case['Id'],Route=route,Error=repr(error)))
            rows.append(entry);save(d/'completion.json',entry)
    finally:
        try:
            guard()
            original_guard()
            for row in driver_files:checked(row)
        except Exception as error:errors.append(dict(Phase='integrity-after',Error=repr(error)))
    comparisons=[c for row in rows for c in row['Comparisons'].values()]
    executions=all(set(row['Routes'])=={'reviewed','ordinary'} and all(r.get('Passed') for r in row['Routes'].values()) for row in rows)
    complete=len(rows)==12 and len(comparisons)==24
    matched=sum(all(c[k] for k in ['WholeRawOutcomeEquality','WholeTypedOutcomeEquality','WholeRawEventEquality','WholeRawOldPhaseEquality']) for c in comparisons)
    result=dict(Passed=complete and executions and matched==24 and not errors,ExecutionPassed=complete and executions,Count=12,Comparisons=len(comparisons),MatchedComparisons=matched,Rows=rows,Errors=errors,
        ReplayedAlreadyAcquiredOriginal=True,OriginalReceiptSha256=plan['OriginalReceipt']['Sha256'],RuntimeManifestSha256=plan['RetainedManifest']['Sha256'],SourceFreezeSha256=a.approved_source_freeze_sha256.upper(),CurrentSourceReplay701=True,SubjectHead=a.expected_head,PortableN2=True,WorkingSourceRows=len(source_rows),PristineCommitClaim=False,
        CleanupProtocol='OLD_A_UNLOAD_BEFORE_CURRENT_PURGE_709',RawOutcomesRewritten=False,PointerAddressesNormalized=False,WholeN2Ready=False,WholeApiCredits=0)
    save(out/'completion.json',result);print(json.dumps(dict(Passed=result['Passed'],ExecutionPassed=result['ExecutionPassed'],Comparisons=len(comparisons),Matched=matched)))
    return 0 if result['Passed'] else 1
if __name__=='__main__':raise SystemExit(main())
