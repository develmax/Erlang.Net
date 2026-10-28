using System.Reflection;

namespace Erlang.Tests;

internal static class AtomicsReservation591Names
{
    internal const string ExternalLabel = "external";
    internal const string ReferenceKey = "ref";
    internal const string Put = "put";
    internal const string Get = "get";
    internal const string GateField = "gate";
    internal const string ClosedField = "closed";
    internal const string InvocationField = "invocation";
    internal const string GuestModule = "atomics";
    internal const string NativeType = "Erlang.AtomicsNative413";
    internal const string Capture = "TryCapture";
    internal const string Failure = "Public atomics reservation custody or close-order control failed.";
    internal const string ValueCase = "runtime/atomics591/public-reservation-value-and-dispose-halt";
    internal const string PinCase = "runtime/atomics591/public-reservation-still-joins-active-pin";
    internal const string CloseCase = "runtime/atomics591/retained-public-reservation-does-not-block-vm-close";
    internal const int StoredValue = 53;
    internal const int PollIntervalMilliseconds = 1;
    internal const BindingFlags Members = BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public;
}
