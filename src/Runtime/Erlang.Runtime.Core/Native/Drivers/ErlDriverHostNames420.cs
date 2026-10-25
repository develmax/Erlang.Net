namespace Erlang;

internal static class ErlDriverHostNames420
{
    internal const int Version = 1;
    internal const int LengthBytes = sizeof(int);
    internal const int MinimumFrameBytes = 256;
    internal const string InvalidFrame = "Driver host frame is invalid for the admitted session";
    internal const string Closed = "Driver host transport has closed";
    internal const string InvalidPackage = "Driver host package does not match its owning build receipt";
    internal const string ExecutableExtension = ".exe";
    internal const string ParentDirectory = "..";
    internal const string PipePrefix = "erlang-driver-host-";
    internal const string ControlSuffix = "-control";
    internal const string EventsSuffix = "-events";
    internal const string ControlArgument = "--control-pipe";
    internal const string EventsArgument = "--events-pipe";
    internal const string SessionArgument = "--session";
    internal const string MaximumFrameArgument = "--maximum-frame-bytes";
    internal const string ConfigurationFile = "erldrv-host-config.json";
    internal const string ParentArgument = "--parent-pid";
    internal const string UnexpectedExit = "Driver host exited before its owned shutdown completed";
    internal const string WrongPeer = "Driver host pipe belongs to another process";
}
