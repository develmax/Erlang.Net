namespace Erlang.Tests;

internal static class HaltLifecycleHostNames
{
    public const string Test = "runtime/halt-foundation-transfer-admission-and-independent-runtime";
    public const string Module = "halt_foundation_host";
    public const string Driver = "halt_foundation_driver";
    public const string Stop = "stop";
    public const string Continuation = "continued";
    public const string Catch = "caught";
    public const string After = "afterwards";
    public const string Run = "run";
    public const string HeldMessage = "halt_held_message";
    public const string TimerMessage = "halt_timer_message";
    public const string TimerStart = "send_after";
    public const int PendingTimerMilliseconds = 60000;
    public const byte PortByte = 37;
    public const string Cast = "$gen_cast";
    public const string Halt = "halt";
    public const string Shutdown = "shutdown";
    public const string Normal = "normal";
    public const string Failure = "Logical halt foundation assertion failed.";
    public const int TransferStatus = 17;
    public const int InitializerStatus = 23;
    public const int SwallowedTransferStatus = 31;
    public const int Contenders = 32;
    public const string Source = "-module(halt_foundation_driver). -export([run/0]). run()->try catch halt_foundation_host:stop(17) of _->halt_foundation_host:continued() catch _:_ ->halt_foundation_host:caught() after halt_foundation_host:afterwards() end,halt_foundation_host:continued().";
    public static readonly TimeSpan Timeout = TimeSpan.FromSeconds(15);
}
