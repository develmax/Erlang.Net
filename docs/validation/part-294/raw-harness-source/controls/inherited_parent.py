import ctypes
from ctypes import wintypes
import json
import os
from pathlib import Path
import subprocess
import sys

holder, identity, nonce, seconds = sys.argv[1:]
os.write(1, bytes([80, 255, 0]))
os.write(2, bytes([81, 254]))
child = subprocess.Popen(
    [sys.executable, holder, seconds],
    stdin=subprocess.DEVNULL,
    stdout=sys.stdout.buffer,
    stderr=sys.stderr.buffer,
    close_fds=True,
    creationflags=subprocess.CREATE_NO_WINDOW,
)
try:
    get_times = ctypes.windll.kernel32.GetProcessTimes
    get_times.argtypes = [wintypes.HANDLE] + [ctypes.POINTER(wintypes.FILETIME)] * 4
    get_times.restype = wintypes.BOOL
    created, exited, kernel, user = [wintypes.FILETIME() for _ in range(4)]
    if not get_times(
        wintypes.HANDLE(int(child._handle)),
        ctypes.byref(created),
        ctypes.byref(exited),
        ctypes.byref(kernel),
        ctypes.byref(user),
    ):
        raise ctypes.WinError()
    record = {
        "Pid": child.pid,
        "CreationFileTimeUtc": (created.dwHighDateTime << 32) | created.dwLowDateTime,
        "Executable": str(Path(sys.executable).resolve()),
        "Nonce": nonce,
        "ChildArguments": [sys.executable, holder, seconds],
        "HoldSeconds": int(seconds),
    }
    with open(identity, "x", encoding="utf-8", newline="\n") as stream:
        json.dump(record, stream, indent=2)
        stream.write("\n")
except BaseException:
    child.kill()
    child.wait(timeout=2)
    raise

# Deliberately do not wait: the owned child retains the redirected stdout/stderr handles.
raise SystemExit(0)
