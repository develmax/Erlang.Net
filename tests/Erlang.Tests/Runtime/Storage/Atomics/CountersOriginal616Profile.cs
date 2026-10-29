using System.Reflection;
using System.Text.Json;

namespace Erlang.Tests;

internal static class CountersOriginal616Profile
{
    internal const string DebugPrefix = "Erlang.Tests.CountersOriginal421Debug.";
    private const string DebugProfileFile = "profile.json";
    private const int RequiredSchedulers = 1;
    private const int RequiredOnline = 1;

    internal static async Task<(string Prefix, bool DebugCompiled, int WordSize, int Schedulers, int Online, bool DebugHeader)> Select(
        ProcessContext context
    )
    {
        async Task<Term> Info(string field) => await context.Runtime.Modules.Call(
            context,
            OriginalCounters610Fields.Erlang,
            OriginalCounters610Fields.SystemInfo,
            Term.A(field)
        );
        int wordSize = OriginalCounters610Protocol.Int(await Info(OriginalCounters610Fields.Wordsize));
        int schedulers = OriginalCounters610Protocol.Int(await Info(OriginalCounters610Fields.Schedulers));
        int online = OriginalCounters610Protocol.Int(await Info(OriginalCounters610Fields.SchedulersOnline));
        Term debugValue = await Info(OriginalCounters610Fields.DebugCompiled);
        CountersOriginal421Access.Require(debugValue is Atom { Name: OriginalCounters610Fields.True or OriginalCounters610Fields.False });
        bool debug = ((Atom)debugValue).Name == OriginalCounters610Fields.True;
        Type header = typeof(ProcessRuntime).Assembly.GetType(OriginalCounters610Fields.ErlangCountersMagicHeader421, true)!;
        bool debugHeader = header.GetField(
            OriginalCounters610Fields.CacheLineCount591,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
        ) is not null;
        CountersOriginal421Access.Require(wordSize == OriginalCounters610Names.RequiredWordSize
            && schedulers == RequiredSchedulers && online == RequiredOnline && debug == debugHeader);
        if (debug)
        {
            using var original = JsonDocument.Parse(CountersOriginal421Access.ProfileResource616(DebugProfileFile, DebugPrefix));
            JsonElement profile = original.RootElement;
            CountersOriginal421Access.Require(profile.GetProperty(OriginalCounters610Fields.DebugCompiled).GetBoolean() == debug
                && profile.GetProperty(OriginalCounters610Fields.Wordsize).GetInt32() == wordSize
                && profile.GetProperty(OriginalCounters610Fields.Schedulers).GetInt32() == schedulers
                && profile.GetProperty(OriginalCounters610Fields.SchedulersOnline).GetInt32() == online);
        }

        return (debug ? DebugPrefix : CountersOriginal421Names.Prefix, debug, wordSize, schedulers, online, debugHeader);
    }
}
