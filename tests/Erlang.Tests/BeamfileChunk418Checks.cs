using System.Buffers.Binary;

namespace Erlang.Tests;

internal static class BeamfileChunk418Checks
{
    internal static async Task Valid()
    {
        await using var runtime = new ProcessRuntime(1);
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        ErlangFunction body = BeamfileChunk418Access.Capture(runtime);
        await actor.InvokeAsync(async context =>
        {
            byte[] data = Enumerable.Range(0, 256).Select(value => (byte)value).ToArray();
            byte[] good = BeamfileChunk418Access.Form(BeamfileChunk418Access.Chunk(BeamfileChunk418Names.DataId, data));
            foreach (BitString input in new[] { new BitString(good), BeamfileChunk418Access.Unaligned(good) })
                BeamfileChunk418Access.Require((await body(context, [input, BeamfileChunk418Access.Identifier])).Equals(new BitString(data)));
            byte[] duplicate = BeamfileChunk418Access.Form(
                BeamfileChunk418Access.Chunk(BeamfileChunk418Names.DataId, [1]),
                BeamfileChunk418Access.Chunk(BeamfileChunk418Names.DataId, [2, 3])
            );
            BeamfileChunk418Access.Require((await body(context, [new BitString(duplicate.Concat(new byte[] { 255 }).ToArray()), BeamfileChunk418Access.Identifier]))
                .Equals(new BitString(new byte[] { 2, 3 })));
            byte[] high = BeamfileChunk418Access.Form(BeamfileChunk418Access.Chunk(0x80000001, [7]));
            Term highExpected = IntPtr.Size == 8 ? Term.A(BeamfileChunk418Names.Undefined) : new BitString(new byte[] { 7 });
            BeamfileChunk418Access.Require((await body(context, [new BitString(high), Term.List(
                Term.I(128),
                Term.I(0),
                Term.I(0),
                Term.I(1)
            )])).Equals(highExpected));
            byte[] positive = BeamfileChunk418Access.Form(BeamfileChunk418Access.Chunk(0x7FFFFFFF, [9]));
            BeamfileChunk418Access.Require((await body(context, [new BitString(positive), Term.List(
                Term.I(127),
                Term.I(255),
                Term.I(255),
                Term.I(255)
            )])).Equals(new BitString(new byte[] { 9 })));
            byte[] allHigh = BeamfileChunk418Access.Form(BeamfileChunk418Access.Chunk(0xFFFFFFFF, [8]));
            Term allHighExpected = IntPtr.Size == 8 ? Term.A(BeamfileChunk418Names.Undefined) : new BitString(new byte[] { 8 });
            BeamfileChunk418Access.Require((await body(context, [new BitString(allHigh), Term.List(
                Term.I(255),
                Term.I(255),
                Term.I(255),
                Term.I(255)
            )])).Equals(allHighExpected));
            byte[] emptyLast = BeamfileChunk418Access.Form(
                BeamfileChunk418Access.Chunk(BeamfileChunk418Names.DataId, [1]),
                BeamfileChunk418Access.Chunk(BeamfileChunk418Names.DataId, [])
            );
            BeamfileChunk418Access.Require((await body(context, [new BitString(emptyLast), BeamfileChunk418Access.Identifier])).Equals(Term.A(BeamfileChunk418Names.Undefined)));
            byte[] padded = BeamfileChunk418Access.Chunk(BeamfileChunk418Names.DataId, [7]);
            padded[^1] = 255;
            BeamfileChunk418Access.Require((await body(context, [new BitString(BeamfileChunk418Access.Form(padded)), BeamfileChunk418Access.Identifier])).Equals(new BitString(new byte[] { 7 })));

            return true;
        });
    }

