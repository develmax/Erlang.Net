"""Root-source-reviewed passive approval authority, separate from producers."""
import argparse,base64,importlib.util,json,os,pathlib,shlex,stat,subprocess,sys
import runtime_receipt_adapter as A
from reviewed_common import verified_plan,exact_head,verify_otp,sha,write_fresh
from controlled_source_build import ROLES,commands,LOG_LIMIT,TOTAL_LOG_LIMIT

def require(value,message):
    if not value:raise ValueError(message)
def read(path):return A.read(path)
def binding(path):return A.digest(path,A.MAX_EVIDENCE_BYTES)
def strict_files(root,receipt_name,receipt,extra=()):
    names=set()
    for item in receipt['Files']:
        name=item['Path'];A.relative_path(name);require(name not in names,'Duplicate file member');names.add(name);p=(root/name).resolve();require(p.is_relative_to(root.resolve()),'Escaping receipt file');actual=binding(p);require(actual['Bytes']==item['Bytes'] and actual['Sha256']==item['Sha256'],'Changed captured file')
    require({p.relative_to(root).as_posix() for p in root.rglob('*') if p.is_file()}==names|{receipt_name}|set(extra),'Raw capture closure mismatch')
def valid_process(process,attempt,role,argv,cwd,deadline):
    require(process['Role']==attempt['Role']==role and process['Arguments']==attempt['Arguments']==argv and process['Cwd']==attempt['Cwd']==str(cwd),'Unexpected build recipe')
    require(type(process['ExitCode']) is int and process['ExitCode']==0 and process['TimedOut'] is False and process['LogBudgetExceeded'] is False and process['ParentReaped'] is True and process['OutputsClosed'] is True,'Unsuccessful source build process')
    require(type(process['OwnedPid']) is int and process['OwnedPid']>0 and process['OwnedProcessGroup']==process['OwnedPid'],'Owned process group required');require(process['DeadlineSeconds']==attempt['DeadlineSeconds']==deadline and 0<=process['ElapsedSeconds']<=deadline+20,'Process deadline mismatch');require(attempt['AutomaticRetryAllowed'] is False and process['CompletedUnixSeconds']>=attempt['StartedUnixSeconds'],'Attempt/order mismatch')
def context(args):
    A.validate_platform(sys.platform,os.uname().machine);root=args.repository.resolve();source=args.otp_source.resolve();plan=verified_plan(root,args.plan,args.approved_plan_sha256);head=exact_head(root,args.expected_head);workflow=root/'.github/workflows/nif-reallocation-native.yml';blob=subprocess.check_output(['git','--no-optional-locks','-C',str(root),'show',head+':.github/workflows/nif-reallocation-native.yml']);require(blob==workflow.read_bytes(),'Workflow differs from published commit');A.git_state(source)
    return root,source,plan,head,workflow

def canonical(value):return json.dumps(value,sort_keys=True,separators=(',',':')).encode()
def source_note(closure,build_receipt):
    return canonical({'Schema':'VerifiedFullPinnedInstalledBuild-v1','InstalledClosureSha256':sha(canonical(closure)),'BuildProcessReceiptSha256':sha(build_receipt.read_bytes())}).decode()

def exact_tools(actual,expected):
    require(len(expected)==2 and actual==expected,'Exact configure/make identity set required')

