namespace Erlang.Tests;

internal static class RuntimeNodeFoundationHostActorParameters
{
    private const int CommandDeadlineSeconds = 10;
    internal static readonly TimeSpan Deadline = TimeSpan.FromSeconds(CommandDeadlineSeconds);
}