    internal static async Task Malformed()
    {
        await using var runtime = new ProcessRuntime(1);
        ErlangFunction body = BeamfileChunk418Access.Capture(runtime);
        await runtime.InitializeModulesAsync(registry => registry.InstallPreloaded(target =>
            target.Register(
                BeamfileChunk418Names.Module,
                BeamfileChunk418Names.Function,
                BeamfileChunk418Names.Arity,
                body
            )));
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            byte[] chunk = BeamfileChunk418Access.Chunk(BeamfileChunk418Names.DataId, [5]);
            byte[] negative = chunk.ToArray();
            BinaryPrimitives.WriteInt32BigEndian(negative.AsSpan(BeamfileChunk418Names.WordBytes), -1);
            byte[] huge = chunk.ToArray();
            BinaryPrimitives.WriteInt32BigEndian(huge.AsSpan(BeamfileChunk418Names.WordBytes), int.MaxValue);
            foreach (byte[] input in new[] {
                Array.Empty<byte>(), BeamfileChunk418Access.Form(chunk, [1]),
                BeamfileChunk418Access.Form(negative), BeamfileChunk418Access.Form(huge),
                BeamfileChunk418Access.Form(chunk[..^1]) })
                BeamfileChunk418Access.Require((await body(context, [new BitString(input), BeamfileChunk418Access.Identifier])).Equals(Term.A(BeamfileChunk418Names.Undefined)));
            foreach (var arguments in new Term[][] {
                [Term.I(1), BeamfileChunk418Access.Identifier],
                [new BitString([128], 1), BeamfileChunk418Access.Identifier],
                [new BitString(chunk), Term.List(Term.I(68), Term.I(65), Term.I(84))],
                [new BitString(chunk), new Cons(Term.I(68), Term.A(BeamfileChunk418Names.Identifier))],
                [Term.I(1), Term.A(BeamfileChunk418Names.Identifier)] })
            {
                bool failed = false;
                try
                {
                    _ = await runtime.Modules.Call(
                        context,
                        BeamfileChunk418Names.Module,
                        BeamfileChunk418Names.Function,
                        arguments
                    );
                }
                catch (ErlangException error)
                {
                    failed = error.Reason.Equals(Term.A(BeamfileChunk418Names.BadArgument))
                        && error.StackTraceTerm is Cons { Head: TupleTerm frame }
                        && frame.Items[0].Equals(Term.A(BeamfileChunk418Names.Module))
                        && frame.Items[1].Equals(Term.A(BeamfileChunk418Names.Function))
                        && frame.Items[2].Equals(Cons.From(arguments));
                }
                BeamfileChunk418Access.Require(failed);
            }

            return true;
        });
    }

    internal static async Task Owner()
    {
        var runtime = new ProcessRuntime(1);
        await using var foreign = new ProcessRuntime(1);
        RuntimeNodeFoundationHostActor? actor = null;
        try
        {
            ErlangFunction body = BeamfileChunk418Access.Capture(runtime);
            await using (var other = new RuntimeNodeFoundationHostActor(foreign))
                await other.InvokeAsync(async context =>
                {
                    bool failed = false;
                    try
                    {
                        _ = await body(context, [new BitString([]), BeamfileChunk418Access.Identifier]);
                    }
                    catch (InvalidOperationException)
                    {
                        failed = true;
                    }
                    BeamfileChunk418Access.Require(failed);

                    return true;
                });
            actor = new(runtime);
            ProcessContext saved = await actor.Ready;
            await actor.InvokeAsync(async context =>
            {
                ((CancellationTokenSource)typeof(ProcessContext).GetProperty(PersistentArena416HostNames.Stop,
                    PersistentArena416HostNames.Members)!.GetValue(context)!).Cancel();
                bool failed = false;
                try
                {
                    _ = await body(context, [new BitString([]), BeamfileChunk418Access.Identifier]);
                }
                catch (OperationCanceledException)
                {
                    failed = true;
                }
                BeamfileChunk418Access.Require(failed);

                return true;
            });
            await runtime.DisposeAsync();
            bool closed = false;
            try
            {
                _ = await body(saved, [new BitString([]), BeamfileChunk418Access.Identifier]);
            }
            catch (ObjectDisposedException)
            {
                closed = true;
            }
            BeamfileChunk418Access.Require(closed);
        }
        finally
        {
            try
            {
                if (actor is not null) await actor.DisposeAsync();
            }
            finally
            {
                await runtime.DisposeAsync();
            }
        }
    }
}
