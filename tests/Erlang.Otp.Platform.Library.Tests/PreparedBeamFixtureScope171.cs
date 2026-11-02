using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Erlang.Tests;

internal sealed class PreparedBeamFixtureScope171
{
    private sealed class Entry
    {
        internal PreparedBeamImage? Image;
        internal Entry? Next;
        internal Exception? Failure;
    }

    private Entry? first;
    private Entry? last;

    internal PreparedBeamImage Prepare(Func<PreparedBeamImage> prepare)
    {
        var entry = new Entry();
        PreparedBeamImage image = prepare();
        entry.Image = image;
        if (last is null) first = entry;
        else last.Next = entry;
        last = entry;

        return image;
    }

    internal static async Task RunAsync(ProcessRuntime runtime,
        Func<ModuleRegistry, PreparedBeamFixtureScope171, ValueTask<Action<ModuleRegistry>>> prepare)
    {
        var scope = new PreparedBeamFixtureScope171();
        Exception? primary = null;
        try
        {
            Action<ModuleRegistry> publish = await prepare(runtime.Modules, scope);
            await runtime.InitializeModulesAsync(publish);
        }
        catch (Exception error)
        {
            primary = error;
            throw;
        }
        finally
        {
            await scope.ReleaseAsync(primary);
        }
    }

    private async ValueTask ReleaseAsync(Exception? primary)
    {
        bool failed = false;
        for (Entry? entry = first; entry is not null; entry = entry.Next)
        {
            try
            {
                await entry.Image!.DisposeAsync();
            }
            catch (Exception error)
            {
                entry.Failure = error;
                failed = true;
            }
        }
        if (!failed) return;
        var failures = new List<Exception>();
        if (primary is not null) failures.Add(primary);
        for (Entry? entry = first; entry is not null; entry = entry.Next)
            if (entry.Failure is not null) failures.Add(entry.Failure);
        throw new AggregateException(failures);
    }
}
