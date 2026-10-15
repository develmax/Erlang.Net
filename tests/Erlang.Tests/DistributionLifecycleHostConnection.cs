using Erlang;

namespace Erlang.Tests;

internal sealed record DistributionLifecycleHostConnection(
    Term Host,
    Term Opaque,
    RuntimeNodeFoundationHostActor Controller,
    Pid Remote
);
