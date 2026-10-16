"""Source-only adapter; never builds or executes OTP. Output needs independent approval."""
import argparse,hashlib,json,os,pathlib,stat,subprocess,sys,platform
PIN='ad05823719d77c8faee87348ea39513d4e2f99c5'
MAX_FILES=50000
MAX_TOTAL_BYTES=4*1024**3
MAX_FILE_BYTES=256*1024**2
MAX_EVIDENCE_BYTES=256*1024**2

def require(value,message):
    if not value:raise ValueError(message)
def pairs(items):
    result={}
    for key,value in items:
        require(key not in result,'Duplicate JSON key');result[key]=value
    return result
def read(path):
    require(path.stat().st_size<=MAX_EVIDENCE_BYTES,'Evidence size budget')
    return json.loads(path.read_bytes(),object_pairs_hook=pairs)
def digest(path,maximum=None):
    if maximum is None:maximum=MAX_FILE_BYTES
    before=path.lstat();require(stat.S_ISREG(before.st_mode) and before.st_size<=maximum,'Regular bounded file required')
    h=hashlib.sha256();total=0
    with path.open('rb') as stream:
        for chunk in iter(lambda:stream.read(1024*1024),b''):
            total+=len(chunk);require(total<=maximum,'File size budget');h.update(chunk)
    after=path.lstat();require(stat.S_ISREG(after.st_mode),'File type changed while hashing');require((before.st_dev,before.st_ino,before.st_size,before.st_mtime_ns)==(after.st_dev,after.st_ino,after.st_size,after.st_mtime_ns) and total==before.st_size,'File changed while hashing')
    return {'Path':str(path),'Bytes':total,'Sha256':h.hexdigest().upper()}
def exact_keys(obj,keys):require(type(obj) is dict and set(obj)==set(keys),'Unexpected fields')
def relative_path(value):
    require(type(value) is str and value and '\\' not in value,'POSIX relative path required');p=pathlib.PurePosixPath(value)
    require(not p.is_absolute() and '..' not in p.parts and '.' not in p.parts and str(p)==value,'Escaping/noncanonical relative path');return p

def inventory(source,prefix):
    source=source.resolve(strict=True);require(prefix.is_absolute() and not prefix.is_symlink(),'Absolute real install prefix required');prefix=prefix.resolve(strict=True)
    require(prefix.is_relative_to(source) and prefix!=source and prefix.is_dir(),'Prefix must be a proper child of source')
    rows=[];files={};total=0
    def visit(directory):
        nonlocal total
        require(directory.resolve(strict=True).is_relative_to(prefix) and not directory.is_symlink(),'Visited directory escapes prefix or is a directory symlink')
        for path in sorted(directory.iterdir()):
            require(len(rows)<MAX_FILES,'Entry count budget');info=path.lstat();relative=path.relative_to(prefix).as_posix()
            if stat.S_ISLNK(info.st_mode):
                target=path.resolve(strict=True);require(target.is_relative_to(prefix),'Escaping installed symlink');require(target.is_file() and not target.is_dir(),'Directory/special symlink refused')
                link=os.readlink(path);require(os.readlink(path)==link,'Link changed');rows.append({'Path':relative,'Kind':'FileSymlink','LinkText':link,'Target':target.relative_to(prefix).as_posix()})
            elif stat.S_ISDIR(info.st_mode):
                rows.append({'Path':relative,'Kind':'Directory','Mode':stat.S_IMODE(info.st_mode)});visit(path);continue
            else:
                require(stat.S_ISREG(info.st_mode),'Special installed file refused');target=path.resolve(strict=True);rows.append({'Path':relative,'Kind':'File','Mode':stat.S_IMODE(info.st_mode)})
            require(target.is_relative_to(prefix),'Every resolved runtime target must belong to prefix')
            key=str(target)
            if key not in files:
                item=digest(target);require(target.resolve(strict=True).is_relative_to(prefix),'Target escaped while hashing');total+=item['Bytes'];require(total<=MAX_TOTAL_BYTES,'Total byte budget');files[key]=item
    visit(prefix)
    require(files,'Empty installed closure')
    runtime_dirs=sorted((prefix/'lib/erlang').glob('erts-*'));require(len(runtime_dirs)==1 and runtime_dirs[0].is_dir() and not runtime_dirs[0].is_symlink(),'Exactly one real erts directory required')
    required=[prefix/'bin/erl',prefix/'bin/erlc',runtime_dirs[0]/'bin/beam.smp',runtime_dirs[0]/'bin/erlexec']
    roles={}
    for path in required:
        resolved=path.resolve(strict=True);require(str(resolved) in files,'Required executable missing from closure');require(path.stat().st_mode & 0o111,'Required executable mode missing');roles[path.name]=str(resolved)
    require(len(set(roles.values()))==4,'Required executable roles alias each other')
    require(any(p.endswith('.beam') for p in files),'Installed BEAM libraries missing')
    return {'Definition':'AllEntriesUnderEntireConfiguredInstallPrefix-v1','InstalledPrefix':str(prefix),'Entries':rows,'RuntimeFiles':[files[k] for k in sorted(files)],'RequiredExecutables':roles,'RegularFileCount':len(files),'TotalRegularFileBytes':total,'ExternalSystemLibrariesIncluded':False,'DirectorySymlinksAllowed':False}

