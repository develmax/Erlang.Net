namespace Erlang;

internal static class DescriptorPortParameters
{
    public const short PollInput = 1;
    public const short PollOutput = 4;
    public const short PollInvalid = 32;
    public const int PollIntervalMilliseconds = 1;
    public const int WindowsInput = -10;
    public const int WindowsOutput = -11;
    public const int WindowsBrokenPipe = 109;
    public const int WindowsOperationAborted = 995;
    public const uint ThreadCancelAccess = 1;
    public const long MaximumSmallInteger = (1L << 59) - 1;
    public const long MinimumSmallInteger = -(1L << 59);
}
