"""Original-only real two-node proc_lib observations, fresh owned foreground EPMD per case."""
import argparse, ast, hashlib, importlib.util, json, os, re, secrets, signal, socket, subprocess, sys, time, traceback
from pathlib import Path, PurePosixPath
sys.dont_write_bytecode = True
PIN = 'ad05823719d77c8faee87348ea39513d4e2f99c5'
TOOL = 'tools/Distribution/Erlang.ProcLibRealPeerObservations'
CASES = ('mfa_spawn','mfa_spawn_link','mfa_spawn_opt','fun_spawn','fun_spawn_link','fun_spawn_opt','labels_visible','labels_hidden')
STOP = False

def require(value, why):
    if not value: raise ValueError(why)
def sha(p): return hashlib.sha256(p.read_bytes()).hexdigest().upper()
def save(p,v):
    with p.open('xb') as f: f.write((json.dumps(v,indent=2)+'\n').encode())
def checked(root,name):
    require(isinstance(name,str) and not PurePosixPath(name).is_absolute() and '..' not in PurePosixPath(name).parts and ':' not in name and '\\' not in name,'Source path')
    p=(root/name).resolve(strict=True);require(p.is_relative_to(root) and p.is_file(),'Source escape');return p
def binding(p,root): return dict(Path=p.relative_to(root).as_posix(),Bytes=p.stat().st_size,Sha256=sha(p))
def verify(a):
    root=a.repository.resolve(strict=True);tool=root/TOOL;mp=tool/'source-manifest.json'
    require(re.fullmatch('[A-Fa-f0-9]{64}',a.manifest_sha256) and sha(mp)==a.manifest_sha256.upper(),'Explicit reviewed manifest')
    m=json.loads(mp.read_bytes());require(m['PinnedOtpCommit']==PIN and m['Cases']==list(CASES),'Manifest scope')
    require({p.relative_to(tool).as_posix() for p in tool.rglob('*') if p.is_file() and p!=mp}=={r['Path'] for r in m['Files']},'Exact source closure')
    for r in m['Files']:require(binding(checked(tool,r['Path']),tool)==r,'Source drift')
    for r in m['ExternalDependencies']+[m['Workflow']]:require(binding(checked(root,r['Path']),root)==r,'Dependency/workflow drift')
    require(re.fullmatch('[0-9a-f]{40}',a.expected_head) and subprocess.check_output(['git','-C',str(root),'rev-parse','HEAD'],timeout=30).decode().strip()==a.expected_head,'Exact collector checkout')
    return root,tool,m

def load(root,m,suffix):
    rows=[r for r in m['ExternalDependencies'] if r['Path'].endswith('/'+suffix+'.py')];require(len(rows)==1,'One exact dependency')
    p=checked(root,rows[0]['Path']);require(binding(p,root)==rows[0],'Import drift')
    spec=importlib.util.spec_from_file_location('real_peer_'+suffix.replace('-','_'),p);v=importlib.util.module_from_spec(spec);spec.loader.exec_module(v);return v

def decode(decoder,p):
    data=p.read_bytes();require(1<len(data)<=16*1024*1024,'ETF size');v,n=decoder.decode_one(data);require(n==len(data),'ETF trailing');return v

def text(value):
    require(value[0]=='list' and all(x[0]=='integer' for x in value[1]),'Charlist');return ''.join(chr(x[1]) for x in value[1])
def map_fields(value): require(value[0]=='map','Map');return dict(value[1])

IDENTITY_MODULES = {'proc_lib_real_peer_probe','proc_lib_remote_payload','proc_lib','peer'}

