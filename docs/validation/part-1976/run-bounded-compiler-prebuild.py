from pathlib import Path
import ctypes as c
from ctypes import wintypes as w
import hashlib,json,os,shutil,subprocess,sys,time

stage=Path(sys.argv[1]).resolve()
trial=stage/"bounded-prebuild"
trial.mkdir()
logs=trial/"driver"
logs.mkdir()
LIMIT_MIB=1024
MINIMUM_PHYSICAL_MIB=1920
MINIMUM_COMMIT_MIB=1984

class MemoryStatus(c.Structure):
    _fields_=[
        ("length",w.DWORD),("load",w.DWORD),("total_physical",c.c_ulonglong),
        ("available_physical",c.c_ulonglong),("total_commit",c.c_ulonglong),
        ("available_commit",c.c_ulonglong),("total_virtual",c.c_ulonglong),
        ("available_virtual",c.c_ulonglong),("available_extended",c.c_ulonglong)
    ]

class BasicLimits(c.Structure):
    _fields_=[
        ("process_time",c.c_longlong),("job_time",c.c_longlong),("flags",w.DWORD),
        ("minimum_set",c.c_size_t),("maximum_set",c.c_size_t),("active",w.DWORD),
        ("affinity",c.c_size_t),("priority",w.DWORD),("scheduling",w.DWORD)
    ]

class Io(c.Structure):
    _fields_=[(f"counter{index}",c.c_ulonglong) for index in range(6)]

class ExtendedLimits(c.Structure):
    _fields_=[
        ("basic",BasicLimits),("io",Io),("process_limit",c.c_size_t),("job_limit",c.c_size_t),
        ("peak_process",c.c_size_t),("peak_job",c.c_size_t)
    ]

kernel=c.WinDLL("kernel32",use_last_error=True)
nt=c.WinDLL("ntdll")
kernel.GlobalMemoryStatusEx.argtypes=[c.POINTER(MemoryStatus)]
kernel.GlobalMemoryStatusEx.restype=w.BOOL
kernel.CreateJobObjectW.argtypes=[c.c_void_p,w.LPCWSTR]
kernel.CreateJobObjectW.restype=w.HANDLE
kernel.SetInformationJobObject.argtypes=[w.HANDLE,c.c_int,c.c_void_p,w.DWORD]
kernel.SetInformationJobObject.restype=w.BOOL
kernel.QueryInformationJobObject.argtypes=[w.HANDLE,c.c_int,c.c_void_p,w.DWORD,c.c_void_p]
kernel.QueryInformationJobObject.restype=w.BOOL
kernel.AssignProcessToJobObject.argtypes=[w.HANDLE,w.HANDLE]
kernel.AssignProcessToJobObject.restype=w.BOOL
kernel.CloseHandle.argtypes=[w.HANDLE]
kernel.CloseHandle.restype=w.BOOL
nt.NtResumeProcess.argtypes=[w.HANDLE]
nt.NtResumeProcess.restype=c.c_long

def check(ok):
    if not ok:
        raise c.WinError(c.get_last_error())

def read(path):
    return path.read_text(encoding="utf-8-sig")

def sha(p):
    return hashlib.sha256(p.read_bytes()).hexdigest().upper()

def json_write(p,value):
    p.write_text(json.dumps(value,indent=2)+"\n",encoding="utf-8",newline="\n")

