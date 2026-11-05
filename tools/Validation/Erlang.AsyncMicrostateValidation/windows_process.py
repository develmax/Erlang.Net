"""A suspended child joins a private Windows Job before executing any code."""
import ctypes as c
from ctypes import wintypes as w
import hashlib
import json
import msvcrt
import os
from pathlib import Path
import subprocess
import threading
import time


CREATE_LOCK = threading.Lock()
KILL_ON_JOB_CLOSE = 0x2000
STARTF_USESTDHANDLES = 0x100
CREATE_SUSPENDED = 0x4
CREATE_NO_WINDOW = 0x08000000
CREATE_UNICODE_ENVIRONMENT = 0x400
WAIT_TIMEOUT = 258
SIZE_T = c.c_size_t


class Startup(c.Structure):
    _fields_ = [("cb", w.DWORD), ("lpReserved", w.LPWSTR), ("lpDesktop", w.LPWSTR),
                ("lpTitle", w.LPWSTR), ("dwX", w.DWORD), ("dwY", w.DWORD),
                ("dwXSize", w.DWORD), ("dwYSize", w.DWORD), ("dwXCountChars", w.DWORD),
                ("dwYCountChars", w.DWORD), ("dwFillAttribute", w.DWORD),
                ("dwFlags", w.DWORD), ("wShowWindow", w.WORD), ("cbReserved2", w.WORD),
                ("lpReserved2", c.POINTER(c.c_byte)), ("hStdInput", w.HANDLE),
                ("hStdOutput", w.HANDLE), ("hStdError", w.HANDLE)]


class ProcessInfo(c.Structure):
    _fields_ = [("hProcess", w.HANDLE), ("hThread", w.HANDLE),
                ("dwProcessId", w.DWORD), ("dwThreadId", w.DWORD)]


class BasicLimits(c.Structure):
    _fields_ = [("ProcessTime", c.c_longlong), ("JobTime", c.c_longlong),
                ("LimitFlags", w.DWORD), ("MinWorkingSet", SIZE_T),
                ("MaxWorkingSet", SIZE_T), ("ActiveProcessLimit", w.DWORD),
                ("Affinity", SIZE_T), ("PriorityClass", w.DWORD), ("SchedulingClass", w.DWORD)]


class IoCounters(c.Structure):
    _fields_ = [(name, c.c_ulonglong) for name in
                ("ReadOperations", "WriteOperations", "OtherOperations",
                 "ReadBytes", "WriteBytes", "OtherBytes")]


class ExtendedLimits(c.Structure):
    _fields_ = [("Basic", BasicLimits), ("Io", IoCounters),
                ("ProcessMemoryLimit", SIZE_T), ("JobMemoryLimit", SIZE_T),
                ("PeakProcessMemory", SIZE_T), ("PeakJobMemory", SIZE_T)]


class Accounting(c.Structure):
    _fields_ = [("UserTime", c.c_longlong), ("KernelTime", c.c_longlong),
                ("PeriodUserTime", c.c_longlong), ("PeriodKernelTime", c.c_longlong),
                ("PageFaults", w.DWORD), ("TotalProcesses", w.DWORD),
                ("ActiveProcesses", w.DWORD), ("TerminatedProcesses", w.DWORD)]


