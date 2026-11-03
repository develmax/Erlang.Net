using System.Numerics;
using System.Reflection;
using System.Runtime.ExceptionServices;
using static Erlang.Tests.BeamLoaderMapKeys565Names;

namespace Erlang.Tests;

// These are finite loader/storage seam controls. Fresh original BEAM execution
// remains a separate required observation; reflection is not admission proof.
internal static class BeamLoaderMapKeys565Checks
{
    private const BindingFlags Members = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

    internal static Task SyntacticPredicateAndImages()
    {
        object first = Code();
        var templates = (IReadOnlyDictionary<int, TupleTerm>)Get(first, Templates);
        Check(templates.Count == 2 && templates.ContainsKey(0) && templates.ContainsKey(1));
        Check(ReferenceEquals(templates[0], templates[1]));
        Check(ReferenceEquals(templates, Get(first, Templates)));
        var other = (IReadOnlyDictionary<int, TupleTerm>)Get(Code(), Templates);
        Check(templates[0].Equals(other[0]) && !ReferenceEquals(templates[0], other[0]));
        var structure = Get(first, Structure);
        var literals = (IReadOnlyList<Term>)Get(structure, Literals);
        Check(templates[0].Equals(literals[2]) && !ReferenceEquals(templates[0], literals[2]));

        return Task.CompletedTask;
    }