def snapshot():
    m=MemoryStatus()
    m.length=c.sizeof(m)
    check(kernel.GlobalMemoryStatusEx(c.byref(m)))
    return {"PhysicalMiB":m.available_physical//2**20,"CommitMiB":m.available_commit//2**20}

deadline=time.monotonic()+25*60
while True:
    sample=snapshot()
    with (logs/"memory.jsonl").open("a",encoding="utf-8") as log:
        log.write(json.dumps({"UnixTime":time.time(),**sample})+"\n")
    if sample["PhysicalMiB"]>=MINIMUM_PHYSICAL_MIB and sample["CommitMiB"]>=MINIMUM_COMMIT_MIB:
        break
    if time.monotonic()>=deadline:
        raise TimeoutError("A process-bounded compiler window was not available")
    time.sleep(12)

original=stage/"run-linked-scope.ps1"
original_source=read(original)
cut='$env:DOTNET_PROCESSOR_COUNT=$null\n& '
begin=original_source.index(cut)
prefix=original_source[:begin]
prefix=prefix.replace(stage.as_posix(),trial.as_posix())
assert prefix.count(trial.as_posix())==1
script=trial/"prebuild.ps1"
script.write_text(prefix+"exit 0\n",encoding="utf-8",newline="\n")
shutil.copyfile(stage/"hydration-receipt.json",trial/"hydration-receipt.json")
job=kernel.CreateJobObjectW(None,None)
check(job)
config=ExtendedLimits()
config.basic.flags=0x200 | 0x2000
config.job_limit=LIMIT_MIB*2**20
process=None
started=time.monotonic()
try:
    check(kernel.SetInformationJobObject(job,9,c.byref(config),c.sizeof(config)))
    with (logs/"stdout.log").open("wb") as out,(logs/"stderr.log").open("wb") as err:
        process=subprocess.Popen(["pwsh","-NoProfile","-File",str(script)],cwd=stage,stdout=out,stderr=err,
            creationflags=0x4 | 0x08000000 | subprocess.BELOW_NORMAL_PRIORITY_CLASS,
            env={**os.environ,"DOTNET_ADD_GLOBAL_TOOLS_TO_PATH":"0"})
        handle=w.HANDLE(int(process._handle))
        check(kernel.AssignProcessToJobObject(job,handle))
        status=nt.NtResumeProcess(handle)
        if status!=0:
            raise RuntimeError(f"Unable to resume the assigned build process: {status}")
        rc=process.wait(timeout=10*60)
    observed=ExtendedLimits()
    check(kernel.QueryInformationJobObject(job,9,c.byref(observed),c.sizeof(observed),None))
    json_write(trial/"job-receipt.json",{"StartedWith":sample,"JobLimitMiB":LIMIT_MIB,
        "PeakProcessMiB":observed.peak_process/2**20,"PeakJobMiB":observed.peak_job/2**20,
        "ElapsedSeconds":time.monotonic()-started,"Exit":rc})
    if rc:
        sys.exit(rc)
finally:
    if process is not None and process.poll() is None:
        process.kill()
        process.wait()
    check(kernel.CloseHandle(job))

ready=json.loads(read(trial/"focused-identity.json"))
for name in ("layout-write.log","layout-check.log","source-identities.json","runtime-reuse-receipt.json",
             "compiler-build.log","tests-build.log","focused-identity.json"):
    dest=stage/name
    assert not dest.exists(),dest
    shutil.copyfile(trial/name,dest)
fixed='''
$env:DOTNET_ADD_GLOBAL_TOOLS_TO_PATH = '0'
$ErrorActionPreference='Stop'
(Get-Process -Id $PID).PriorityClass='BelowNormal'
'''+next(line for line in original_source.splitlines() if line.startswith("$stage="))+"\n"+next(
    line for line in original_source.splitlines() if line.startswith("$work="))+"\n"+next(
    line for line in original_source.splitlines() if line.startswith("$sdk="))+"\n"+'''
Set-Location -LiteralPath $work
$env:TEMP=(New-Item "$stage/temp" -ItemType Directory).FullName
$env:TMP=$env:TEMP
$sourceIds=Get-Content -LiteralPath "$stage/source-identities.json" -Raw|ConvertFrom-Json
foreach($file in $sourceIds){
    if((Get-FileHash -LiteralPath "$work/$($file.Path)").Hash -ne $file.Sha256){throw "Prebuilt source changed $($file.Path)"}
}
$bin="$work/tests/Erlang.Tests/bin/Debug/net10.0"
$exact=Get-Content -LiteralPath "$stage/focused-identity.json" -Raw|ConvertFrom-Json
$compilerHash=(Get-FileHash "$bin/Erlang.Compiler.dll").Hash
$runtimeHash=(Get-FileHash "$bin/Erlang.Runtime.dll").Hash
if($compilerHash -ne $exact.Compiler -or $runtimeHash -ne $exact.Runtime -or
   (Get-FileHash "$bin/Erlang.Tests.dll").Hash -ne $exact.Tests){throw 'Bounded prebuild payload identities changed'}
'''
shutil.copyfile(original,trial/"original-linked-scope.ps1")
original.write_text(fixed.lstrip()+original_source[begin:],encoding="utf-8",newline="\n")
archive=stage/"record-and-archive-1976.py"
archive_source=read(archive)
marker='    "scope-plan.json",'
assert archive_source.count(marker)==1
additional=['bounded-prebuild/job-receipt.json','bounded-prebuild/driver/stdout.log',
            'bounded-prebuild/driver/stderr.log','bounded-prebuild/driver/memory.jsonl','bounded-prebuild/prebuild.ps1',
            'bounded-prebuild/original-linked-scope.ps1','run-bounded-compiler-prebuild.py']
archive_source=archive_source.replace(marker,"    "+",\n    ".join(map(json.dumps,additional))+",\n"+marker)
compile(archive_source,str(archive),"exec")
archive.write_text(archive_source,encoding="utf-8",newline="\n")
print(json.dumps({"Built":ready,"PeakMiB":observed.peak_job/2**20}),flush=True)
with (stage/"post-prebuild-resource.stdout.log").open("wb") as out,(stage/"post-prebuild-resource.stderr.log").open("wb") as err:
    next_stage=subprocess.Popen(["pwsh","-NoProfile","-File",str(stage/"launch-with-resource-budget.ps1")],
         cwd=stage,stdout=out,stderr=err,creationflags=0x08000000)
json_write(trial/"next-stage.json",{"Process":next_stage.pid})