def api():
    k = c.WinDLL("kernel32", use_last_error=True)
    declarations = {
        "CreateJobObjectW": ([c.c_void_p, w.LPCWSTR], w.HANDLE),
        "SetInformationJobObject": ([w.HANDLE, c.c_int, c.c_void_p, w.DWORD], w.BOOL),
        "QueryInformationJobObject": ([w.HANDLE, c.c_int, c.c_void_p, w.DWORD, c.c_void_p], w.BOOL),
        "AssignProcessToJobObject": ([w.HANDLE, w.HANDLE], w.BOOL),
        "TerminateJobObject": ([w.HANDLE, w.UINT], w.BOOL),
        "TerminateProcess": ([w.HANDLE, w.UINT], w.BOOL),
        "CloseHandle": ([w.HANDLE], w.BOOL),
        "ResumeThread": ([w.HANDLE], w.DWORD),
        "WaitForSingleObject": ([w.HANDLE, w.DWORD], w.DWORD),
        "GetExitCodeProcess": ([w.HANDLE, c.POINTER(w.DWORD)], w.BOOL),
        "OpenProcess": ([w.DWORD, w.BOOL, w.DWORD], w.HANDLE),
        "IsProcessInJob": ([w.HANDLE, w.HANDLE, c.POINTER(w.BOOL)], w.BOOL),
        "QueryFullProcessImageNameW": ([w.HANDLE, w.DWORD, w.LPWSTR, c.POINTER(w.DWORD)], w.BOOL),
        "CreateProcessW": ([w.LPCWSTR, w.LPWSTR, c.c_void_p, c.c_void_p,
                            w.BOOL, w.DWORD, c.c_void_p, w.LPCWSTR,
                            c.POINTER(Startup), c.POINTER(ProcessInfo)], w.BOOL),
    }
    for name, (arguments, result) in declarations.items():
        function = getattr(k, name)
        function.argtypes, function.restype = arguments, result
    return k


def check(value):
    if not value:
        raise c.WinError(c.get_last_error())
    return value


def query(k, job, kind, structure):
    value = structure()
    check(k.QueryInformationJobObject(job, kind, c.byref(value), c.sizeof(value), None))
    return value


def drain(k, job, seconds=15):
    deadline = time.monotonic() + seconds
    active = query(k, job, 1, Accounting).ActiveProcesses
    while active and time.monotonic() < deadline:
        time.sleep(0.05)
        active = query(k, job, 1, Accounting).ActiveProcesses
    return active


COMPILER_RETIREMENT_EXIT1288 = 0xE0011288


def _compiler_require1288(value, reason):
    if not value:
        raise RuntimeError("Compiler ancillary retirement guard: " + reason)


def _compiler_sha1288(path):
    return hashlib.sha256(path.read_bytes()).hexdigest().upper()


def _compiler_profile1288(value, cwd):
    fields = {"Version", "CompilerPath", "CompilerSha256", "AncillaryPath", "AncillarySha256",
              "SourcePath", "SourceSha256", "OutputPath"}
    _compiler_require1288(type(value) is dict and set(value) == fields, "profile schema")
    _compiler_require1288(type(value["Version"]) is int and value["Version"] == 1, "profile version")
    result = dict(value)
    for role in ("Compiler", "Ancillary", "Source"):
        path = Path(value[role + "Path"])
        _compiler_require1288(path.is_absolute() and path.is_file() and not path.is_symlink(), role + " input")
        path = path.resolve(strict=True)
        expected = value[role + "Sha256"]
        _compiler_require1288(type(expected) is str and len(expected) == 64
                              and all(ch in "0123456789ABCDEF" for ch in expected), role + " SHA schema")
        _compiler_require1288(_compiler_sha1288(path) == expected, role + " SHA differs")
        result[role + "Path"] = path
    compiler, ancillary, source = (result[key + "Path"] for key in ("Compiler", "Ancillary", "Source"))
    _compiler_require1288(compiler.name.casefold() == "cl.exe" and ancillary.name.casefold() == "vctip.exe"
                          and ancillary.parent == compiler.parent, "exact compiler-adjacent ancillary")
    output = Path(value["OutputPath"])
    _compiler_require1288(output.is_absolute() and not output.exists(), "output must be fresh before effects")
    output = output.resolve(strict=False)
    _compiler_require1288(output.parent == source.parent == Path(cwd).resolve()
                          and source.suffix.casefold() == ".c" and output.suffix.casefold() == ".dll"
                          and output.stem == source.stem, "exact fixture source/output ownership")
    result["OutputPath"] = output
    return result