def verify_module_identities(identity,installed,beam):
    modules=identity[('atom','modules')]
    require(modules[0]=='list' and len(modules)==2 and len(modules[1])==len(IDENTITY_MODULES),'Exact module identity list')
    seen=set()
    for entry in modules[1]:
        require(entry[0]=='tuple' and len(entry)==2 and len(entry[1])==6,'Module identity tuple')
        module,loaded_path,object_path,loaded_md5,code_md5,file_md5=entry[1]
        require(module[0]=='atom' and module[1] in IDENTITY_MODULES and module[1] not in seen,'Exact unique identity module')
        seen.add(module[1])
        loaded_path=Path(text(loaded_path)).resolve(strict=True);object_path=Path(text(object_path)).resolve(strict=True)
        require(loaded_path==object_path,'Loaded path/object path mismatch')
        expected_root=beam if module[1] in {'proc_lib_real_peer_probe','proc_lib_remote_payload'} else installed
        require(loaded_path.is_relative_to(expected_root) and loaded_path.is_file(),'Module outside its reviewed root')
        for digest in (loaded_md5,code_md5,file_md5):
            require(isinstance(digest,tuple) and len(digest)==2 and digest[0]=='binary' and isinstance(digest[1],bytes) and len(digest[1])==16,'MD5 binary16')
        require(loaded_md5==code_md5,'Actual loaded code differs from retrieved BEAM code checksum')
        require(file_md5==('binary',hashlib.md5(loaded_path.read_bytes()).digest()),'Retrieved full BEAM differs from file')
    require(seen==IDENTITY_MODULES,'Missing identity module')

def proc_token(pid):
    try:
        raw=Path('/proc',str(pid),'stat').read_text();parts=raw[raw.rfind(')')+2:].split();return (int(parts[1]),int(parts[19]))
    except (FileNotFoundError, ProcessLookupError):return None

def epmd_owner(process,port):
    pid=process.process.pid;require(process.poll() is None,'Owned EPMD exited')
    inodes=set()
    for fd in Path('/proc',str(pid),'fd').iterdir():
        try:target=os.readlink(fd)
        except (FileNotFoundError, ProcessLookupError):continue
        match=re.fullmatch(r'socket:\[(\d+)\]',target)
        if match:inodes.add(match.group(1))
    found=[]
    for line in Path('/proc',str(pid),'net/tcp').read_text().splitlines()[1:]:
        fields=line.split()
        if fields[3]=='0A' and fields[9] in inodes:
            addr,p=fields[1].split(':')
            if int(p,16)==port:found.append(addr)
    require(found and all(x=='0100007F' for x in found),'Private EPMD listener not owned/loopback')
    return dict(Pid=pid,Port=port,ListenerAddress='127.0.0.1',OwnedSocketInodes=sorted(inodes))

def retire_peer(pid,start,installed,peer_name,beam):
    if pid is None:return dict(Status='NO_PEER_PID_OBSERVED',Forced=False)
    token=proc_token(pid)
    if token is None:return dict(Status='PEER_OS_PROCESS_GONE',Forced=False)
    proc=Path('/proc',str(pid))
    # Never signal a PID without current executable, unique name/path and generation ownership.
    args=(proc/'cmdline').read_bytes().split(b'\0')
    require((proc/'exe').resolve(strict=True).is_relative_to(installed) and proc.stat().st_uid==os.getuid(),'Unexpected peer executable/owner')
    require((peer_name+'@127.0.0.1').encode() in args and str(beam).encode() in args,'Unique peer command identity')
    require(start is None or token[1]==start,'Peer PID generation changed')
    start=token[1]
    try:descriptor=os.pidfd_open(pid)
    except ProcessLookupError:return dict(Status='PEER_OS_PROCESS_GONE',Forced=False)
    try:
        current=proc_token(pid)
        if current is None:return dict(Status='PEER_OS_PROCESS_GONE',Forced=False)
        require(current[1]==start,'Peer generation changed before pidfd cleanup')
        for sig,seconds in [(signal.SIGTERM,3),(signal.SIGKILL,5)]:
            try:signal.pidfd_send_signal(descriptor,sig)
            except ProcessLookupError:return dict(Status='FORCED_OWNED_PEER_EXIT',Forced=True,Pid=pid,StartTicks=start)
            deadline=time.monotonic()+seconds
            while time.monotonic()<deadline:
                current=proc_token(pid)
                if current is None or current[1]!=start:return dict(Status='FORCED_OWNED_PEER_EXIT',Forced=True,Pid=pid,StartTicks=start)
                time.sleep(.025)
        raise TimeoutError('Owned peer OS cleanup incomplete')
    finally:os.close(descriptor)

