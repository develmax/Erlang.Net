namespace Erlang.Tests;

internal static class MessageQueuePolicy411Access
{
    internal static string RemoteMode(Term options)
    {
        Type parser = typeof(ProcessRuntime).Assembly.GetType(MessageQueuePolicy411Names.OptionsType)!;
        object remote = parser.GetMethod(MessageQueuePolicy411Names.ParseRemote, MessageQueuePolicy411Names.Members)!.Invoke(null, [options])!;

        return parser.GetProperty(MessageQueuePolicy411Names.ModeProperty, MessageQueuePolicy411Names.Members)!.GetValue(remote)!.ToString()!;
    }
}