def verify_build(args):
    root,source,plan,head,workflow=context(args);output=args.source_build.resolve();receipt=read(output/'build-process-receipt.json');require(receipt['Complete'] is True and receipt['ApprovalIssued'] is False and receipt['ObservationVmExplicitlyInvoked'] is False,'Build producer cannot approve');require(receipt['Head']==head and receipt['WorkflowSha256']==sha(workflow.read_bytes()) and receipt['PlanSha256']==sha(args.plan.read_bytes()) and receipt['PinnedOtpCommit']==A.PIN,'Source build authority binding mismatch');require(receipt['WorkflowRun']=={k:os.environ.get(k,'') for k in ('GITHUB_SHA','GITHUB_RUN_ID','GITHUB_RUN_ATTEMPT','GITHUB_JOB')} and receipt['WorkflowRun']['GITHUB_SHA']==head and all(receipt['WorkflowRun'].values()),'Workflow run context mismatch');require(receipt['SourceRoot']==str(source) and receipt['InstalledPrefix']==str(source/'observer-install') and receipt['Roles']==list(ROLES),'Source/prefix/roles changed');require(receipt['SourceBefore']==receipt['SourceAfter']==A.git_state(source),'Tracked source state changed');strict_files(output,'build-process-receipt.json',receipt)
    log=b'';previous=0;recipes=commands(source)
    for role,argv,deadline in recipes:
        attempt=read(output/(role+'.attempt.json'));process=read(output/(role+'.process.json'));valid_process(process,attempt,role,argv,source,deadline);require(attempt['StartedUnixSeconds']>=previous,'Build processes reordered');previous=process['CompletedUnixSeconds'];require(process['AttemptSha256']==sha((output/(role+'.attempt.json')).read_bytes()),'Attempt linkage changed')
        for suffix,key in [('stdout','Stdout'),('stderr','Stderr')]:
            raw=(output/(role+'.'+suffix)).read_bytes();require(len(raw)<=LOG_LIMIT and process[key]['Sha256']==sha(raw) and process[key]['Bytes']==len(raw),'Process log linkage failed');log+=('=== '+role+'.'+suffix+' ===\n').encode()+raw+b'\n'
    require(log==(output/'complete-build.log').read_bytes() and len(log)<=TOTAL_LOG_LIMIT+4096,'Complete source build log mismatch')
    prefix=source/'observer-install';include=prefix/'lib/erlang/usr/include';verify_otp(source,include,plan);closure=A.inventory(source,prefix);require(canonical(closure)==canonical(receipt['InstalledClosure']),'Whole installed prefix changed after build');provenance=read(output/'legacy-provenance.json');A.validate_provenance(provenance,source,prefix,closure)
    expected_tools=[{'Role':role,'CommandPath':str(argv[0]),'CanonicalFile':binding(pathlib.Path(argv[0]).resolve())} for role,argv in [('configure',recipes[0][1]),('make',recipes[1][1])]]
    exact_tools(receipt['ProgramIdentities'],expected_tools);exact_tools(receipt['ProgramIdentitiesBefore'],expected_tools)
    return {'Schema':'PinnedSourceBuildIndependentReview-v1','SourceBuildAccepted':True,'PinnedOtpCommit':A.PIN,'SourceRoot':str(source),'InstalledPrefix':str(prefix),'ProvenanceSha256':sha((output/'legacy-provenance.json').read_bytes()),'BuildLogSha256':sha(log),'ReviewedBuildCommands':[shlex.join(argv) for _,argv,_ in recipes],'ReviewNote':source_note(closure,output/'build-process-receipt.json')}

