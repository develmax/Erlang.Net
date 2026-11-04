namespace Erlang;

internal static class LinuxDescriptorPollNames
{
    internal const string ThreadName = "Erlang descriptor poll 0";
    internal const string PollType = "poll";
    internal const string Closed = "Linux descriptor poll owner has stopped";
    internal const string NotReady = "Actual Linux descriptor poll thread is not ready";
    internal const string DuplicateStart = "Linux descriptor poll thread may start only once";
    internal const string SelfJoin = "Poll thread cannot synchronously retire its own native lifetime";
    internal const string ForeignOwner = "Descriptor poll registration belongs to another owner";
    internal const int PollIdentity = 0;
    internal const int WakePollIndex = 0;
    internal const int PipeReadIndex = 0;
    internal const int PipeWriteIndex = 1;
    internal const int WakeSignalBytes = 1;
    internal const byte WakeSignalValue = 1;
    internal const int FirstRequestIndex = 1;
    internal const int InfiniteTimeout = -1;
    internal const int InvalidDescriptor = -1;
    internal const int WakeBufferBytes = 256;
    internal const short Error = 8;
    internal const short Hangup = 16;
    internal const short AllowedEvents = DescriptorPortParameters.PollInput | DescriptorPortParameters.PollOutput;
}