def git_state(source,runner=subprocess.run):
    results=[]
    for arguments in [['rev-parse','--show-toplevel'],['rev-parse','HEAD'],['status','--porcelain=v1','--untracked-files=no','--ignore-submodules=none']]:
        command=['git','--no-optional-locks','-C',str(source),*arguments]
        completed=runner(command,stdout=subprocess.PIPE,stderr=subprocess.PIPE,timeout=30,check=False)
        require(completed.returncode==0 and not completed.stderr,'Read-only Git inspection failed');results.append(completed.stdout.decode('utf-8').strip())
    require(pathlib.Path(results[0]).resolve()==source.resolve(),'Source root must be Git top level');require(results[1]==PIN,'Wrong source pin');require(results[2]=='','Tracked source/index/submodule modifications')
    return {'PinnedOtpCommit':PIN,'TrackedClean':True,'UntrackedBuildOutputsIntentionallyExcluded':True,'ReadOnlyCommands':[['rev-parse','--show-toplevel'],['rev-parse','HEAD'],['status','--porcelain=v1','--untracked-files=no','--ignore-submodules=none']]}

def validate_provenance(provenance,source,prefix,closure):
    exact_keys(provenance,['SourceHead','ConfiguredPrefix','NativeNifCasesExecuted','Files']);require(provenance['SourceHead']==PIN and provenance['NativeNifCasesExecuted'] is False,'Old provenance pin/scope mismatch')
    require(pathlib.Path(provenance['ConfiguredPrefix']).resolve()==prefix.resolve(),'Configured prefix mismatch');require(type(provenance['Files']) is list and len(provenance['Files'])==4,'Legacy four-row provenance required')
    seen=set();roles={};actual={e['Path']:e for e in closure['RuntimeFiles']}
    for item in provenance['Files']:
        exact_keys(item,['Path','Bytes','Sha256']);p=relative_path(item['Path']);require(item['Path'] not in seen,'Duplicate old provenance path');seen.add(item['Path']);path=(source/pathlib.Path(p)).resolve(strict=True)
        require(path.is_relative_to(prefix) and str(path) in actual,'Old provenance path outside closure');require(type(item['Bytes']) is int and item['Bytes']>=0,'Invalid old byte count');require(item['Bytes']==actual[str(path)]['Bytes'] and item['Sha256']==actual[str(path)]['Sha256'],'Old provenance bytes differ');require(path.name not in roles,'Duplicate old executable role');roles[path.name]=str(path)
    require(roles==closure['RequiredExecutables'],'Old four roles differ from actual installed roles')