def independent_runtime(receipt,source):
    require(receipt['PinnedOtpCommit']==A.PIN and receipt['BuiltFromPinnedSource'] is True and receipt['VmExecuted'] is False and receipt['BuiltOrInstalledByAdapter'] is False,'Adapter scope mismatch');closure=receipt['InstalledClosure'];prefix=pathlib.Path(closure['InstalledPrefix']).resolve();require(prefix==source/'observer-install','Adapter configured prefix mismatch');expected={e['Path']:e for e in receipt['RuntimeFiles']};require(len(expected)==len(receipt['RuntimeFiles']) and receipt['RuntimeFiles']==closure['RuntimeFiles'],'Adapter duplicate or divergent runtime list');entries=[];actual={};total=0;stack=[prefix]
    while stack:
        directory=stack.pop();require(directory.resolve().is_relative_to(prefix) and not directory.is_symlink(),'Visited directory escaped')
        for path in directory.iterdir():
            require(len(entries)<A.MAX_FILES,'Installed entry budget');mode=path.lstat().st_mode;name=path.relative_to(prefix).as_posix();target=path.resolve(strict=True);require(target.is_relative_to(prefix),'Installed path escaped')
            if stat.S_ISDIR(mode):entries.append({'Path':name,'Kind':'Directory','Mode':stat.S_IMODE(mode)});stack.append(path);continue
            if stat.S_ISLNK(mode):require(target.is_file(),'Directory/dangling symlink refused');entries.append({'Path':name,'Kind':'FileSymlink','LinkText':os.readlink(path),'Target':target.relative_to(prefix).as_posix()})
            else:require(stat.S_ISREG(mode),'Special runtime file refused');entries.append({'Path':name,'Kind':'File','Mode':stat.S_IMODE(mode)})
            if str(target) not in actual:actual[str(target)]=A.digest(target);total+=actual[str(target)]['Bytes'];require(total<=A.MAX_TOTAL_BYTES,'Runtime byte budget')
    # Adapter traversal is depth-first sorted; compare namespace records by path.
    require(canonical(sorted(entries,key=lambda e:e['Path']))==canonical(sorted(closure['Entries'],key=lambda e:e['Path'])),'Actual runtime namespace closure changed');require(canonical(actual)==canonical(expected) and type(closure['TotalRegularFileBytes']) is int and total==closure['TotalRegularFileBytes'] and len(actual)==closure['RegularFileCount'],'Complete installed files differ');require(closure['ExternalSystemLibrariesIncluded'] is False and closure['DirectorySymlinksAllowed'] is False,'Closure scope broadened')
    directories=list((prefix/'lib/erlang').glob('erts-*'));require(len(directories)==1,'Exactly one erts directory');roles={p.name:str(p.resolve()) for p in [prefix/'bin/erl',prefix/'bin/erlc',directories[0]/'bin/beam.smp',directories[0]/'bin/erlexec']};require(len(set(roles.values()))==4 and roles==closure['RequiredExecutables'] and set(roles.values())<=set(actual),'Actual runtime roles missing');return prefix

def runtime_approval(args):
    reviewed=verify_build(args);review=read(args.review);require(review==reviewed,'Independent source-build review changed');receipt=read(args.runtime_receipt);source=args.otp_source.resolve();independent_runtime(receipt,source);output=args.source_build.resolve()
    for key,path in [('OriginalProvenance',output/'legacy-provenance.json'),('CompleteBuildLog',output/'complete-build.log'),('IndependentSourceBuildReview',args.review)]:require(receipt['SourceEvidence'][key]==binding(path),'Adapter evidence role mismatch')
    require(receipt['OriginalBuildProvenance']==read(output/'legacy-provenance.json'),'Original provenance was relabeled');copies={x['Path']:x for x in receipt['PreservedEvidenceCopies']};require(set(copies)=={'original-build-provenance.json','complete-build.log','independent-source-build-review.json'},'Adapter raw evidence copies incomplete')
    for name,item in copies.items():p=args.runtime_receipt.parent/name;require(p.stat().st_size==item['Bytes'] and sha(p.read_bytes())==item['Sha256'],'Raw preserved evidence changed')
    require({p.name for p in args.runtime_receipt.parent.iterdir()}==set(copies)|{'source-build-receipt.json'},'Unexpected adapter capture files')
    return {'Passed':True,'Authority':'PublishedRootSourceReviewedPassiveVerifier','Head':args.expected_head,'PlanSha256':sha(args.plan.read_bytes()),'VerifierSha256':sha(pathlib.Path(__file__).read_bytes()),'ApprovedReviewSha256':sha(args.review.read_bytes()),'ApprovedSourceBuildReceiptSha256':sha(args.runtime_receipt.read_bytes()),'RuntimeFiles':len(receipt['RuntimeFiles']),'VmExecutedByVerifier':False}

