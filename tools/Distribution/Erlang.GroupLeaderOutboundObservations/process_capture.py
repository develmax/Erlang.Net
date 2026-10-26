import json
import os
import signal
import subprocess


def execute(arguments, cwd, environment, prefix, timeout=90):
    process = subprocess.Popen(arguments, cwd=cwd, env=environment, stdout=subprocess.PIPE, stderr=subprocess.PIPE, start_new_session=True)
    timed_out = False
    pipes_closed = True
    reaped = True
    try:
        stdout, stderr = process.communicate(timeout=timeout)
    except subprocess.TimeoutExpired:
        timed_out = True
        try:
            os.killpg(process.pid, signal.SIGKILL)
        except ProcessLookupError:
            pass
        try:
            stdout, stderr = process.communicate(timeout=10)
        except subprocess.TimeoutExpired as error:
            pipes_closed = False
            stdout, stderr = error.stdout or b'', error.stderr or b''
            process.stdout.close()
            process.stderr.close()
            try:
                process.wait(timeout=5)
            except subprocess.TimeoutExpired:
                reaped = False
    prefix.with_suffix('.stdout').write_bytes(stdout)
    prefix.with_suffix('.stderr').write_bytes(stderr)
    prefix.with_suffix('.process.json').write_text(json.dumps({'Arguments': arguments, 'OwnedPid': process.pid, 'ExitCode': process.returncode, 'TimedOut': timed_out, 'TimeoutSeconds': timeout, 'PipesClosed': pipes_closed, 'ParentReaped': reaped}, indent=2) + '\n', encoding='utf-8')
    if timed_out or process.returncode != 0:
        raise RuntimeError('Original process failed or timed out: ' + str(prefix))
    return subprocess.CompletedProcess(arguments, process.returncode, stdout, stderr)
