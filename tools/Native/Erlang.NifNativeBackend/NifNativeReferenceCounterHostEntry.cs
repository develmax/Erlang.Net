using System.Collections.Concurrent;
using System.Reflection;

namespace Erlang.NifNativeBackend;

public static class NifNativeReferenceCounterHostEntry
{
    public static async Task Boundaries()
    {
        await using var runtime = new ProcessRuntime(NifNativeReferenceCounterHostNames.LogicalSchedulers);
        Require(runtime.ReferenceDomainCapacity == NifNativeReferenceCounterHostNames.LogicalSchedulers);
        FieldInfo counter = Field(NifNativeReferenceCounterHostNames.Counter);
        FieldInfo domain = Field(NifNativeReferenceCounterHostNames.Domain);
        foreach (ulong previous in NifNativeReferenceCounterHostNames.Boundaries)
        {
            counter.SetValue(runtime, unchecked((long)previous));
            domain.SetValue(runtime, ReferenceNumberLayout.HostDomain);
            ulong expected = previous + 1;
            ReferenceTerm reference = runtime.MakeReference();
            Require(reference.Words.SequenceEqual(new uint[]
            {
                (uint)(expected & ReferenceIdentityLayout.LocalFirstWordMask),
                (uint)expected & ReferenceNumberLayout.CountMiddleMask,
                (uint)(expected >> ReferenceNumberLayout.CountHighShift)
            }));
            Require(reference.Node == runtime.Node);
        }
        counter.SetValue(runtime, -1L);
        domain.SetValue(runtime, ReferenceNumberLayout.HostDomain);
        ReferenceTerm wrapped = runtime.MakeReference();
        Require(wrapped.Words.SequenceEqual(new uint[] { 0, 1, 0 }));
        ReferenceTerm following = runtime.MakeReference();
        Require(following.Words.SequenceEqual(new uint[] { 1, 1, 0 }));
        Require(!wrapped.Equals(following));
        counter.SetValue(runtime, 0L);
        domain.SetValue(runtime, ReferenceNumberLayout.HostDomain);
        ReferenceTerm initial = runtime.MakeReference();
        counter.SetValue(runtime, -1L);
        domain.SetValue(runtime, (uint)runtime.ReferenceDomainCapacity);
        ReferenceTerm exhaustedDomains = runtime.MakeReference();
        Require(exhaustedDomains.Words.SequenceEqual(new uint[] { 0, 0, 0 }));
        Require(runtime.MakeReference().Equals(initial));
    }

    public static async Task ConcurrentBoundaries()
    {
        await using var runtime = new ProcessRuntime(NifNativeReferenceCounterHostNames.LogicalSchedulers);
        Require(runtime.ReferenceDomainCapacity == NifNativeReferenceCounterHostNames.LogicalSchedulers);
        FieldInfo counter = Field(NifNativeReferenceCounterHostNames.Counter);
        FieldInfo domain = Field(NifNativeReferenceCounterHostNames.Domain);
        ulong[] seeds =
        [
            (ulong)long.MaxValue - NifNativeReferenceCounterHostNames.HalfWindow,
            ulong.MaxValue - NifNativeReferenceCounterHostNames.HalfWindow
        ];
        foreach (ulong seed in seeds)
        {
            counter.SetValue(runtime, unchecked((long)seed));
            domain.SetValue(runtime, ReferenceNumberLayout.HostDomain);
            var actual = new ConcurrentBag<ReferenceTerm>();
            Parallel.For(0, NifNativeReferenceCounterHostNames.ConcurrentCount, _ => actual.Add(runtime.MakeReference()));
            var expected = new HashSet<Term>();
            for (int index = 1; index <= NifNativeReferenceCounterHostNames.ConcurrentCount; index++)
            {
                ulong value = unchecked(seed + (ulong)index);
                uint expectedDomain = value < seed ? 1u : ReferenceNumberLayout.HostDomain;
                expected.Add(new ReferenceTerm(
                    runtime.Node,
                    new uint[]
                {
                    (uint)(value & ReferenceIdentityLayout.LocalFirstWordMask),
                    (uint)value & ReferenceNumberLayout.CountMiddleMask | expectedDomain,
                    (uint)(value >> ReferenceNumberLayout.CountHighShift)
                }
                ));
            }
            Require(actual.Count == NifNativeReferenceCounterHostNames.ConcurrentCount);
            Require(new HashSet<Term>(actual).Count == actual.Count);
            Require(expected.SetEquals(actual));
        }
    }

    private static FieldInfo Field(string name)
        => typeof(ProcessRuntime).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException(NifNativeReferenceCounterHostNames.Failure);

    private static void Require(bool condition)
    {
        if (!condition)
            throw new InvalidOperationException(NifNativeReferenceCounterHostNames.Failure);
    }
}
