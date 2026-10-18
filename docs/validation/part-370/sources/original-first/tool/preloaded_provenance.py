"""Linux ERTS/preloaded-module provenance; never claims disk companion is loaded code."""
from pathlib import Path
import hashlib

SCHEMA='erlang-preloaded-v1'
POLICY='compiled-monitored-worker-values-and-stacks-v1'

def sha(path):return hashlib.sha256(path.read_bytes()).hexdigest().upper()

def module_md5(data):
    assert len(data)<=32*1024*1024 and data[:4]==b'FOR1' and data[8:12]==b'BEAM'
    assert int.from_bytes(data[4:8],'big')+8==len(data)
    chunks={};position=12
    while position<len(data):
        assert position+8<=len(data)
        name=data[position:position+4].decode('ascii');size=int.from_bytes(data[position+4:position+8],'big')
        assert name not in chunks and position+8+size<=len(data)
        chunks[name]=data[position+8:position+8+size];position+=8+((size+3)//4)*4
    assert position==len(data) and all(name in chunks for name in ['Code','StrT','ImpT','ExpT'])
    significant=[]
    for name in ['Atom','AtU8','Code','StrT','ImpT','ExpT','FunT','LitT','Meta','Recs','DbgB']:
        if name not in chunks:continue
        chunk=chunks[name]
        if name=='FunT':
            chunk=bytearray(chunk)
            for offset in range(4,len(chunk)-23,24):chunk[offset+20:offset+24]=b'\0'*4
            chunk=bytes(chunk)
        significant.append(chunk)
    return hashlib.md5(b''.join(significant)).digest()

def runtime_inputs(runtime,build,tracked,out):
    rows=build['InstalledFiles']
    emulator=[r for r in rows if r['Path'].endswith('/bin/beam.smp')]
    companion=[r for r in rows if r['Path'].endswith('/erlang.beam')]
    assert len(emulator)==len(companion)==1
    executable=(runtime/emulator[0]['Path']).resolve();beam=(runtime/companion[0]['Path']).resolve()
    assert executable.is_relative_to(runtime) and beam.is_relative_to(runtime)
    assert sha(executable)==emulator[0]['Sha256'] and sha(beam)==companion[0]['Sha256']
    raw=beam.read_bytes();(out/'erlang-preloaded-companion.beam').write_bytes(raw)
    return dict(MetadataPolicy=SCHEMA,InstalledEmulatorPath=str(executable),InstalledEmulatorSha256=sha(executable),InstalledEmulatorMd5=hashlib.md5(executable.read_bytes()).hexdigest().upper(),InstalledEmulatorBytes=executable.stat().st_size,InstalledCompanionPath=str(beam),InstalledCompanionSha256=sha(beam),InstalledCompanionModuleMd5=module_md5(raw).hex().upper(),CompanionIsLoadedObjectClaimed=False,PinnedErlangSource=next(x for x in tracked['TrackedFiles'] if x['Path']=='erts/preloaded/src/erlang.erl'),PinnedBifTable=next(x for x in tracked['TrackedFiles'] if x['Path']=='erts/emulator/beam/bif.tab'))

def expression_prefix(loads,module,names):
    assert all(c.isalnum() or c=='_' for c in module+names)
    return ('O=os:getenv("FAMILY_OUTPUT"),E=os:getenv("FAMILY_EBIN"),'+loads+
        ',{module,erlang}=code:ensure_loaded(erlang),{ok,Executable}=file:read_link("/proc/self/exe"),'+
        '{ok,ExecutableBytes}=file:read_file("/proc/self/exe"),'+
        'M=#{schema=>erlang_preloaded_v1,subject=>erlang,root=>code:root_dir(),which=>code:which(erlang),loaded=>code:is_loaded(erlang),'+
        'module_md5=>erlang:module_info(md5),exports=>erlang:module_info(exports),exe=>Executable,exe_md5=>erlang:md5(ExecutableBytes),exe_bytes=>byte_size(ExecutableBytes),'+
        'os_pid=>os:getpid(),erts_version=>erlang:system_info(version),otp_release=>erlang:system_info(otp_release),system_architecture=>erlang:system_info(system_architecture),'+
        'emu_flavor=>erlang:system_info(emu_flavor),build_type=>erlang:system_info(build_type),schedulers=>erlang:system_info(schedulers),schedulers_online=>erlang:system_info(schedulers_online),'+
        'fixture_file=>code:which('+module+'),names=>'+module+':'+names+'(),value_acquisition=>"'+POLICY+'"},'+
        'ok=file:write_file(filename:join(O,"metadata.etf"),term_to_binary(M)),')

def text(node):
    assert node[0]=='list' and all(x[0]=='integer' and 0<=x[1]<=0x10ffff for x in node[1])
    return ''.join(chr(x[1]) for x in node[1])

def fields(node):
    assert node[0]=='map' and all(k[0]=='atom' for k,v in node[1])
    result={k[1]:v for k,v in node[1]};assert len(result)==len(node[1])
    return result

def validate(node,inputs,runtime,fixture,ebin,pid,selected):
    data=fields(node)
    expected={'schema','subject','root','which','loaded','module_md5','exports','exe','exe_md5','exe_bytes','os_pid','erts_version','otp_release','system_architecture','emu_flavor','build_type','schedulers','schedulers_online','fixture_file','names','value_acquisition'}
    assert set(data)==expected
    assert data['schema']==('atom','erlang_preloaded_v1') and data['subject']==('atom','erlang')
    assert data['which']==('atom','preloaded') and data['loaded']==('tuple',(('atom','file'),('atom','preloaded')))
    assert Path(text(data['root'])).resolve()==runtime
    assert Path(text(data['exe'])).resolve()==Path(inputs['InstalledEmulatorPath']).resolve()
    assert data['exe_md5']==('binary',bytes.fromhex(inputs['InstalledEmulatorMd5'])) and data['exe_bytes']==('integer',inputs['InstalledEmulatorBytes'])
    assert text(data['os_pid'])==str(pid)
    assert data['module_md5']==('binary',bytes.fromhex(inputs['InstalledCompanionModuleMd5']))
    assert text(data['otp_release'])=='29' and text(data['erts_version']) and text(data['system_architecture'])
    assert data['emu_flavor'][0]==data['build_type'][0]=='atom'
    assert data['schedulers']==data['schedulers_online']==('integer',2)
    assert Path(text(data['fixture_file'])).resolve()==(ebin/(fixture['Module']+'.beam')).resolve()
    assert data['names']==('list',tuple(('atom',x) for x in fixture['Names'])) and text(data['value_acquisition'])==POLICY
    exports=data['exports'];assert exports[0]=='list'
    table=[]
    for row in exports[1]:
        assert row[0]=='tuple' and len(row[1])==2 and row[1][0][0]=='atom' and row[1][1][0]=='integer'
        table.append('erlang:'+row[1][0][1]+'/'+str(row[1][1][1]))
    assert len(table)==len(set(table)) and set(selected)<=set(table)
    return dict(MetadataPolicy=SCHEMA,ActualEmulatorBound=True,PreloadedModuleMd5MatchesInstalledCompanion=True,CompanionIsLoadedObjectClaimed=False,RecordedExportCount=len(table),SelectedMfasPresent=len(selected),OsPid=pid,ValueAcquisition=POLICY)

def validate_outcome(node,stacked):
    assert node[0]=='tuple' and len(node[1])>=2
    parts=node[1]
    if parts[0]==('atom','ok'):assert len(parts)==2
    else:
        assert parts[0]==('atom','error') and len(parts)==(4 if stacked else 3)
        assert parts[1][0]=='atom' and parts[1][1] in ['error','exit','throw']
        if stacked:assert parts[3][0]=='list'