def run_owned(owned,args,d,label,env,timeout,observe=lambda:None):
    save(d/(label+'.attempt.json'),dict(Arguments=args,TimeoutSeconds=timeout))
    op=owned.OwnedProcess(args,d,env,d/label)
    try:op.wait(time.monotonic()+timeout,lambda: observe() or STOP)
    finally:op.close()
    receipt=json.loads((d/(label+'.process.json')).read_bytes())
    require(receipt['ExitCode']==0 and receipt['ParentReaped'] and receipt['StreamsClosed'] and receipt['CleanupFailure'] is None and not receipt['TerminationRequested'] and not receipt['RemainingGroupKillRequested'],'Original owned process or normal cleanup failed')
    return receipt

def collect(a):
    root,tool,m=verify(a);require(sys.platform=='linux','Reviewed Linux ownership only');out=a.output.resolve();require(not out.exists(),'Fresh output');out.mkdir(parents=True)
    base=load(root,m,'collect-original');owned=load(root,m,'owned_process');decoder=load(root,m,'passive_etf')
    # Reuse exact published source/VM/compiler provenance implementation, not a copied framework.
    erlc,installed=base.provenance(a,root,tool,out,owned)
    require(base.git(root,'rev-parse','HEAD').decode().strip()==a.expected_head,'Exact collector checkout')
    anchors=[]
    for anchor in m['PinnedSourceAnchors']:
        name=anchor['Path'];p=checked(a.otp_source.resolve(),name)
        require(p.read_bytes()==base.git(a.otp_source,'show',PIN+':'+name) and binding(p,a.otp_source.resolve())==anchor,'Pinned peer/EPMD/proc source');anchors.append(binding(p,a.otp_source.resolve()))
    epmd=a.erl.parent/'epmd';require(epmd.resolve().is_relative_to(installed),'EPMD installation');save(out/'peer-epmd-provenance.json',dict(Sources=anchors,Epmd=binding(epmd.resolve(),installed)))
    results=[]
    for case in CASES:
        if STOP:break
        d=out/case;d.mkdir();(d/'beam').mkdir();(d/'source').mkdir();nonce=secrets.token_hex(8)
        with socket.socket() as reserve:reserve.bind(('127.0.0.1',0));port=reserve.getsockname()[1]
        env=base.environment(d);env.update(ERL_EPMD_PORT=str(port),ERL_EPMD_ADDRESS='127.0.0.1',ERLANG_NET_PEER_NAME='peer_'+nonce,ERLANG_NET_EXACT_ERL=str(a.erl),ERLANG_NET_MAKER_MARKER='maker_'+nonce,ERLANG_NET_PEER_CAPTURE=str(d))
        cookie='private_peer_'+nonce;origin='origin_'+nonce+'@127.0.0.1';row=dict(Name=case,Status='NOT_EXECUTED',Port=port,Origin=origin,SdkExecuted=False,ExpectedValuesPresent=False);mapper=None;peer_pid=None;peer_start=None
        def observe():
            nonlocal peer_pid,peer_start
            if peer_pid is None:
                # Harvest only descendants of our still-owned origin, before any failure can retire it.
                pending=[parent_pid[0]];seen=set()
                while pending and peer_pid is None:
                    current=pending.pop()
                    if current is None or current in seen:continue
                    seen.add(current)
                    try:children=Path('/proc',str(current),'task',str(current),'children').read_text().split()
                    except (FileNotFoundError, ProcessLookupError):continue
                    for child in children:
                        child=int(child);pending.append(child);proc=Path('/proc',str(child))
                        try:
                            args=(proc/'cmdline').read_bytes().split(b'\0');token=proc_token(child)
                            if token is not None and (env['ERLANG_NET_PEER_NAME']+'@127.0.0.1').encode() in args and str(d/'beam').encode() in args and (proc/'exe').resolve(strict=True).is_relative_to(installed):
                                require(proc.stat().st_uid==os.getuid(),'Peer owner UID');peer_pid=child;peer_start=token[1]
                                save(d/'peer-owned-process.json',dict(Pid=peer_pid,StartTicks=peer_start,OriginOwnedPid=parent_pid[0]));break
                        except (FileNotFoundError, ProcessLookupError):continue
            p=d/'peer-identity.etf'
            if p.exists():
                identity=map_fields(decode(decoder,p));reported=int(text(identity[('atom','os_pid')]))
                require(peer_pid is None or peer_pid==reported,'Peer native PID disagrees with owned descendant')
                if peer_pid is None:peer_pid=reported
            return False
        parent_pid=[None]
        try:
            for module in ('proc_lib_real_peer_probe','proc_lib_remote_payload'):(d/'source'/(module+'.erl')).write_bytes((tool/(module+'.erl')).read_bytes())
            run_owned(owned,[str(erlc),'-o',str(d/'beam'),str(d/'source/proc_lib_real_peer_probe.erl'),str(d/'source/proc_lib_remote_payload.erl')],d,'compile',env,a.timeout_seconds)
            save(d/'epmd.attempt.json',dict(Arguments=[str(epmd),'-address','127.0.0.1','-port',str(port)],Foreground=True))
            mapper=owned.OwnedProcess([str(epmd),'-address','127.0.0.1','-port',str(port)],d,env,d/'epmd')
            end=time.monotonic()+5
            while True:
                try:owner=epmd_owner(mapper,port);break
                except ValueError:
                    require(time.monotonic()<end and mapper.poll() is None,'Private EPMD startup failed');time.sleep(.025)
            save(d/'epmd-owner.json',owner)
            expression='proc_lib_real_peer_probe:run('+case+',os:getenv("ERLANG_NET_PEER_CAPTURE")),halt(0).'
            command=[str(a.erl),'+S','2:2','+pc','latin1','-name',origin,'-setcookie',cookie,'-start_epmd','false','-kernel','inet_dist_use_interface','{127,0,0,1}','-noshell','-noinput','-boot','start_clean','-pa',str(d/'beam'),'-eval',expression]
            save(d/'run.attempt.json',dict(Arguments=command,TimeoutSeconds=a.timeout_seconds));vm=owned.OwnedProcess(command,d,env,d/'run');parent_pid[0]=vm.process.pid
            try:vm.wait(time.monotonic()+a.timeout_seconds,lambda: observe() or STOP)
            finally:vm.close()
            receipt=json.loads((d/'run.process.json').read_bytes());require(receipt['ExitCode']==0 and receipt['CleanupFailure'] is None and receipt['ParentReaped'] and receipt['StreamsClosed'] and not receipt['TerminationRequested'] and not receipt['RemainingGroupKillRequested'],'Parent VM not normally joined')
            require(decode(decoder,d/'outer.etf')==('tuple',(('atom','result'),('atom','ok'))),'Scenario exception retained')
            require(decode(decoder,d/'stage.etf')==('tuple',(('atom','complete'),('atom',case))),'Incomplete scenario')
            for name in ('child-cleanup.etf','peer-controller-cleanup.etf'):
                require(decode(decoder,d/name)==('tuple',(('atom','observed'),('atom','normal'))),'Normal child/controller DOWN required')
            require(decode(decoder,d/'peer-stop.etf')==('atom','ok'),'Peer stop incomplete')
            node_cleanup=decode(decoder,d/'peer-node-cleanup.etf');require(node_cleanup[0]=='tuple' and node_cleanup[1][:2]==(('atom','observed'),('atom','nodedown')),'Peer nodedown missing')
            for name in ('primary-down.etf','linked-exit.etf'):
                if (d/name).exists():require(decode(decoder,d/name)==('tuple',(('atom','observed'),('atom','normal'))),'Normal optional relation cleanup required')
            if peer_pid is None:observe()
            current=proc_token(peer_pid) if peer_pid is not None else None
            require(peer_pid is not None and (current is None or peer_start is not None and current[1]!=peer_start),'Peer OS process remains alive')
            for name in ('origin-identity.etf','peer-identity.etf'):
                identity=map_fields(decode(decoder,d/name));require(text(identity[('atom','otp')])=='29' and Path(text(identity[('atom','root')])).resolve()==installed,'Both exact original runtime roots')
                verify_module_identities(identity,installed,(d/'beam').resolve())
            row['Status']='OBSERVED_NORMAL_CHILD_PEER_AND_OS_CLEANUP'
        except Exception as error:row.update(Status='FAILED_RAW_RETAINED',Error=repr(error),ErrorTraceback=traceback.format_exc())
        finally:
            try:
                if peer_pid is None and (d/'peer-identity.etf').exists():
                    peer_pid=int(text(map_fields(decode(decoder,d/'peer-identity.etf'))[('atom','os_pid')]))
                cleanup=retire_peer(peer_pid,peer_start,installed,env['ERLANG_NET_PEER_NAME'],d/'beam')
                save(d/'peer-os-cleanup.json',cleanup)
                if cleanup['Forced']:row.update(Status='FAILED_RAW_RETAINED',ForcedPeerCleanup=True)
            except Exception as error:row.update(Status='FAILED_RAW_RETAINED',PeerOsCleanupError=repr(error))
            if mapper is not None:
                try:
                    owner=epmd_owner(mapper,port);save(d/'epmd-owner-before-stop.json',owner)
                    run_owned(owned,[str(epmd),'-port',str(port),'-names'],d,'epmd-names',env,5)
                    require(not re.search(rb'(?m)^name ',(d/'epmd-names.stdout').read_bytes()),'Owned EPMD still has registered nodes')
                    run_owned(owned,[str(epmd),'-port',str(port),'-kill'],d,'epmd-stop',env,5)
                    mapper.wait(time.monotonic()+5,lambda:STOP)
                except Exception as error:row.update(Status='FAILED_RAW_RETAINED',EpmdCleanupError=repr(error))
                finally:mapper.close()
                er=json.loads((d/'epmd.process.json').read_bytes())
                if er['ExitCode']!=0 or er['TerminationRequested'] or er['RemainingGroupKillRequested'] or er['CleanupFailure'] is not None:row.update(Status='FAILED_RAW_RETAINED',EpmdNormalExit=False)
        raw=[]
        for p in sorted(d.glob('*.etf')):
            try:decode(decoder,p)
            except Exception as error:row.update(Status='FAILED_RAW_RETAINED',RawDecodeError=repr(error))
            raw.append(binding(p,d))
        row['RawEtf']=raw;save(d/'observation.json',row);results.append(row);print(json.dumps(dict(Name=case,Status=row['Status'])),flush=True)
    verify(a);passed=len(results)==len(CASES) and all(r['Status']=='OBSERVED_NORMAL_CHILD_PEER_AND_OS_CLEANUP' for r in results)
    save(out/'observations.json',dict(Status='ORIGINAL_REAL_PEER_OBSERVATIONS_COMPLETE' if passed else 'ORIGINAL_REAL_PEER_OBSERVATIONS_FAILED',PlannedCases=len(CASES),Cases=results,CollectorHead=a.expected_head,SourceManifestSha256=a.manifest_sha256.upper(),PinnedOtpCommit=PIN,SdkExecuted=False,ExpectedValuesPresent=False,Scope='Actual original TCP peers on loopback, not SDK interoperability or semantic completion'))
    return 0 if passed else 1

def stop(_signal,_frame):
    global STOP
    STOP=True
if __name__=='__main__':
    p=argparse.ArgumentParser(description=__doc__);p.add_argument('mode',choices=['verify','collect']);p.add_argument('--repository',type=Path,required=True);p.add_argument('--manifest-sha256',required=True);p.add_argument('--expected-head',required=True);p.add_argument('--otp-source',type=Path);p.add_argument('--erl',type=Path);p.add_argument('--output',type=Path);p.add_argument('--timeout-seconds',type=int,default=60);a=p.parse_args();require(1<=a.timeout_seconds<=120,'Bounded timeout');signal.signal(signal.SIGTERM,stop);signal.signal(signal.SIGINT,stop)
    if a.mode=='verify':verify(a);print('SOURCE_VERIFIED_NOT_EXECUTED')
    else:require(a.otp_source and a.erl and a.output,'Original capture arguments');raise SystemExit(collect(a))