def make_receipt(source,prefix,provenance_path,log_path,review_path,approved_review,runner=subprocess.run):
    source=source.resolve(strict=True);prefix=prefix.absolute();evidence=[digest(p,MAX_EVIDENCE_BYTES) for p in [provenance_path,log_path,review_path]]
    require(evidence[2]['Sha256']==approved_review.upper(),'Independently approved build-review digest required');review=read(review_path)
    exact_keys(review,['Schema','SourceBuildAccepted','PinnedOtpCommit','SourceRoot','InstalledPrefix','ProvenanceSha256','BuildLogSha256','ReviewedBuildCommands','ReviewNote'])
    require(review['Schema']=='PinnedSourceBuildIndependentReview-v1' and review['SourceBuildAccepted'] is True and review['PinnedOtpCommit']==PIN,'Independent source-build review not accepted')
    require(pathlib.Path(review['SourceRoot']).resolve()==source and pathlib.Path(review['InstalledPrefix']).resolve()==prefix.resolve(),'Review source/install identity mismatch');require(review['ProvenanceSha256']==evidence[0]['Sha256'] and review['BuildLogSha256']==evidence[1]['Sha256'],'Review evidence hashes mismatch')
    require(type(review['ReviewedBuildCommands']) is list and len(review['ReviewedBuildCommands'])==3 and all(type(s) is str and s for s in review['ReviewedBuildCommands']),'Explicit configure/build/install review required');require(type(review['ReviewNote']) is str and review['ReviewNote'],'Independent review note required')
    before=git_state(source,runner);closure=inventory(source,prefix);provenance=read(provenance_path);validate_provenance(provenance,source,prefix,closure)
    require(inventory(source,prefix)==closure,'Installed tree changed across complete snapshots');require(git_state(source,runner)==before,'Source changed during inspection')
    require([digest(p,MAX_EVIDENCE_BYTES) for p in [provenance_path,log_path,review_path]]==evidence,'Evidence changed during inspection')
    return {'Schema':'PinnedInstalledRuntimeAdapter-v1','Status':'CANDIDATE_REQUIRES_INDEPENDENT_FINAL_RECEIPT_HASH_APPROVAL','PinnedOtpCommit':PIN,'BuiltFromPinnedSource':True,'RuntimeFiles':closure['RuntimeFiles'],'InstalledClosure':closure,'SourceGitInspection':before,'OriginalBuildProvenance':provenance,'SourceEvidence':dict(zip(['OriginalProvenance','CompleteBuildLog','IndependentSourceBuildReview'],evidence)),'VersionCommandExecuted':False,'VmExecuted':False,'BuiltOrInstalledByAdapter':False}

def validate_output(source,output):
    resolved=output.resolve();require(not resolved.is_relative_to(source.resolve()),'Output must be outside immutable source/install tree');require(not output.exists(),'Fresh output required')
    return output

def validate_platform(platform_id,machine):
    require(platform_id=='linux' and machine=='x86_64','Adapter execution is Linux x64 installed-runtime profile only')

def main():
    parser=argparse.ArgumentParser()
    for name in ['source','installed-prefix','provenance','build-log','independent-review','output']:parser.add_argument('--'+name,type=pathlib.Path,required=True)
    parser.add_argument('--approved-review-sha256',required=True);args=parser.parse_args();validate_platform(sys.platform,platform.machine())
    validate_output(args.source,args.output)
    receipt=make_receipt(args.source,args.installed_prefix,args.provenance,args.build_log,args.independent_review,args.approved_review_sha256)
    # Exclusive directory creation only after all read-only checks; never overwrite/retry.
    args.output.mkdir(parents=False,exist_ok=False)
    for source,name in [(args.provenance,'original-build-provenance.json'),(args.build_log,'complete-build.log'),(args.independent_review,'independent-source-build-review.json')]:
        data=source.read_bytes();target=args.output/name
        with target.open('xb') as stream:stream.write(data)
    copied=[digest(args.output/n,MAX_EVIDENCE_BYTES) for n in ['original-build-provenance.json','complete-build.log','independent-source-build-review.json']]
    require([x['Sha256'] for x in copied]==[x['Sha256'] for x in receipt['SourceEvidence'].values()],'Evidence copy changed')
    receipt['PreservedEvidenceCopies']=[{'Path':pathlib.Path(x['Path']).name,'Bytes':x['Bytes'],'Sha256':x['Sha256']} for x in copied]
    with (args.output/'source-build-receipt.json').open('x',encoding='utf-8') as stream:json.dump(receipt,stream,indent=2);stream.write('\n')
if __name__=='__main__':main()