def _compiler_artifacts1288(profile):
    for role in ("Compiler", "Ancillary", "Source"):
        _compiler_require1288(_compiler_sha1288(profile[role + "Path"]) == profile[role + "Sha256"],
                              role + " input changed")
    output = profile["OutputPath"]
    _compiler_require1288(output.is_file() and not output.is_symlink(), "actual DLL output missing")
    raw = output.read_bytes()
    _compiler_require1288(len(raw) >= 64 and raw[:2] == b"MZ", "actual DLL DOS header")
    pe = int.from_bytes(raw[60:64], "little")
    _compiler_require1288(pe <= len(raw) - 24 and raw[pe:pe + 4] == b"PE\0\0"
                          and int.from_bytes(raw[pe + 4:pe + 6], "little") == 0x8664
                          and int.from_bytes(raw[pe + 22:pe + 24], "little") & 0x2000,
                          "actual Windows x64 DLL output")
    return {"Path": str(output), "Bytes": len(raw), "Sha256": hashlib.sha256(raw).hexdigest().upper()}


def _compiler_job_pids1288(k, job):
    limit = 1024
    raw = c.create_string_buffer(8 + c.sizeof(c.c_size_t) * limit)
    check(k.QueryInformationJobObject(job, 3, c.byref(raw), c.sizeof(raw), None))
    header = c.cast(raw, c.POINTER(w.DWORD))
    assigned, listed = int(header[0]), int(header[1])
    _compiler_require1288(listed <= limit and assigned == listed, "complete actual Job PID census")
    ids = c.cast(c.addressof(raw) + 8, c.POINTER(c.c_size_t))
    return [int(ids[index]) for index in range(listed)]


def _compiler_retire1288(k, job, profile, audit):
    # Same original15-second cleanup window; classification/wait/drain share it.
    deadline = time.monotonic() + 15
    output = _compiler_artifacts1288(profile)
    audit["OutputBeforeRetirement"] = output
    pids = _compiler_job_pids1288(k, job)
    audit["ObservedJobPids"] = pids
    if not pids:
        _compiler_require1288(query(k, job, 1, Accounting).ActiveProcesses == 0, "natural census consistency")
        audit.update(Success=True, Disposition="Natural", NaturalDescendantsExit=True,
                     TerminationRequested=False, InspectionHandleClosed=True)
        return False
    _compiler_require1288(len(pids) == 1, "only one ancillary member may be retired")
    pid = pids[0]
    handle = None
    audit.update(NaturalDescendantsExit=False, TerminationRequested=False, InspectionHandleClosed=True)
    try:
        # Limited query + terminate + wait; never PROCESS_ALL_ACCESS or PID-only kill.
        handle = check(k.OpenProcess(0x1000 | 0x0001 | 0x00100000, False, pid))
        belongs = w.BOOL()
        check(k.IsProcessInJob(handle, job, c.byref(belongs)))
        _compiler_require1288(belongs.value, "opened PID is not a current member of this exact Job")
        capacity = w.DWORD(32768)
        image = c.create_unicode_buffer(capacity.value)
        check(k.QueryFullProcessImageNameW(handle, 0, image, c.byref(capacity)))
        actual_image = Path(image.value[:capacity.value]).resolve(strict=True)
        _compiler_require1288(os.path.normcase(str(actual_image)) == os.path.normcase(str(profile["AncillaryPath"])),
                              "actual member image differs from declared adjacent VCTIP")
        actual_sha = _compiler_sha1288(actual_image)
        _compiler_require1288(actual_sha == profile["AncillarySha256"], "actual member image SHA differs")
        _compiler_require1288(k.WaitForSingleObject(handle, 0) == WAIT_TIMEOUT, "member already exited before retirement")
        _compiler_require1288(_compiler_job_pids1288(k, job) == [pid], "Job membership changed before retirement")
        # Complete every identity/artifact check BEFORE the only process mutation.
        _compiler_require1288(_compiler_artifacts1288(profile) == output, "artifact changed before retirement")
        audit.update(Pid=pid, Image=str(actual_image), ImageSha256=actual_sha, MembershipVerified=True,
                     RequestedExitCode=COMPILER_RETIREMENT_EXIT1288, Disposition="ValidatedBeforeRetirement")
        check(k.TerminateProcess(handle, COMPILER_RETIREMENT_EXIT1288))
        audit["TerminationRequested"] = True
        remaining = max(0, int((deadline - time.monotonic()) * 1000))
        _compiler_require1288(k.WaitForSingleObject(handle, remaining) == 0, "retired ancillary did not exit")
        code = w.DWORD()
        check(k.GetExitCodeProcess(handle, c.byref(code)))
        audit["ObservedExitCode"] = code.value
        _compiler_require1288(code.value == COMPILER_RETIREMENT_EXIT1288, "ancillary exit differs from actual retirement")
        active = drain(k, job, max(0, deadline - time.monotonic()))
        audit["JobActiveAfterRetirement"] = active
        _compiler_require1288(active == 0 and not _compiler_job_pids1288(k, job), "Job not empty after ancillary retirement")
        _compiler_require1288(_compiler_artifacts1288(profile) == output, "artifact changed after retirement")
        audit.update(Success=True, Disposition="DeclaredAncillaryRetirement", OutputAfterRetirement=output)
        return True
    except Exception as failure:
        audit["RetirementFailure"] = repr(failure)
        raise
    finally:
        if handle:
            try:
                audit["InspectionHandleClosed"] = bool(k.CloseHandle(handle))
                if not audit["InspectionHandleClosed"]:
                    audit["InspectionCloseWin32Error"] = c.get_last_error()
            except Exception as cleanup:
                audit["InspectionHandleClosed"] = False
                audit["InspectionCloseFailure"] = repr(cleanup)
            # Retain the original guard/wait failure if both actions fail.
            if not audit["InspectionHandleClosed"] and "RetirementFailure" not in audit:
                raise RuntimeError("Compiler ancillary retirement inspection handle did not close")


