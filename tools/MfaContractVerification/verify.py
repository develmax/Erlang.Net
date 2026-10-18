"""Local manifest-driven verification. Never installs, publishes, or applies ledger changes."""
from pathlib import Path
import argparse, importlib.util, json, os, shutil, sys, time
sys.dont_write_bytecode = True
from contract_constants import *
from contracts import admit, digest, load_catalog, read, resolve, select, write

ROOT = Path(__file__).resolve().parents[2]
TOOL = Path(__file__).resolve().parent

def load(path, name):
    spec = importlib.util.spec_from_file_location(name, path)
    module = importlib.util.module_from_spec(spec); spec.loader.exec_module(module); return module

def arguments():
    p = argparse.ArgumentParser()
    p.add_argument('command', choices=['plan', 'run'])
    p.add_argument('--catalog', default=str(TOOL / 'catalog.json'))
    p.add_argument('--mfa', action='append'); p.add_argument('--family'); p.add_argument('--queue', choices=['pending']); p.add_argument('--all', action='store_true')
    p.add_argument('--output', required=True); p.add_argument('--dotnet', default=shutil.which('dotnet'))
    p.add_argument('--original-context'); p.add_argument('--original-context-sha256')
    return p.parse_args()

def main():
    a = arguments(); output = Path(a.output).resolve()
    if not output.is_relative_to(ROOT / 'artifacts'): raise ValueError('Output must be a fresh repository artifacts directory.')
    output.mkdir(parents=True, exist_ok=False)
    stages = []; source_rows = []; selected = []; fixtures = []; statuses = {}; definitions = {}; outcomes = []; receipts = []
    def stage(name, status, **details): stages.append(dict(Stage=name, Status=status, **details))
    try:
        catalog, registered, statuses, families, definitions, known_fixtures = load_catalog(ROOT, Path(a.catalog))
        selected, fixtures = select(registered, statuses, families, definitions, known_fixtures, a.mfa, a.family, a.queue, a.all)
        missing = sorted(set(selected) - set(definitions))
        extras = sorted({m for f in fixtures for m in f['Mfas']} - set(selected))
        request = []; bindings = []
        inputs = output / 'inputs'; inputs.mkdir(); libraries = output / 'lib'; libraries.mkdir()
        for row in catalog['Snapshot']['Libraries']:
            source = admit(ROOT, row); target = libraries / source.name; shutil.copyfile(source, target); bindings.append(dict(row, LocalPath=str(target.relative_to(output))))
        for f in fixtures:
            folder = inputs / f['Id']; folder.mkdir()
            source = admit(ROOT, f['Source']); target = folder / f['DeclaredSourceFile']
            if Path(f['DeclaredSourceFile']).name != f['DeclaredSourceFile']: raise ValueError('Declared source must be a basename.')
            shutil.copyfile(source, target)
            expected = None
            if f['ExpectedProtocol'] is not None:
                source_expected = admit(ROOT, f['ExpectedProtocol']); expected = folder / 'expected.etf'; shutil.copyfile(source_expected, expected)
            request.append(dict(Id=f['Id'],SourcePath=str(target),SourceSha256=f['Source']['Sha256'],DeclaredSourceFile=f['DeclaredSourceFile'],Module=f['Module'],EntryPoint=f['EntryPoint'],Arguments=f['Arguments'],TrapExits=f['TrapExits'],CompilerPolicy=f['CompilerPolicy'],ExpectedProtocolPath=str(expected) if expected else None,ExpectedProtocolSha256=f['ExpectedProtocol']['Sha256'] if expected else None,EffectKeys=f['EffectKeys'],DeadlineSeconds=f['DeadlineSeconds']))
        write(output / 'selection.json', dict(PinnedOtpCommit=PIN,SelectedMfas=selected,SelectedCount=len(selected),RegisteredCount=len(registered),CompleteContractCount=sum(x==VERIFIED for x in statuses.values()),MissingDescriptors=missing,FixtureIds=[f['Id'] for f in fixtures],SharedFixtureExtraMfas=extras,SnapshotId=catalog['Snapshot']['Id']))
        write(output / 'request.json', request)
        stage('prepare', PASS, Selected=len(selected), Fixtures=len(fixtures), MissingDescriptors=len(missing))
        if a.command == 'plan':
            for name in STAGES[1:]: stage(name,BLOCKED,Reason='PLAN_ONLY_NO_EXECUTION')
        else:
            owned = load(admit(ROOT,catalog['Helpers']['Owned']), 'contract_owned')
            decoder = load(admit(ROOT,catalog['Helpers']['Decoder']), 'contract_decoder')
            build = output / 'build'; build.mkdir()
            for source in (TOOL / 'Runner').iterdir():
                if source.is_file(): shutil.copyfile(source,build/source.name)
            shutil.copyfile(ROOT/'Directory.Build.props',output/'Directory.Build.props'); shutil.copyfile(ROOT/'global.json',output/'global.json')
            (output/'NuGet.Config').write_text('<configuration><packageSources><clear /></packageSources></configuration>\n')
            for p in [output/'Directory.Build.props',output/'global.json',output/'NuGet.Config',output/'request.json']:
                source_rows.append(dict(Path=str(p),Bytes=p.stat().st_size,Sha256=digest(p)))
            for root in [TOOL,inputs,libraries,build]:
                for p in sorted(root.rglob('*')):
                    if p.is_file(): source_rows.append(dict(Path=str(p),Bytes=p.stat().st_size,Sha256=digest(p)))
            write(output/'source-before.json',dict(Files=source_rows,SnapshotId=catalog['Snapshot']['Id'],DotnetPath=a.dotnet,DotnetExecutableSha256=digest(a.dotnet) if a.dotnet else None))
            def verify():
                for row in source_rows:
                    p=Path(row['Path'])
                    if p.stat().st_size!=row['Bytes'] or digest(p)!=row['Sha256']: raise ValueError('Source changed: '+str(p))
            env=dict(os.environ,DOTNET_PROCESSOR_COUNT='1',MSBUILDDISABLENODEREUSE='1',DOTNET_CLI_TELEMETRY_OPTOUT='1',DOTNET_SKIP_FIRST_TIME_EXPERIENCE='1',PYTHONDONTWRITEBYTECODE='1')
            for key in ['DOTNET_STARTUP_HOOKS','DOTNET_ADDITIONAL_DEPS','DOTNET_SHARED_STORE']:env.pop(key,None)
            processes=output/'processes';processes.mkdir()
            def execute(name,command,seconds):
                verify();error=None
                try: owned.owned(command,processes,name,seconds,env,output)
                except Exception as e:error=repr(e)
                path=processes/(name+'.exit.json');receipt=read(path) if path.exists() else None
                process_status='TIMEOUT' if receipt and receipt['TimedOut'] else 'CRASH' if receipt and receipt['ExitCode'] is not None and receipt['ExitCode']<0 else BLOCKER if error or not receipt or not receipt['Settled'] or receipt['Failures'] else PASS
                receipts.append(dict(Name=name,Receipt=receipt,Error=error,Outcome=process_status))
                return receipt is not None and receipt['ExitCode']==0 and receipt['Settled'] and not receipt['TimedOut'] and not receipt['Failures']
            available=bool(a.dotnet and fixtures)
            built=available and execute('build',[a.dotnet,'build',str(build/'MfaContract.Runner.csproj'),'--disable-build-servers','-m:1','-nr:false','--nologo','-p:UseSharedCompilation=false','-p:NuGetAudit=false','-p:ContractLibraryDirectory='+str(libraries)],180)
            stage('build',PASS if built else receipts[-1]['Outcome'] if available and receipts else BLOCKED,Reason=None if built else 'SDK_HARNESS_BUILD_UNAVAILABLE_OR_FAILED')
            runner=build/'bin/Debug/net10.0/MfaContract.Runner.dll'
            parsed=built and execute('parse',[a.dotnet,str(runner),'parse',str(output/'request.json'),str(output/'parse')],180)
            parse_rows=read(output/'parse/results.json')['Results'] if (output/'parse/results.json').exists() else []
            stage('parse',PASS if parsed else receipts[-1]['Outcome'] if built else BLOCKED,Results=parse_rows,Reason=None if parsed else 'BUILD_OR_PARSE_FAILED_OR_NO_FIXTURES')
            passed={x['Id'] for x in parse_rows if x['Status']==PASS}
            sdk_request=[f for f in request if f['Id'] in passed];write(output/'sdk-request.json',sdk_request)
            sdk_ok=bool(sdk_request) and execute('sdk',[a.dotnet,str(runner),'sdk',str(output/'sdk-request.json'),str(output/'sdk')],900)
            sdk_rows=read(output/'sdk/results.json')['Results'] if (output/'sdk/results.json').exists() else []
            for f in request:
                if f['Id'] not in passed:sdk_rows.append(dict(Id=f['Id'],Status=BLOCKED,Reason='FAILED_PARSE_DEPENDENCY'))
            stage('sdk',PASS if sdk_ok and len(sdk_request)==len(request) else receipts[-1]['Outcome'] if sdk_request else BLOCKED,Results=sdk_rows)
            raw_checks=[]
            for f in fixtures:
                path=output/'sdk'/f['Id']/'raw.etf'
                if not path.exists():continue
                try:
                    raw=decoder.decode_exact(path.read_bytes());assert raw[0]=='tuple' and len(raw[1])==2 and raw[1][1][0]=='list'
                    shape=f['RawShape'];count=1
                    if shape:
                        outer=raw[1][0];assert outer[0]=='tuple' and outer[1][0]==('atom','returned')
                        profiles=outer[1][1];assert profiles[0]=='list' and len(profiles[1])==len(shape['Depths']);count=0
                        for depth,row in zip(shape['Depths'],profiles[1]):
                            label=('atom',depth) if isinstance(depth,str) else ('integer',depth)
                            assert row[0]=='tuple' and row[1][0]==label
                            cells=row[1][1];assert cells[0]=='list' and len(cells[1])==len(shape['Cases'])
                            for case,cell in zip(shape['Cases'],cells[1]):
                                assert cell[0]=='tuple' and cell[1][0]==('atom',case['Name']);node=cell[1][1]
                                assert node[0]=='tuple';fields=node[1]
                                if fields[0]==('atom',shape['SuccessTag']):assert len(fields)==shape['SuccessArity']
                                else:assert fields[0]==('atom',shape['ErrorTag']) and len(fields)==shape['ErrorArity'] and fields[shape['StackIndex']][0]=='list'
                                assert fields[shape['EffectsIndex']][0]=='list';count+=1
                    raw_checks.append(dict(Id=f['Id'],Status=PASS,FullRawOutcomes=count,Sha256=digest(path)))
                except Exception as e:raw_checks.append(dict(Id=f['Id'],Status=BLOCKER,Error=repr(e)))
            write(output/'raw-checks.json',raw_checks)
            provider={};provider_problems=[]
            if a.original_context:
                try:
                    if not a.original_context_sha256 or digest(a.original_context)!=a.original_context_sha256.upper():raise ValueError('Original context SHA admission required.')
                    context=read(a.original_context)
                    if context['SchemaVersion']!=1 or context['PinnedOtpCommit']!=PIN or context['Mode']!='reviewed-existing-capture':raise ValueError('Unsupported original context; no implicit VM runner.')
                    for row in context['Provenance']:admit(ROOT,row)
                    provider={x['Id']:x for x in context['Fixtures']}
                    if len(provider)!=len(context['Fixtures']):raise ValueError('Duplicate original fixture.')
                except Exception as e:provider={};provider_problems.append(repr(e))
            originals=[];compared=[]
            for f in fixtures:
                try:
                    if f['Id'] not in provider:raise ValueError('NO_REVIEWED_HASH_BOUND_ORIGINAL_CONTEXT_FOR_FIXTURE')
                    p=provider[f['Id']]
                    for key in ['EntryPoint','Arguments','TrapExits','DeclaredSourceFile','EffectKeys']:
                        if p[key]!=f[key]:raise ValueError('Original input binding mismatch: '+key)
                    if p['SourceSha256']!=f['Source']['Sha256']:raise ValueError('Original source differs.')
                    raw_path=admit(ROOT,p['Raw']);proof=read(admit(ROOT,p['CaptureReceipt']))
                    if not(proof['ExitCode']==0 and proof['ParentReaped'] and proof['StreamsClosed'] and not proof['TerminationRequested'] and not proof['RemainingGroupKillRequested'] and proof['CleanupFailure'] is None):raise ValueError('Original capture did not clean up normally.')
                    originals.append(dict(Id=f['Id'],Status=PASS,RawSha256=digest(raw_path)))
                    sdk=output/'sdk'/f['Id']/'raw.etf'
                    if not sdk.exists():raise ValueError('SDK_RAW_DEPENDENCY_MISSING')
                    equal=decoder.decode_exact(raw_path.read_bytes())==decoder.decode_exact(sdk.read_bytes())
                    compared.append(dict(Id=f['Id'],Status=PASS if equal else DIFFERENCES,Policy='ENTIRE_RAW_TERM_WITH_ERRORS_STACKS_AND_DECLARED_EFFECTS'))
                except Exception as e:
                    if not any(x['Id']==f['Id'] for x in originals):originals.append(dict(Id=f['Id'],Status=BLOCKED,Reason=repr(e)))
                    compared.append(dict(Id=f['Id'],Status=BLOCKED,Reason=repr(e)))
            stage('original',PASS if originals and all(x['Status']==PASS for x in originals) else BLOCKED,Provider='reviewed-existing-capture' if a.original_context else None,Results=originals,Problems=provider_problems)
            stage('compare',PASS if compared and all(x['Status']==PASS for x in compared) else DIFFERENCES if any(x['Status']==DIFFERENCES for x in compared) else BLOCKED,Results=compared)
            verify();write(output/'source-after.json',dict(Files=source_rows))
            sdk_by={x['Id']:x['Status'] for x in sdk_rows}; global_sdk_failure=next((x['Outcome'] for x in receipts if x['Name']=='sdk' and x['Outcome']!=PASS),None); compare_by={x['Id']:x['Status'] for x in compared}
            for mfa in selected:
                relevant=[f for f in fixtures if mfa in f['Mfas']];definition=definitions.get(mfa)
                coverage=bool(definition and definition['CoverageCompleteReviewed'] and definition['RequiredPartitions'] and set(definition['RequiredPartitions'])<={f['Id'] for f in relevant})
                codes=[sdk_by.get(f['Id'],global_sdk_failure or BLOCKED) for f in relevant]
                status='TIMEOUT' if 'TIMEOUT' in codes else 'CRASH' if 'CRASH' in codes else BLOCKER if BLOCKER in codes else DIFFERENCES if any(x=='LOCAL_EXPECTATION_DIFFERENCE' for x in codes) or any(compare_by.get(f['Id'])==DIFFERENCES for f in relevant) else INSUFFICIENT if not coverage else ACCEPTED if relevant and all(compare_by.get(f['Id'])==PASS for f in relevant) else BLOCKED
                outcomes.append(dict(Mfa=mfa,Outcome=status,CurrentLedgerStatus=statuses.get(mfa,'Unregistered candidate'),CoverageCompleteReviewed=coverage,OriginalComparisonComplete=bool(relevant) and all(compare_by.get(f['Id'])==PASS for f in relevant),Fixtures=[f['Id'] for f in relevant]))
        if a.command=='plan':
            outcomes=[dict(Mfa=m,Outcome=INSUFFICIENT if m not in definitions or not definitions[m]['CoverageCompleteReviewed'] else BLOCKED,CurrentLedgerStatus=statuses.get(m,'Unregistered candidate')) for m in selected]
    except Exception as error:
        if not any(x['Stage']=='prepare' for x in stages):stage('prepare',BLOCKER,Error=repr(error))
        else:stage('orchestration',BLOCKER,Error=repr(error))
    for name in STAGES:
        if not any(x['Stage']==name for x in stages):stage(name,BLOCKED,Reason='PREPARATION_OR_STAGE_DEPENDENCY_FAILED')
    changes=[dict(Mfa=x['Mfa'],Before=x['CurrentLedgerStatus'],Proposed=VERIFIED,EvidenceOutcome=x['Outcome']) for x in outcomes if x['Outcome']==ACCEPTED and x['CurrentLedgerStatus']!=VERIFIED]
    write(output/'proposed-ledger-diff.json',dict(ProposedOnly=True,Applied=False,Changes=changes,ExistingVerifiedStatusesPreserved=True))
    counts={key:sum(x['Outcome']==key for x in outcomes) for key in [ACCEPTED,DIFFERENCES,INSUFFICIENT,BLOCKED,BLOCKER,'CRASH','TIMEOUT']}
    summary=dict(Status='PREPARED_PLAN_ONLY' if a.command=='plan' and stages[0]['Status']==PASS else 'LOCAL_RUN_RECORDED' if stages[0]['Status']==PASS else BLOCKER,SelectedMfaCount=len(selected),OutcomeCounts=counts,Stages=stages,Mfas=outcomes,ProcessReceipts=receipts,LedgerApplied=False,Published=False,NetworkExecuted=False,OriginalVmExecuted=False,SnapshotSourceRows=len(source_rows))
    write(output/'outcomes.json',summary)
    print(json.dumps(dict(Status=summary['Status'],SelectedMfas=len(selected),Outcomes=counts,Stages=[dict(Stage=x['Stage'],Status=x['Status']) for x in stages])))
    return 1 if any(x['Status'] in [BLOCKER,'CRASH','TIMEOUT'] for x in stages) else 0

if __name__=='__main__':raise SystemExit(main())
