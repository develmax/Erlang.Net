internal static class SignalRawNames
{
    internal const string ByteScript = "import sys;sys.stdout.buffer.write(bytes([0,255,128]));sys.stderr.buffer.write(bytes([254,129]));sys.exit(3)";
    internal const string PipePrefix = "raw-inherited-pipe";
    internal const string ChildPid = "owned-child.pid";
    internal const string PipeScript = "import subprocess,sys,pathlib;child=subprocess.Popen([sys.executable,\"-c\",\"import time;time.sleep(60)\"],stdout=sys.stdout,stderr=sys.stderr,stdin=subprocess.DEVNULL,close_fds=False,creationflags=getattr(subprocess,\"CREATE_NO_WINDOW\",0));pathlib.Path(sys.argv[1]).write_text(str(child.pid));sys.stdout.buffer.write(bytes([0,255]));sys.stderr.buffer.write(bytes([254]));sys.exit(3)";
    internal const string Failure = "Signal raw helper byte/exit contract failed";
    internal const string Prefix = "raw-byte-exit3";
    internal const string PythonCode = "-c";
    internal const int ExitCode = 3;
    internal static readonly byte[] Stdout = [0, 255, 128];
    internal static readonly byte[] Stderr = [254, 129];
}
