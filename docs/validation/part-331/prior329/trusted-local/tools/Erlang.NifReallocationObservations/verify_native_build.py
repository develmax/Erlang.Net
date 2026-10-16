"""Passive independent native/managed build receipt gate; never loads binaries."""
import argparse,json,pathlib,struct,sys,os,shutil
import runtime_receipt_adapter as A
from reviewed_common import verified_plan,verify_otp,exact_head,sha,write_fresh

def require(v,m):
    if not v:raise ValueError(m)
def read(p):return A.read(p)
def filecheck(root,item):
    A.relative_path(item['Path']);path=(root/item['Path']).resolve();require(path.is_relative_to(root.resolve()),'Escaping build member');raw=path.read_bytes();require(type(item['Bytes']) is int and len(raw)==item['Bytes'] and sha(raw)==item['Sha256'],'Build member changed');return path

def elf(path):
    b=path.read_bytes();require(b[:6]==b'\x7fELF\x02\x01','ELF64 little endian required');require(struct.unpack_from('<HH',b,16)==(3,62),'ET_DYN x86_64 required');u64=lambda p:struct.unpack_from('<Q',b,p)[0];shoff=u64(40);shsize,shcount=struct.unpack_from('<HH',b,58);require(shsize==64 and 0<shcount<4096 and shoff+shsize*shcount<=len(b),'ELF section bounds');sections=[struct.unpack_from('<IIQQQQIIQQ',b,shoff+i*shsize) for i in range(shcount)];exports=set();imports=set();needed=[]
    def string(section,offset):
        start=section[4]+offset;end=b.find(b'\0',start,section[4]+section[5]);require(end>=start,'ELF string bounds');return b[start:end].decode('ascii')
    for section in sections:
        kind,offset,size,link,entsize=section[1],section[4],section[5],section[6],section[9];require(offset+size<=len(b) or kind==8,'ELF section outside file')
        if kind==11:
            require(entsize==24 and link<len(sections),'Dynamic symbol shape')
            for at in range(offset,offset+size,entsize):
                name,info,other,index,value,length=struct.unpack_from('<IBBHQQ',b,at)
                if not name:continue
                symbol=string(sections[link],name)
                if index==0:imports.add(symbol)
                elif info>>4 in (1,2) and other&3 in (0,3):exports.add(symbol)
        if kind==6:
            require(link<len(sections) and entsize==16,'Dynamic table shape')
            for at in range(offset,offset+size,16):
                tag,value=struct.unpack_from('<qQ',b,at)
                if tag==1:needed.append(string(sections[link],value))
                require(tag not in (15,29),'Unexpected RPATH/RUNPATH')
    phoff=u64(32);phsize,phcount=struct.unpack_from('<HH',b,54);require(phsize==56 and 0<phcount<256 and phoff>=64 and phoff+phsize*phcount<=len(b),'Program header bounds');segments=[]
    for at in range(phoff,phoff+phsize*phcount,phsize):
        kind,flags,offset,vaddr,paddr,filesz,memsz,align=struct.unpack_from('<IIQQQQQQ',b,at);require(offset+filesz<=len(b) and memsz>=filesz,'ELF segment bounds');require(not(kind in (1,0x6474e551) and flags&3==3),'Writable executable ELF segment');require(kind!=3,'Unexpected executable interpreter');segments.append((kind,offset,filesz))
    require(any(x[0]==1 and x[2]>0 for x in segments) and sum(x[0]==2 for x in segments)==1,'Loaded shared-object segments missing')
    dynamic_sections=[s for s in sections if s[1]==6];require(len(dynamic_sections)==1 and (2,dynamic_sections[0][4],dynamic_sections[0][5]) in segments,'Dynamic section/program linkage differs')
    require(set(needed)<= {'libc.so.6'},'Unreviewed native dependent library')
    return {'Machine':'x86_64','Class':'ELF64','Type':'ET_DYN','Exports':sorted(exports),'Imports':sorted(imports),'Needed':needed,'RpathOrRunpath':False,'WritableExecutableSegments':False}

def process(root,name,expected,timeout):
    stem=name.replace('.','-');p=read(root/(stem+'.process.json'));require(p['Arguments']==[str(x) for x in expected] and type(p['ExitCode']) is int and p['ExitCode']==0 and p['TimedOut'] is False and p['PipesClosed'] is True and p['ParentReaped'] is True and p['TimeoutSeconds']==timeout and type(p['OwnedPid']) is int and p['OwnedPid']>0,'Failed/mismatched native build process');require(not (root/(stem+'.stderr')).read_bytes(),'Unexpected native build stderr');return (root/(stem+'.stdout')).read_bytes()

