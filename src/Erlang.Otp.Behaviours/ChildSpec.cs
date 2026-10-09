using System.Diagnostics;

namespace Erlang.Otp;

public sealed record ChildSpec(Term Id, Func<ProcessContext, ValueTask<ProcessHandle>> Start, RestartPolicy Restart = RestartPolicy.Permanent, TimeSpan? Shutdown = null);
