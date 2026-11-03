using System.Reflection;
using System.Runtime.InteropServices;
using static Erlang.Tests.NativeResource524Access;
using static Erlang.Tests.RegexCustody597Names;

namespace Erlang.Tests;

internal static class RegexCustody597Checks
{
    internal static Task Physical() => RegexCustody597Fixture.Run(async fixture =>
    {
        object frame = fixture.Frame();
        int first = (int)Call(frame, Reserve)!, second = (int)Call(frame, Reserve)!;
        Call(
            frame,
            Set,
            first,
            fixture.Reference
        );
        Call(
            frame,
            Set,
            second,
            fixture.Reference
        );
        var a = (ReferenceTerm)Call(frame, Read, first)!;
        var b = (ReferenceTerm)Call(frame, Read, second)!;
        object? firstBlock = a.GetType().GetProperty(Storage, Members)!.GetValue(a);
        object? secondBlock = b.GetType().GetProperty(Storage, Members)!.GetValue(b);
        Require(firstBlock is not null && secondBlock is not null && !ReferenceEquals(firstBlock, secondBlock));
        nint address = AddressOf(fixture.Heap, firstBlock!);
        nint next = AddressOf(fixture.Heap, secondBlock!);
        nint binary = (nint)Marshal.ReadInt64(address, IntPtr.Size);
        Require(Marshal.ReadInt64(address) == Header && (nint)Marshal.ReadInt64(next, 2 * IntPtr.Size) == address);
        Require(Marshal.ReadInt64(next, IntPtr.Size) == binary && Marshal.ReadInt64(binary, CodeOffset) != 0);
        Require(Marshal.ReadInt64(binary, PrefixRefCountOffset) == 2);
        long savedRefs597 = Marshal.ReadInt64(binary, PrefixRefCountOffset);
        fixture.Restore(() =>
 {
     if (fixture.Backing is { } live && !(bool)Property(live, Freed)) Marshal.WriteInt64(binary, PrefixRefCountOffset, savedRefs597);
 });
        Marshal.WriteInt64(binary, PrefixRefCountOffset, savedRefs597 + 1);
        Expect<InvalidOperationException>(() => _ = a.Words[0]);
        Marshal.WriteInt64(binary, PrefixRefCountOffset, savedRefs597);
        uint old = a.Words[0];
        int saved = Marshal.ReadInt32(binary, Ref0Offset);
        fixture.Restore(() =>
 {
     if (fixture.Backing is { } backing && !(bool)Property(backing, Freed)) Marshal.WriteInt32(binary, Ref0Offset, saved);
 });
        Marshal.WriteInt32(binary, Ref0Offset, saved ^ 1);
        Require(a.Words[0] == (old ^ 1) && b.Words[0] == (old ^ 1));
        Marshal.WriteInt32(binary, Ref0Offset, saved);
        fixture.Release();
        Call(frame, Clear, first);
        Call(fixture.Heap, Collect);
        Require(!(bool)Property(fixture.Backing!, Freed));
        Call(frame, Clear, second);
        Call(fixture.Heap, Collect);
        await ((Task)Property(fixture.Backing!, Retired)).WaitAsync(Deadline);
        Require((bool)Property(fixture.Backing!, Freed));
        GC.KeepAlive(a);
        GC.KeepAlive(b);
        GC.KeepAlive(fixture.Reference);
    });