def verify(root,source,planpath,approved,head,output,after_sdk=False):
    plan=verified_plan(root,planpath,approved);exact_head(root,head);verify_otp(source,source/'observer-install/lib/erlang/usr/include',plan);receipt=read(output/'build-receipt.json');require(receipt['Status']=='BUILT_NOT_EXECUTED' and receipt['Head']==head and receipt['PlanSha256']==sha(planpath.read_bytes()) and receipt['SdkNativeExecuted'] is False,'Native build scope differs');names=set()
    for item in receipt['Files']:require(item['Path'] not in names,'Duplicate build file');names.add(item['Path']);filecheck(output,item)
    additions={'sdk-attempt.json','sdk.stdout','sdk.stderr','sdk.process.json','sdk-receipt.json'} if after_sdk else set();require({p.relative_to(output).as_posix() for p in output.rglob('*') if p.is_file()}==names|{'build-receipt.json'}|additions,'Native build output closure differs')
    resolved_tools={name:shutil.which(name) for name in ('dotnet','gcc','nm','readelf')};require(all(resolved_tools.values()),'Independently resolved native build tools required');dotnet=resolved_tools['dotnet'];gcc=resolved_tools['gcc'];require(process(output,'sdk-version',[dotnet,'--version'],90).strip().decode()==plan['DotNetSdk'],'SDK version differs');require(bool(process(output,'compiler-version',[gcc,'--version'],90)),'Compiler version absent');include=source/'observer-install/lib/erlang/usr/include';native=root/'tools/Erlang.NifReallocationObservations/native';old=root/'tools/Erlang.NifNativeBackend/native';flags=['-shared','-fPIC','-O2','-std=c11','-Wall','-Wextra','-Werror','-I',str(include)];recipes={'reallocation_fixture.so':[native/'reallocation_fixture.c'],'reallocation_extension.so':[native/'nif_reallocation_bridge.c'],'legacy_fixture.so':[root/'tools/Erlang.NifFixtureHost/native/fixture.c'],'legacy_bridge.so':[old/'nif_bridge_exports.c',old/'nif_bridge_varargs.c']};binaries=[];tools={gcc,dotnet}
    for name,sources in recipes.items():
        path=output/name;process(output,name+'-compile',[gcc,*flags,'-fvisibility=hidden',*sources,'-o',path],90);defined=read(output/(name.replace('.','-')+'-defined.process.json'))['Arguments'];undefined=read(output/(name.replace('.','-')+'-undefined.process.json'))['Arguments'];elfargs=read(output/(name.replace('.','-')+'-readelf.process.json'))['Arguments'];nm=resolved_tools['nm'];readelf=resolved_tools['readelf'];require(defined[0]==undefined[0]==nm and elfargs[0]==readelf,'Substituted nm/readelf identity');tools.update([nm,readelf]);definitions=process(output,name+'-defined',[nm,'-D','--defined-only',path],90);references=process(output,name+'-undefined',[nm,'-D','--undefined-only',path],90);raw_readelf=process(output,name+'-readelf',[readelf,'-h','-Ws','-d',path],90);require(b'ELF Header:' in raw_readelf and b'ELF64' in raw_readelf and b'Symbol table' in raw_readelf and b'Dynamic section' in raw_readelf,'Empty or malformed readelf capture');parsed=elf(path);captured_exports={x.split()[-1] for x in definitions.decode().splitlines() if x.split()};captured_imports={x.split()[-1].split('@')[0] for x in references.decode().splitlines() if x.split()};require(set(parsed['Exports'])==captured_exports==set(plan['NativeExports'][name]),'Raw/captured/allowed exports differ');require(set(parsed['Imports'])==captured_imports and captured_imports<=set(plan['NativeImports'][name]),'Raw/captured/allowed imports differ');binaries.append({'Name':name,'Sha256':sha(path.read_bytes()),'Elf':parsed})
    process(output,'build-managed',[dotnet,'build',root/'tools/Erlang.NifNativeBackend/Erlang.NifNativeBackend.csproj','-c','Release','-o',output/'managed','--nologo','-m:2','-nodeReuse:false','-p:UseSharedCompilation=false','-p:RestoreDisableParallel=true'],600);require((output/'managed/Erlang.NifNativeBackend.dll').is_file() and (output/'managed/Erlang.Runtime.dll').is_file(),'Actual backend/Runtime outputs missing');deps=read(output/'managed/Erlang.NifNativeBackend.deps.json');managed=[]
    for target in deps['targets'].values():
        for library in target.values():
            for name in library.get('runtime',{}):require(pathlib.PurePosixPath(name).name==name,'Unexpected managed runtime path');managed.append(A.digest(output/'managed'/name))
    return {'Passed':True,'Authority':'PublishedRootSourceReviewedPassiveNativeVerifier','Head':head,'PlanSha256':sha(planpath.read_bytes()),'VerifierSha256':sha(pathlib.Path(__file__).read_bytes()),'ApprovedBuildReceiptSha256':sha((output/'build-receipt.json').read_bytes()),'Binaries':binaries,'ManagedRuntimeDependencies':managed,'ActualTools':[{'Role':name,'CommandPath':resolved_tools[name],'CanonicalFile':A.digest(pathlib.Path(resolved_tools[name]).resolve())} for name in sorted(resolved_tools)],'NativeExecutedByVerifier':False,'ManagedExecutedByVerifier':False}

def approval_output(output,repository,capture_roots):
    path=output.resolve();require(path.is_relative_to(repository.resolve()/'artifacts'),'Approval output must belong to artifact namespace');require(not output.exists(),'Fresh approval output required')
    for root in capture_roots:
        if root is not None:require(not path.is_relative_to(root.resolve()),'Approval output overlaps source or strict capture root')

def main():
    p=argparse.ArgumentParser()
    for n in ('repository','otp-source','plan','native-build','output'):p.add_argument('--'+n,type=pathlib.Path,required=True)
    for n in ('expected-head','approved-plan-sha256'):p.add_argument('--'+n,required=True)
    a=p.parse_args();approval_output(a.output,a.repository,[a.otp_source,a.native_build]);A.validate_platform(sys.platform,os.uname().machine);write_fresh(a.output,verify(a.repository.resolve(),a.otp_source.resolve(),a.plan,a.approved_plan_sha256,a.expected_head,a.native_build.resolve()))
if __name__=='__main__':main()