def run(command, directory, name, timeout, environment, cwd, compiler_retirement=None):
    if os.name != "nt":
        raise RuntimeError("This process controller requires Windows")
    k = api()
    started = time.monotonic()
    job = process = thread = None
    errors = []
    timed_out = forced = closed = False
    exit_code = active_after = None
    metrics = {}
    compiler_profile1288 = None
    compiler_audit1288 = None
    pid = None
    stdout_path, stderr_path = directory / (name + ".stdout.log"), directory / (name + ".stderr.log")
    try:
        if compiler_retirement is not None:
            compiler_profile1288 = _compiler_profile1288(compiler_retirement, cwd)
            compiler_audit1288 = {"PolicyVersion": 1, "Kind": "DeclaredMSVCCompilerAncillary",
                                  "Success": False, "Disposition": "AdmittedBeforeEffects"}
        job = check(k.CreateJobObjectW(None, None))
        limits = ExtendedLimits()
        limits.Basic.LimitFlags = KILL_ON_JOB_CLOSE
        check(k.SetInformationJobObject(job, 9, c.byref(limits), c.sizeof(limits)))
        with stdout_path.open("xb") as stdout, stderr_path.open("xb") as stderr, open(os.devnull, "rb") as stdin:
            with CREATE_LOCK:
                handles = [msvcrt.get_osfhandle(f.fileno()) for f in (stdin, stdout, stderr)]
                try:
                    for handle in handles:
                        os.set_handle_inheritable(handle, True)
                    info = Startup()
                    info.cb, info.dwFlags = c.sizeof(info), STARTF_USESTDHANDLES
                    info.hStdInput, info.hStdOutput, info.hStdError = handles
                    pi = ProcessInfo()
                    for key, value in environment.items():
                        if "\0" in key or "\0" in value:
                            raise ValueError("Invalid process environment")
                    block = c.create_unicode_buffer("\0".join(k + "=" + v for k, v in
                                                        sorted(environment.items(), key=lambda p: p[0].upper())) + "\0\0")
                    text = c.create_unicode_buffer(subprocess.list2cmdline(command))
                    check(k.CreateProcessW(command[0], text, None, None, True,
                                           CREATE_SUSPENDED | CREATE_NO_WINDOW | CREATE_UNICODE_ENVIRONMENT,
                                           block, str(cwd), c.byref(info), c.byref(pi)))
                    process, thread, pid = pi.hProcess, pi.hThread, pi.dwProcessId
                    check(k.AssignProcessToJobObject(job, process))
                    if k.ResumeThread(thread) == 0xFFFFFFFF:
                        raise c.WinError(c.get_last_error())
                finally:
                    for handle in handles:
                        os.set_handle_inheritable(handle, False)
            (directory / (name + ".live.json")).write_text(json.dumps({"Pid": pid, "Command": command}))
            wait = k.WaitForSingleObject(process, int(timeout * 1000))
            if wait == WAIT_TIMEOUT:
                timed_out = forced = True
                check(k.TerminateJobObject(job, 1460))
            elif wait != 0:
                raise c.WinError(c.get_last_error())
            if k.WaitForSingleObject(process, 15000) != 0:
                raise RuntimeError("Owned parent did not settle")
            code = w.DWORD()
            check(k.GetExitCodeProcess(process, c.byref(code)))
            exit_code = code.value
            if compiler_profile1288 is not None:
                _compiler_require1288(exit_code == 0 and not timed_out, "nonzero/timed-out main is never ancillary success")
                try:
                    if _compiler_retire1288(k, job, compiler_profile1288, compiler_audit1288):
                        forced = True  # Actual targeted termination remains explicit in the raw receipt.
                finally:
                    if compiler_audit1288.get("TerminationRequested"):
                        forced = True
            elif drain(k, job):
                forced = True
                errors.append("Owned descendants survived the cleanup window")
                check(k.TerminateJobObject(job, 1460))
                if drain(k, job):
                    errors.append("Owned descendants did not settle after termination")
            account = query(k, job, 1, Accounting)
            memory = query(k, job, 9, ExtendedLimits)
            active_after = account.ActiveProcesses
            metrics = {"TotalProcesses": account.TotalProcesses,
                       "UserCpuSeconds": account.UserTime / 10000000,
                       "KernelCpuSeconds": account.KernelTime / 10000000,
                       "PeakJobCommitBytes": memory.PeakJobMemory,
                       "PeakProcessCommitBytes": memory.PeakProcessMemory}
    except Exception as error:
        errors.append(repr(error))
        if job:
            forced = True
            if not k.TerminateJobObject(job, 1460):
                errors.append("Could not terminate the private Job")
        # Assignment can fail while our newly created child is still suspended.
        if process:
            # The suspended parent may not have joined the Job if assignment failed.
            if k.WaitForSingleObject(process, 0) == WAIT_TIMEOUT:
                if not k.TerminateProcess(process, 1460):
                    errors.append("Could not terminate the unpublished parent")
            if k.WaitForSingleObject(process, 15000) != 0:
                errors.append("Failed unpublished parent cleanup")
        if job:
            try:
                active_after = drain(k, job)
                if active_after:
                    errors.append("Owned descendants did not settle on the error path")
            except Exception as cleanup_error:
                errors.append("Job drain failed: " + repr(cleanup_error))
    finally:
        close_results = [bool(k.CloseHandle(handle)) for handle in (thread, process, job) if handle]
        closed = bool(close_results) and all(close_results)
        if not closed:
            errors.append("Could not close every private process/Job handle")
    receipt = {"Command": command, "Pid": pid, "ExitCode": exit_code,
               "ElapsedSeconds": time.monotonic() - started, "TimedOut": timed_out,
               "ForcedCleanup": forced, "ActiveProcessesAfter": active_after,
               "ResourcesClosed": closed, "Errors": errors, **metrics}
    if compiler_audit1288 is not None:
        receipt["ResourcesClosed"] = closed and compiler_audit1288.get("InspectionHandleClosed", True)
        receipt["CompilerRetirement"] = compiler_audit1288
        receipt["NaturalDescendantsExit"] = compiler_audit1288.get("NaturalDescendantsExit")
    for label, path in (("StdoutSha256", stdout_path), ("StderrSha256", stderr_path)):
        receipt[label] = hashlib.sha256(path.read_bytes()).hexdigest().upper() if path.exists() else None
    (directory / (name + ".exit.json")).write_text(json.dumps(receipt, indent=2) + "\n")
    return receipt