    internal static async Task PhysicalCustodyAndExport()
    {
        ProcessRuntime runtime = OwnedGuest506RuntimeFactory.Create();
        object? heap = null;
        IDisposable? roots = null;
        var errors = new List<Exception>();
        try
        {
            object code = Code();
            Call(runtime, Register, code);
            var templates = (IReadOnlyDictionary<int, TupleTerm>)Get(code, Templates);
            TupleTerm keys = templates[0];
            Check((bool)Call(runtime, IsLiteral, keys)!);
            Check(!(bool)Call(runtime, IsLiteral, new TupleTerm(keys.Items.ToArray()))!);
            heap = New(HeapType, runtime);
            roots = (IDisposable)Call(heap, Enter)!;
            int slot = (int)Call(roots, Reserve)!;
            var entries = keys.Items.Select((key, index) => new KeyValuePair<Term, Term>(key, Term.I(index + 1))).ToArray();
            var source = (MapTerm)Activator.CreateInstance(
                typeof(MapTerm),
                Members,
                null,
                [entries, keys],
                null
            )!;
            var actual = (MapTerm)Call(
                roots,
                Adopt,
                slot,
                source
            )!;
            Call(heap, Collect);
            Check(ReferenceEquals(Get(actual, FlatKeys), keys) && Property(keys, Storage) is null);
            // Map header, arity, key-tuple reference, and two actual values.
            // There is no process-owned mirror of the loader's tuple.
            Check(PhysicalWords(heap) == 5 && actual.Equals(source));
            var exported = (MapTerm)Call(
                heap,
                Export,
                roots,
                actual
            )!;
            Check(exported.Equals(source));
            Call(roots, Clear, slot);
            Call(heap, Collect);
            Check(PhysicalWords(heap) == 0);
            var dynamicSource = new MapTerm(entries);
            var dynamicActual = (MapTerm)Call(
                roots,
                Adopt,
                slot,
                dynamicSource
            )!;
            Call(heap, Collect);
            Check(!ReferenceEquals(Get(dynamicActual, FlatKeys), keys) && PhysicalWords(heap) == 8);
            Call(roots, Clear, slot);
            Call(heap, Collect);
            Check(PhysicalWords(heap) == 0);
            roots.Dispose();
            await ((Task)Call(heap, Retire)!).WaitAsync(TimeSpan.FromSeconds(DrainSeconds));
            Check(exported.Equals(source) && keys.Items.SequenceEqual(entries.Select(pair => pair.Key)));
        }
        catch (Exception error)
        {
            errors.Add(error);
        }
        finally
        {
            try
            {
                roots?.Dispose();
            }
            catch (Exception error)
            {
                errors.Add(error);
            }
            if (heap is not null)
            {
                try
                {
                    await ((Task)Call(heap, Retire)!).WaitAsync(TimeSpan.FromSeconds(DrainSeconds));
                }
                catch (Exception error)
                {
                    errors.Add(error);
                }
            }
            try
            {
                await runtime.DisposeAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(DrainSeconds));
            }
            catch (Exception error)
            {
                errors.Add(error);
            }
        }
        if (errors.Count == 1)
            ExceptionDispatchInfo.Capture(errors[0]).Throw();
        if (errors.Count > 1)
            throw new AggregateException(CleanupFailure, errors);
    }

    internal static async Task HiddenKeyCustodyRefusals()
    {
        ProcessRuntime firstRuntime = OwnedGuest506RuntimeFactory.Create();
        ProcessRuntime secondRuntime = OwnedGuest506RuntimeFactory.Create();
        object? firstHeap = null, secondHeap = null;
        IDisposable? firstRoots = null, secondRoots = null, forkRoots = null;
        var errors = new List<Exception>();
        try
        {
            firstHeap = New(HeapType, firstRuntime);
            secondHeap = New(HeapType, secondRuntime);
            firstRoots = (IDisposable)Call(firstHeap, Enter)!;
            secondRoots = (IDisposable)Call(secondHeap, Enter)!;
            int firstSlot = (int)Call(firstRoots, Reserve)!;
            int secondSlot = (int)Call(secondRoots, Reserve)!;
            Term[] keys = [Term.A(First), Term.A(Second)];
            var foreignKeys = (TupleTerm)Call(
                secondRoots,
                CreateTuple,
                secondSlot,
                keys
            )!;
            var entries = keys.Select(key => new KeyValuePair<Term, Term>(key, Term.I(1))).ToArray();
            var source = (MapTerm)Activator.CreateInstance(
                typeof(MapTerm),
                Members,
                null,
                [entries, foreignKeys],
                null
            )!;
            Check(ReferenceEquals(Get(source, FlatKeys), foreignKeys));
            Reject(() => Call(
                firstRoots,
                Adopt,
                firstSlot,
                source
            ), ForeignFailure);
            Check(PhysicalWords(firstHeap) == 0);
            Call(secondRoots, Clear, secondSlot);
            Call(secondHeap, Collect);
            Check(PhysicalWords(secondHeap) == 0);
            Reject(() => Call(
                firstRoots,
                Adopt,
                firstSlot,
                source
            ), ForeignFailure);
            Check(PhysicalWords(firstHeap) == 0);

            object code = Code();
            Call(firstRuntime, Register, code);
            var templates = (IReadOnlyDictionary<int, TupleTerm>)Get(code, Templates);
            TupleTerm literalKeys = templates[0];
            Check((bool)Call(firstRuntime, IsLiteral, literalKeys)!);
            object custody = Call(firstRoots, BeginConstruction, Term.I(0))!;
            forkRoots = (IDisposable)Call(firstHeap, Enter)!;
            Reject(() => Call(
                forkRoots,
                BeginFork,
                custody,
                new Term[] { literalKeys }
            ), UnadmittedFailure);
            Check(PhysicalWords(firstHeap) == 0);
        }
        catch (Exception error)
        {
            errors.Add(error);
        }
        finally
        {
            foreach (IDisposable? roots in new[] { forkRoots, firstRoots, secondRoots })
            {
                try
                {
                    roots?.Dispose();
                }
                catch (Exception error)
                {
                    errors.Add(error);
                }
            }
            foreach (object? heap in new[] { firstHeap, secondHeap })
            {
                if (heap is null)
                    continue;
                try
                {
                    await ((Task)Call(heap, Retire)!).WaitAsync(TimeSpan.FromSeconds(DrainSeconds));
                }
                catch (Exception error)
                {
                    errors.Add(error);
                }
            }
            foreach (ProcessRuntime runtime in new[] { firstRuntime, secondRuntime })
            {
                try
                {
                    await runtime.DisposeAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(DrainSeconds));
                }
                catch (Exception error)
                {
                    errors.Add(error);
                }
            }
        }
        if (errors.Count == 1)
            ExceptionDispatchInfo.Capture(errors[0]).Throw();
        if (errors.Count > 1)
            throw new AggregateException(CleanupFailure, errors);
    }

    private static void Reject(Action action, string expected)
    {
        try
        {
            action();
        }
        catch (InvalidOperationException error) when (error.Message == expected)
        {
            return;
        }

        throw new InvalidOperationException(Failure);
    }

    private static object Code()
    {
        object source = Operand(LiteralKind, 0);
        object[] literalKeys = [Operand(AtomKind, 2), Operand(AtomKind, 3)];
        object[] largeKeys = Enumerable.Range(0, 33).Select(index => Operand(IntegerKind, index)).ToArray();
        object[] instructions =
        [
            Instruction(
                0,
                Associate,
                source,
                literalKeys
            ),
            Instruction(
                1,
                Associate,
                source,
                literalKeys
            ),
            // A source register may hold #{} at runtime. It remains dynamic.
            Instruction(
                2,
                Associate,
                Operand(RegisterKind, 0),
                literalKeys
            ),
            // A runtime key equal to a literal is still a register operand.
            Instruction(
                3,
                Associate,
                source,
                [Operand(RegisterKind, 1)]
            ),
            Instruction(
                4,
                Associate,
                source,
                largeKeys
            ),
            Instruction(
                5,
                Exact,
                source,
                literalKeys
            ),
            Instruction(
                6,
                Associate,
                Operand(LiteralKind, 1),
                literalKeys
            )
        ];
        var nonempty = new MapTerm(new KeyValuePair<Term, Term>[] { new(Term.A(First), Term.I(1)) });
        Term[] literals = [new MapTerm(Array.Empty<KeyValuePair<Term, Term>>()), nonempty, Term.Tuple(Term.A(First), Term.A(Second))];
        object structure = New(
            StructureType,
            Empty(ChunkType),
            new string?[] { null, ModuleName, First, Second },
            Empty(ImportType),
            Empty(ExportType),
            New(
                HeaderType,
                0,
                255,
                0,
                0,
                ReadOnlyMemory<byte>.Empty
            ),
            literals,
            0
        );

        return New(
            CodeType,
            structure,
            New(
                TypesType,
                true,
                null,
                Empty(TypeEntryType)
            ),
            Typed(InstructionType, instructions),
            Empty(FunctionType),
            new Dictionary<int, int>(),
            Empty(FunctionType),
            null,
            Empty(LambdaType)
        );
    }

    private static object Instruction(
        int offset,
        string opcode,
        object source,
        object[] keys
    )
    {
        var operands = new List<object>
        {
            Operand(FailureKind, 0), source, Operand(RegisterKind, 0), Operand(UnsignedKind, 1), Operand(CountKind, keys.Length * 2)
        };
        foreach (object key in keys)
        {
            operands.Add(key);
            operands.Add(Operand(RegisterKind, 0));
        }

        return New(
            InstructionType,
            offset,
            New(
                OpcodeType,
                (byte)0,
                opcode,
                5,
                false
            ),
            Typed(OperandType, operands)
        );
    }

    private static object Operand(string kind, int value) => New(
        OperandType,
        kind,
        new BigInteger(value),
        null
    );

    private static Array Empty(string type) => Array.CreateInstance(Type(type), 0);

    private static Array Typed(string type, IEnumerable<object> source)
    {
        object[] values = source.ToArray();
        Array result = Array.CreateInstance(Type(type), values.Length);
        for (int index = 0; index < values.Length; index++)
            result.SetValue(values[index], index);

        return result;
    }

    private static Type Type(string name) => typeof(ProcessRuntime).Assembly.GetType(Prefix + name, true)!;

    private static object New(string type, params object?[] arguments) => Activator.CreateInstance(
        Type(type),
        Members,
        null,
        arguments,
        null
    )!;

    private static object Get(object target, string property) => Property(target, property)!;

    private static object? Property(object target, string property) => target.GetType().GetProperty(property, Members)!.GetValue(target);

    private static long PhysicalWords(object heap) => (long)Get(Get(heap, Snapshot), Words);

    private static object? Call(object target, string name, params object?[] arguments)
    {
        try
        {
            return target.GetType().GetMethod(name, Members)!.Invoke(target, arguments);
        }
        catch (TargetInvocationException error) when (error.InnerException is { } inner)
        {
            ExceptionDispatchInfo.Capture(inner).Throw();
            throw;
        }
    }

    private static void Check(bool condition)
    {
        if (!condition)
            throw new InvalidOperationException(Failure);
    }
}
