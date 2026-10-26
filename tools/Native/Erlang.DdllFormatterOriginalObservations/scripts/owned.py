"""Root-owned focused execution proposal. Source author did not build or run it."""
import argparse
import hashlib
import json
import os
from pathlib import Path
import subprocess
import time


def digest(path):
    return hashlib.sha256(path.read_bytes()).hexdigest().upper()


def write(path, value):
    path.write_text(json.dumps(value, indent=2) + "\n", encoding="utf-8")


def owned(command, directory, name, timeout, environment, cwd):
    stdout_path = directory / (name + ".stdout.log")
    stderr_path = directory / (name + ".stderr.log")
    started = time.monotonic()
    process = None
    timed_out = False
    failures = []
    with stdout_path.open("xb") as stdout, stderr_path.open("xb") as stderr:
        try:
            process = subprocess.Popen(command, cwd=cwd, env=environment, stdout=stdout, stderr=stderr)
            write(directory / (name + ".live.json"), {"Pid": process.pid, "Command": command, "StartedUnixSeconds": time.time()})
            try:
                process.wait(timeout=timeout)
            except subprocess.TimeoutExpired:
                timed_out = True
        except Exception as error:
            failures.append(repr(error))
        finally:
            if process is not None and process.poll() is None:
                try:
                    if os.name == "nt":
                        killed = subprocess.run(["taskkill", "/PID", str(process.pid), "/T", "/F"], capture_output=True, timeout=15)
                        (directory / (name + ".kill.stdout.log")).write_bytes(killed.stdout)
                        (directory / (name + ".kill.stderr.log")).write_bytes(killed.stderr)
                        if killed.returncode != 0 and process.poll() is None:
                            failures.append("Owned process-tree termination failed: " + str(killed.returncode))
                    else:
                        process.kill()
                    process.wait(timeout=15)
                except Exception as error:
                    failures.append(repr(error))
    receipt = {"Command": command, "Pid": None if process is None else process.pid,
        "ElapsedSeconds": time.monotonic() - started, "TimedOut": timed_out,
        "Settled": process is not None and process.poll() is not None,
        "ExitCode": None if process is None else process.poll(), "Failures": failures,
        "StdoutSha256": digest(stdout_path), "StderrSha256": digest(stderr_path)}
    write(directory / (name + ".exit.json"), receipt)
    if not receipt["Settled"] or timed_out or failures or receipt["ExitCode"] != 0:
        raise RuntimeError("Owned stage failed; all available raw evidence retained: " + name)
    return receipt