    internal static Task Generations() => RegexCustody597Fixture.Run(async fixture =>
    {
        object frame = fixture.Frame();
        int slot = (int)Call(frame, Reserve)!;
        Call(
            frame,
            Set,
            slot,
            fixture.Reference
        );
        var value = (ReferenceTerm)Call(frame, Read, slot)!;
        Term pattern = Term.Tuple(
            Term.A(RegexNative411Names.Compiled),
            Term.I(0),
            Term.I(0),
            Term.I(0),
            value
        );
        await fixture.Actor!.InvokeAsync(async context =>
        {
            Term result = await fixture.Engine!.RunAsync(
                context,
                RegexNative411Names.Bytes(Word),
                pattern,
                Nil.Value
            );
            Require(result.Equals(RegexNative411Access.Match(0, 1)));

            return true;
        });
        await fixture.Actor.InvokeAsync(async context =>
        {
            bool refused = false;
            try
            {
                await fixture.Engine!.DisposeAsync();
            }
            catch (InvalidOperationException)
            {
                refused = true;
            }
            Require(refused && !fixture.Engine!.Report.Closed);

            return true;
        });
        fixture.Release();
        Call(
            fixture.Heap,
            CollectGeneration,
            false,
            ushort.MaxValue
        );
        Call(
            fixture.Heap,
            CollectGeneration,
            false,
            ushort.MaxValue
        );
        Call(frame, Clear, slot);
        Call(
            fixture.Heap,
            CollectGeneration,
            false,
            ushort.MaxValue
        );
        Require(!(bool)Property(fixture.Backing!, Freed));
        Call(
            fixture.Heap,
            CollectGeneration,
            true,
            ushort.MaxValue
        );
        await ((Task)Property(fixture.Backing!, Retired)).WaitAsync(Deadline);
        await using var foreign = OwnedGuest506RuntimeFactory.Create();
        MethodInfo direct = typeof(ProcessRuntime).Assembly.GetType(RegexRoots, true)!.GetMethod(Direct, BindingFlags.Static | BindingFlags.NonPublic)!;
        Expect<NotSupportedException>(() => direct.Invoke(null, [value, foreign]));
        Expect<ObjectDisposedException>(() => direct.Invoke(null, [value, fixture.Runtime]));
    });

    internal static Task CompileImport() => RegexCustody597Fixture.Run(
        async fixture =>
    {
        Term input = ExternalTermFormat.Decode(RegexThreeOriginal413Access.Read(RegexThreeOriginal413Names.ExportInputs));
        Require(input is TupleTerm { Items.Count: 2 });
        var exported = ((TupleTerm)input).Items[0];
        Term compiled = await fixture.Actor!.InvokeAsync(async context =>
            RegexNative411Access.Pattern(await fixture.Engine!.CompileAsync(context, RegexNative411Names.Bytes(Word), Nil.Value)));
        Term imported = await fixture.Actor.InvokeAsync(async context =>
            await RegexNative411Access.Call(context, Import, exported));
        var first = (ResourceReferenceTerm)((TupleTerm)compiled).Items[4];
        var second = (ResourceReferenceTerm)((TupleTerm)imported).Items[4];
        object? firstBacking = first.GetType().GetProperty(RegexBacking, Members)?.GetValue(first);
        object? secondBacking = second.GetType().GetProperty(RegexBacking, Members)?.GetValue(second);
        Require(firstBacking is not null && secondBacking is not null && !ReferenceEquals(firstBacking, secondBacking));
        await fixture.Actor.InvokeAsync(async context =>
        {
            Require((await fixture.Engine!.RunAsync(
                context,
                RegexNative411Names.Bytes(Word),
                compiled,
                Nil.Value
            )).Equals(RegexNative411Access.Match(0, 1)));
            Term named = await RegexNative411Access.Call(
                context,
                Inspect,
                imported,
                Term.A(NameList)
            );
            Require(named is TupleTerm { Items.Count: 2 });

            return true;
        });
        fixture.Release();
        await fixture.Actor.DisposeAsync().AsTask().WaitAsync(Deadline);
        await fixture.Runtime.DisposeAsync().AsTask().WaitAsync(Deadline);
        Require((bool)Property(firstBacking!, Freed) && (bool)Property(secondBacking!, Freed));
        RegexNative411Access.Empty(fixture.Engine!);
        GC.KeepAlive(compiled);
        GC.KeepAlive(imported);
    },
        originalRe: true
    );

    private static nint AddressOf(object heap, object block)
    {
        lock (Property(heap, Gate)) return (nint)Property(Call(
            heap,
            CaptureAddress,
            block,
            0,
            Words
        )!, Address);
    }

    private static void Require(bool value)
    {
        if (!value) throw new InvalidOperationException(Failure);
    }

    private static void Expect<T>(Action body) where T : Exception
    {
        try
        {
            body();
        }
        catch (TargetInvocationException error) when (error.InnerException is T)
        {
            return;
        }
        catch (T)
        {
            return;
        }
        throw new InvalidOperationException(Failure);
    }
}
