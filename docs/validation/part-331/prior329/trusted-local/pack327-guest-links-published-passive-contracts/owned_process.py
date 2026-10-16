import json
import os
import signal
import subprocess
import time

MAX_STREAM_BYTES = 4 * 1024 * 1024
TERMINATE_SECONDS = 3
KILL_SECONDS = 5


class OwnedProcess:
    def __init__(self, arguments, cwd, environment, prefix):
        self.arguments = arguments
        self.prefix = prefix
        self.stdout = prefix.with_suffix('.stdout').open('xb')
        self.stderr = prefix.with_suffix('.stderr').open('xb')
        self.process = None
        self.closed = False
        self.termination_requested = False
        self.group_kill_requested = False
        self.started = time.monotonic()
        try:
            self.process = subprocess.Popen(arguments, cwd=cwd, env=environment,
                                            stdout=self.stdout, stderr=self.stderr, start_new_session=True)
        except BaseException:
            self.stdout.close()
            self.stderr.close()
            raise

    def poll(self):
        for stream in (self.stdout, self.stderr):
            if os.fstat(stream.fileno()).st_size > MAX_STREAM_BYTES:
                raise RuntimeError('Original process stream exceeds retained evidence budget: ' + str(self.prefix))
        return self.process.poll()

    def wait(self, deadline, cancelled):
        while self.poll() is None:
            if cancelled():
                raise InterruptedError('Owned original collection cancellation: ' + str(self.prefix))
            if time.monotonic() >= deadline:
                raise TimeoutError('Owned original process deadline: ' + str(self.prefix))
            time.sleep(0.025)
        if self.process.returncode != 0:
            raise RuntimeError('Owned original process failed: ' + str(self.prefix))

    def close(self):
        if self.closed:
            return
        self.closed = True
        failure = None
        try:
            if self.process.poll() is None:
                self.termination_requested = True
                try:
                    os.killpg(self.process.pid, signal.SIGTERM)
                except ProcessLookupError:
                    pass
                try:
                    self.process.wait(timeout=TERMINATE_SECONDS)
                except subprocess.TimeoutExpired:
                    pass
            # Also retire remaining descendants if the foreground parent exited first.
            try:
                os.killpg(self.process.pid, signal.SIGKILL)
                self.group_kill_requested = True
            except ProcessLookupError:
                pass
            self.process.wait(timeout=KILL_SECONDS)
        except BaseException as error:
            failure = repr(error)
            raise
        finally:
            self.stdout.close()
            self.stderr.close()
            receipt = {
                'Arguments': self.arguments, 'OwnedPid': self.process.pid,
                'OwnedProcessGroup': self.process.pid, 'ExitCode': self.process.returncode,
                'ElapsedSeconds': time.monotonic() - self.started,
                'TerminationRequested': self.termination_requested,
                'RemainingGroupKillRequested': self.group_kill_requested,
                'ParentReaped': self.process.returncode is not None,
                'CleanupFailure': failure, 'StreamsClosed': True,
                'StdoutBytes': self.prefix.with_suffix('.stdout').stat().st_size,
                'StderrBytes': self.prefix.with_suffix('.stderr').stat().st_size,
                'Scope': 'Linux foreground private epmd/erl process groups; automatic epmd daemon disabled',
            }
            self.prefix.with_suffix('.process.json').write_bytes((json.dumps(receipt, indent=2) + '\n').encode('utf-8'))
