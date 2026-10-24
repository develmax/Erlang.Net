using System.Reflection;

namespace Erlang.Tests;

internal static class MessageQueuePolicy411Names
{
    internal const string FlagsTest = "HOST/message-queue411/flags-and-queued-message-preservation";
    internal const string SpawnTest = "HOST/message-queue411/actual-child-first-instruction-and-request";
    internal const string InvalidTest = "HOST/message-queue411/invalid-immediate-and-remote-parser";
    internal const string Flag = "message_queue_data";
    internal const string On = "on_heap";
    internal const string Off = "off_heap";
    internal const string Invalid = "invalid";
    internal const string Info = "process_info";
    internal const string SystemInfo = "system_info";
    internal const string Spawn = "spawn_opt";
    internal const string Request = "spawn_request";
    internal const string Monitor = "monitor";
    internal const string Reply = "spawn_reply";
    internal const string Success = "ok";
    internal const string Down = "DOWN";
    internal const string First = "first";
    internal const string Second = "second";
    internal const string Error = "Logical mailbox policy contract HOST failed";
    internal const string OptionsType = "Erlang.ProcessSpawnOptions";
    internal const string ParseRemote = "ParseRemote";
    internal const string ModeProperty = "MessageQueueData";
    internal const string OffEnum = "OffHeap";
    internal const string ErrorInfo = "error_info";
    internal const BindingFlags Members = BindingFlags.Static | BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
    internal static readonly TimeSpan Deadline = TimeSpan.FromSeconds(10);
}