def captures(args):
    approval=runtime_approval(args);require(read(args.runtime_approval)==approval,'Runtime approval is stale');root=args.repository.resolve();sys.path.insert(0,str(pathlib.Path(__file__).resolve().parent));spec=importlib.util.spec_from_file_location('passive_captures',pathlib.Path(__file__).with_name('review-results.py'));G=importlib.util.module_from_spec(spec);spec.loader.exec_module(G)
    # Native gate is independently replayed even after SDK capture; build receipt
    # remains immutable while the final SDK receipt adds owned capture files.
    import verify_native_build as N
    native=N.verify(args.repository.resolve(),args.otp_source.resolve(),args.plan,args.approved_plan_sha256,args.expected_head,args.sdk.resolve(),after_sdk=True);require(native==read(args.native_approval),'Native binary approval is stale')
    original=args.original.resolve();sdk=args.sdk.resolve();one=read(original/'receipt.json');two=read(sdk/'sdk-receipt.json');require(one['Complete'] is True and two['Complete'] is True and one['Head']==two['Head']==args.expected_head and one['PinnedOtpCommit']==A.PIN,'Actual result receipt scope mismatch');require(one['PlanSha256']==two['PlanSha256']==sha(args.plan.read_bytes()),'Result source plan differs');require(one['IndependentlyApprovedSourceBuildReceiptSha256']==approval['ApprovedSourceBuildReceiptSha256'] and sha((original/'approved-source-build-receipt.json').read_bytes())==approval['ApprovedSourceBuildReceiptSha256'],'Original actual runtime provenance differs');G.exact_files(original,'receipt.json',one);G.exact_files(sdk,'sdk-receipt.json',two)
    require(one['Cases']==['null_positive','null_zero','resize_sequence'] and two['OriginalCompared'] is False,'Case inventory or comparison timing mismatch')
    for prefix in ['metadata',*one['Cases']]:
        process=read(original/(prefix+'.process.json'));require(type(process['ExitCode']) is int and process['ExitCode']==0 and process['TimedOut'] is False and process['ParentReaped'] is True and process['PipesClosed'] is True and not (original/(prefix+'.stderr')).read_bytes(),'Actual original VM capture failed')
    process=read(sdk/'sdk.process.json');require(type(process['ExitCode']) is int and process['ExitCode']==0 and process['TimedOut'] is False and process['ParentReaped'] is True and process['PipesClosed'] is True and not (sdk/'sdk.stderr').read_bytes(),'Actual SDK capture failed')
    return {'Passed':True,'Authority':'PublishedRootSourceReviewedPassiveVerifier','Head':args.expected_head,'PlanSha256':sha(args.plan.read_bytes()),'ApprovedOriginalReceiptSha256':sha((original/'receipt.json').read_bytes()),'ApprovedSdkReceiptSha256':sha((sdk/'sdk-receipt.json').read_bytes()),'SemanticComparisonStillRequired':True,'NativeExecutedByVerifier':False,'OriginalExecutedByVerifier':False}

def main():
    p=argparse.ArgumentParser();p.add_argument('phase',choices=['source-build','runtime-receipt','captured-receipts'])
    for name in ('repository','otp-source','plan','source-build','output'):p.add_argument('--'+name,type=pathlib.Path,required=True)
    for name in ('review','runtime-receipt','runtime-approval','native-approval','original','sdk'):p.add_argument('--'+name,type=pathlib.Path)
    for name in ('expected-head','approved-plan-sha256'):p.add_argument('--'+name,required=True)
    args=p.parse_args();from verify_native_build import approval_output
    approval_output(args.output,args.repository,[args.otp_source,args.source_build,args.runtime_receipt.parent if args.runtime_receipt else None,args.original,args.sdk]);result=verify_build(args) if args.phase=='source-build' else runtime_approval(args) if args.phase=='runtime-receipt' else captures(args);write_fresh(args.output,result)
if __name__=='__main__':main()
