using System.Collections;
using System.Reflection;
using System.Numerics;
using System.Runtime.ExceptionServices;
using System.Text.Json;
using Erlang.Compiler;
using static Erlang.Tests.RegisterCapacity273Names;

namespace Erlang.Tests;

internal static class RegisterCapacity273Checks
{
    private static readonly Type State = typeof(ProcessRuntime).Assembly.GetType(StateType, true)!;
    private static readonly Type Capacity = typeof(ProcessRuntime).Assembly.GetType(CapacityType, true)!;

    internal static async Task Run()
    {
        string directory = Environment.GetEnvironmentVariable(FixtureEnvironment)
            ?? throw new InvalidOperationException(FixtureEnvironment);
        if (!Path.IsPathFullyQualified(directory)) throw new ArgumentException(FixtureEnvironment);
        string[] modules = [Observer, Wide, Boundary, Live];
        byte[][] bytes = modules.Select(name => File.ReadAllBytes(Path.Combine(directory, name + ".beam"))).ToArray();
        Term expected = ExternalTermFormat.Decode(File.ReadAllBytes(Path.Combine(directory, Expected)));
        using (var ordinary = (IDisposable)Activator.CreateInstance(
            State,
            Members,
            null,
            [],
            null
        )!)
        {
            Require(Values(ordinary).Length == Full);
            Write(ordinary, Full - 1, Term.I(273));
            Require(Read(ordinary, Full - 1).Equals(Term.I(273)));
        }
        foreach (bool owned in new[] { false, true })
        {
            await using var runtime = OwnedGuest506RuntimeFactory.Create(owned);
            PreparedBeamImage[]? prepared = null;
            await PreparedBeamFixtureScope171.RunAsync(
                runtime,
                (registry, scope) =>
            {
                var dependencies = new BeamDependencySelection(modules.Append("erlang"));
                prepared = bytes.Select(image => scope.Prepare(() => registry.PrepareBeam(image, dependencies))).ToArray();

                return ValueTask.FromResult<Action<ModuleRegistry>>(target =>
                    Require(target.PublishPreparedBeam(prepared).Status == BeamPublicationStatus.Published));
            }
            );
            await using var actor = new RuntimeNodeFoundationHostActor(runtime);
            await actor.InvokeAsync(async context =>
            {
                _ = await Execution.EvaluateAsync(new Expr.Literal(Nil.Value), context);
                CheckState(context);
                using var host = new NativeTermHostScope524(runtime);
                Term first = await runtime.Modules.Call(context, Observer, RunFunction);
                Term wide = await runtime.Modules.Call(context, Wide, RunFunction);
                Term boundary = await runtime.Modules.Call(context, Boundary, RunFunction);
                Term live = await runtime.Modules.Call(
                    context,
                    Live,
                    RunFunction,
                    Enumerable.Range(0, Small).Select(value => (Term)Term.I(value)).ToArray()
                );
                Term actual = Term.Tuple(
                    first,
                    wide,
                    boundary,
                    live
                );
                Require(actual.Equals(expected));
                var capacities = new List<object>();
                foreach (PreparedBeamImage image in prepared!)
                {
                    object code = Get(image, "Code");
                    object entry = ((IEnumerable)Get(code, "Functions")).Cast<object>()
                        .Single(function => (string)Get(function, "Name") == RunFunction);
                    int count = Bound(
                        code,
                        entry,
                        0,
                        0
                    );
                    Require(count >= 4 && count <= Full);
                    if (image.Name == Boundary) Require(count == Full - 1 && MaximumX(code) == Full - 1);
                    if (image.Name == Wide) Require(count >= Small && MaximumX(code) == Small);
                    if (image.Name == Live) Require(count >= Small);
                    if (image.Name == Observer)
                    {
                        string[] opcodes = ((IEnumerable)Get(code, "Instructions")).Cast<object>()
                            .Select(instruction => (string)Get(Get(instruction, "Definition"), "Name")).ToArray();
                        Require(opcodes.Any(opcode => opcode is "apply" or "apply_last")
                            && opcodes.Any(opcode => opcode is "call_fun" or "call_fun2")
                            && ((IEnumerable)Get(code, "Lambdas")).Cast<object>().Any(lambda => (int)Get(lambda, "FreeVariables") > 0));
                    }
                    else Require(count < Full);
                    // The memberwise clone shares all IR values but has no
                    // reference-keyed preparation certificate or sizing grant.
                    object clone = code.GetType().GetMethod("MemberwiseClone", Members)!.Invoke(code, null)!;
                    Require(Bound(
                        clone,
                        entry,
                        0,
                        0
                    ) == Full);
                    CheckUntrustedFootprintFallbacks(code);
                    if (image.Name == Observer) CheckRoleAwareSizing475(code, entry);
                    Require(Bound(
                        code,
                        entry,
                        Full,
                        1
                    ) == Full);
                    capacities.Add(new { image.Name, Count = count });
                }
                Console.WriteLine(JsonSerializer.Serialize(new { RegisterCapacity273 = true, PrivateProfile = owned, ActualPreparedCapacities = capacities }));

                return true;
            });
        }
    }

    private static int MaximumX(object code)
        => ((IEnumerable)Get(code, "Instructions")).Cast<object>()
            .SelectMany(instruction => ((IEnumerable)Get(instruction, "Operands")).Cast<object>())
            .Where(operand => (string)Get(operand, "Kind") == "x")
            .Select(operand => (int)(BigInteger)Get(operand, "Value") + 1).DefaultIfEmpty(0).Max();

    private static void CheckUntrustedFootprintFallbacks(object source)
    {
        // Calculator negatives only: these raw clones are NEVER published,
        // admitted, validated or executed as runtime images/capabilities.
        object first = ((IEnumerable)Get(source, "Instructions")).Cast<object>().First();
        Type operandType = typeof(ProcessRuntime).Assembly.GetType("Erlang.BeamOperand", true)!;
        foreach (var (kind, value) in new[] { ("unknown273", BigInteger.Zero), ("unsigned", BigInteger.MinusOne), ("x", new BigInteger(Full)) })
        {
            object operand = Activator.CreateInstance(
                operandType,
                Members,
                null,
                [kind, value, null],
                null
            )!;
            Array operands = Array.CreateInstance(operandType, 1);
            operands.SetValue(operand, 0);
            object instruction = Activator.CreateInstance(
                first.GetType(),
                Members,
                null,
                [Get(first, "Offset"), Get(first, "Definition"), operands],
                null
            )!;
            Array instructions = Array.CreateInstance(first.GetType(), 1);
            instructions.SetValue(instruction, 0);
            object?[] arguments = new[] { "Structure", "Types", "Instructions", "Functions", "Labels", "Exports", "OnLoadEntryLabel", "Lambdas" }
                .Select(name => name == "Instructions" ? instructions : source.GetType().GetProperty(name, Members)!.GetValue(source)).ToArray();
            object clone = source.GetType().GetConstructors(Members).Single(ctor => ctor.GetParameters().Length == 8).Invoke(arguments);
            int count = (int)Capacity.GetMethod("Conservative", StaticMembers)!.Invoke(null, [clone])!;
            Require(count == Full);
        }
    }

    private static void CheckRoleAwareSizing475(object source, object actualEntry)
    {
        // These are calculator inputs ONLY: raw unsealed code/structure clones
        // are never published, validated, admitted or executed as images.
        Assembly assembly = typeof(ProcessRuntime).Assembly;
        Type operandType = assembly.GetType(RoleOperandType475, true)!;
        object exemplar = ((IEnumerable)Get(source, RoleInstructions475)).Cast<object>().First();
        Type definitionType = Get(exemplar, RoleDefinition475).GetType();

        object Operand(string kind, int value) => Activator.CreateInstance(
            operandType,
            Members,
            null,
            [kind, new BigInteger(value), null],
            null
        )!;

        Array Typed(Type type, object[] values)
        {
            Array result = Array.CreateInstance(type, values.Length);
            for (int index = 0; index < values.Length; index++) result.SetValue(values[index], index);

            return result;
        }

        object Clone(object value) => value.GetType().GetMethod(RoleClone475, Members)!.Invoke(value, null)!;

        void Set(object value, string name, object data) => value.GetType().GetProperty(name, Members)!.SetValue(value, data);

        object Raw(
            string opcode,
            object[] operands,
            int? functionArity = null,
            string?[]? atoms = null
        )
        {
            object code = Clone(source);
            int definitionArity = opcode is RoleBsMatch475 or RoleSelect475 ? RolePrefix475 : operands.Length;
            object definition = Activator.CreateInstance(
                definitionType,
                Members,
                null,
                [(byte)0, opcode, definitionArity, false],
                null
            )!;
            object instruction = Activator.CreateInstance(
                exemplar.GetType(),
                Members,
                null,
                [0, definition, Typed(operandType, operands)],
                null
            )!;
            Set(code, RoleInstructions475, Typed(exemplar.GetType(), [instruction]));
            foreach (string name in new[] { RoleFunctions475, RoleLambdas475 })
            {
                Type element = source.GetType().GetProperty(name, Members)!.PropertyType.GetGenericArguments().Single();
                Set(code, name, Array.CreateInstance(element, 0));
            }
            if (functionArity is { } arity)
            {
                object function = Clone(actualEntry);
                Set(function, RoleArity475, arity);
                Set(code, RoleFunctions475, Typed(function.GetType(), [function]));
            }
            if (atoms is not null)
            {
                object structure = Clone(Get(source, RoleStructure475));
                Set(structure, RoleAtoms475, atoms);
                Set(code, RoleStructure475, structure);
            }

            return code;
        }
        var observations = new List<object>();

        void Check(string name, object code, int expected)
        {
            int actual = (int)Capacity.GetMethod(RoleConservative475, StaticMembers)!.Invoke(null, [code])!;
            if (actual != expected)
                throw new InvalidOperationException(JsonSerializer.Serialize(new { Name = name, Actual = actual, Expected = expected }));
            // A successful private calculator probe never grants raw IR a
            // reference-keyed preparation/entry certificate.
            Require(Bound(
                code,
                actualEntry,
                0,
                0
            ) == Full);
            observations.Add(new { Name = name, Actual = actual, Expected = expected });
        }

        object U(int value) => Operand(RoleUnsigned475, value);

        object X(int value) => Operand(RoleX475, value);

        object L(int value) => Operand(RoleLabel475, value);

        object C(int value) => Operand(RoleCount475, value);
        Check(RoleUnknownUnsigned475, Raw(RoleUnknown475, [U(RoleLarge475)]), Full);
        Check(RoleUnknownCount475, Raw(RoleUnknown475, [C(RoleLarge475)]), Full);
        Check(RoleUnknownSmallUnsigned475, Raw(RoleUnknown475, [U(Small)]), Small + RoleExtra475);
        Check(RoleUnknownSmallCount475, Raw(RoleUnknown475, [C(Small)]), Small + RoleExtra475);
        foreach (string opcode in new[] { RoleLabelOpcode475, RoleLine475, RoleDeallocate475 })
            Check(opcode, Raw(opcode, [U(RoleLarge475)]), RoleMinimum475);
        Check(RoleAllocate475, Raw(RoleAllocate475, [U(RoleLarge475), U(0)]), RoleMinimum475);
        Check(RoleAllocateLive475, Raw(RoleAllocate475, [U(RoleLarge475), U(Small)]), Small + RoleExtra475);
        Check(RoleAllocateHeap475, Raw(RoleAllocateHeap475, [U(RoleLarge475), U(RoleLarge475), U(0)]), RoleMinimum475);
        Check(RoleAllocateHeapLive475, Raw(RoleAllocateHeap475, [U(RoleLarge475), U(RoleLarge475), U(Small)]), Small + RoleExtra475);
        Check(RoleTestHeap475, Raw(RoleTestHeap475, [U(RoleLarge475), U(0)]), RoleMinimum475);
        Check(RoleTestHeapLive475, Raw(RoleTestHeap475, [U(RoleLarge475), U(Small)]), Small + RoleExtra475);
        Check(RoleTrim475, Raw(RoleTrim475, [U(RoleLarge475), U(RoleLarge475)]), RoleMinimum475);
        Check(RoleY475, Raw(RoleMove475, [Operand(RoleY475, RoleLarge475), X(0)]), RoleMinimum475);
        Check(RoleHighX475, Raw(RoleMove475, [X(Full / 2), X(0)]), Full / 2 + 1);
        Check(RoleCallLast475, Raw(RoleCallLast475, [U(Small), L(1), U(RoleLarge475)]), Small + RoleExtra475);
        Check(RoleCallExtLast475, Raw(RoleCallExtLast475, [U(Small), U(0), U(RoleLarge475)]), Small + RoleExtra475);
        Check(RoleApply475, Raw(RoleApply475, [U(Small)]), Small + RoleExtra475);
        Check(RoleArity475, Raw(RoleReturn475, [], Small), Small);
        Check(RoleBadLayout475, Raw(RoleAllocate475, [U(RoleLarge475)]), Full);
        Check(RoleNegativeRole475, Raw(RoleLabelOpcode475, [U(-1)]), Full);

        string?[] atoms = [null, RoleEnsureAtLeast475, RoleEnsureExactly475, RoleSkip475, RoleEqual475, RoleBinary475, RoleTail475, RoleUnknown475];

        object Bs(object[] commands, int sourceX = 0) => Raw(RoleBsMatch475,
            new[] { L(1), X(sourceX), C(commands.Length) }.Concat(commands).ToArray(), atoms: atoms);
        Check(RoleEnsureAtLeast475, Bs([Operand(RoleAtom475, 1), U(RoleLarge475), U(RoleUnit475)]), RoleMinimum475);
        Check(RoleEnsureExactly475, Bs([Operand(RoleAtom475, 2), U(RoleLarge475)]), RoleMinimum475);
        Check(RoleSkip475, Bs([Operand(RoleAtom475, 3), U(RoleLarge475)]), RoleMinimum475);
        Check(RoleEqual475, Bs([Operand(RoleAtom475, 4), Operand(RoleNil475, 0), U(RolePatternBits475), U(RolePattern475)]), RoleMinimum475);
        Check(
            RoleBinary475,
            Bs([Operand(RoleAtom475, 5), U(3), Operand(RoleNil475, 0), U(RoleBinarySize475), U(RoleUnit475), X(2)]),
            RoleMinimum475 + 1
        );
        Check(
            RoleBsLive475,
            Bs([Operand(RoleAtom475, 5), U(Small), Operand(RoleNil475, 0), U(RoleBinarySize475), U(RoleUnit475), X(2)]),
            Small + RoleExtra475
        );
        Check(RoleBsDestination475, Bs([Operand(RoleAtom475, 6), U(0), U(1), X(Full / 2)]), Full / 2 + 1);
        Check(RoleBsSource475, Bs([Operand(RoleAtom475, 2), U(RoleLarge475)], Full / 2), Full / 2 + 1);
        Check(RoleBsUnknown475, Bs([Operand(RoleAtom475, 7), U(0)]), Full);
        Check(RoleBsMalformed475, Bs([Operand(RoleAtom475, 5), U(0)]), Full);
        Check(RoleBsInvalidLive475, Bs([Operand(RoleAtom475, 6), U(Full + 1), U(1), X(0)]), Full);
        Check(RoleBsBadCount475, Raw(RoleBsMatch475,
            [L(1), X(0), C(1), Operand(RoleAtom475, 2), U(RoleLarge475)], atoms: atoms), Full);
        object[] table = Enumerable.Range(0, RoleTableCount475 / 2)
            .SelectMany(index => new[] { Operand(RoleInteger475, index), L(1) }).ToArray();
        Check(RoleSelect475, Raw(RoleSelect475, new[] { X(0), L(1), C(table.Length) }.Concat(table).ToArray()), RoleMinimum475);
        Check(RoleSelectHigh475, Raw(RoleSelect475, new[] { X(Full / 2), L(1), C(table.Length) }.Concat(table).ToArray()), Full / 2 + 1);
        Check(RoleSelectBad475, Raw(RoleSelect475, new[] { X(0), L(1), C(table.Length - 1) }.Concat(table).ToArray()), Full);
        object[] duplicateTable = (object[])table.Clone();
        duplicateTable[2] = Operand(RoleInteger475, 0);
        Check(RoleSelectDuplicate475, Raw(RoleSelect475, new[] { X(0), L(1), C(table.Length) }.Concat(duplicateTable).ToArray()), Full);
        object[] malformedTable = (object[])table.Clone();
        malformedTable[1] = U(1);
        Check(RoleSelectTarget475, Raw(RoleSelect475, new[] { X(0), L(1), C(table.Length) }.Concat(malformedTable).ToArray()), Full);
        Require(Bound(
            source,
            actualEntry,
            -1,
            0
        ) == Full && Bound(
            source,
            actualEntry,
            0,
            -1
        ) == Full);
        Require(Bound(
            source,
            actualEntry,
            Small,
            0
        ) == Math.Max(Bound(
            source,
            actualEntry,
            0,
            0
        ), Small));
        Console.WriteLine(JsonSerializer.Serialize(new { RegisterCapacityRoles475 = true, Cases = observations }));
    }

    private static void CheckState(ProcessContext context)
    {
        object heap = Get(context, "TermHeap506");
        int rootsBefore = (int)Get(Get(heap, "Snapshot"), "RootFrames");
        using (var state = New(context, Small))
        {
            Require(Values(state).Length == Small);
            object? roots = State.GetField("roots507", Members)!.GetValue(state);
            if (roots is not null)
                foreach (string name in new[] { "first", "second", "alternate", "published", "reserved507" })
                    Require(((Array)roots.GetType().GetField(name, Members)!.GetValue(roots)!).Length == Small);
            for (int index = 0; index < Small; index++) Write(state, index, Term.Tuple(Term.I(index), Term.List(Term.I(index))));
            Method(state, "CheckLive", typeof(int)).Invoke(state, [Small]);
            using (var clone = (IDisposable)Method(state, "Clone").Invoke(state, null)!)
            {
                Require(Values(clone).Length == Small && !ReferenceEquals(Values(state), Values(clone)));
                Term preserved = Read(clone, Small - 1);
                Write(state, Small - 1, Term.A("changed273"));
                Require(Read(clone, Small - 1).Equals(preserved));
                Method(state, "ClearXExcept", typeof(int[])).Invoke(state, [new[] { Small - 1 }]);
                Require(Read(state, Small - 1).Equals(Term.A("changed273")));
                Require(Read(clone, Small - 1).Equals(preserved));
            }
        }
        Require((int)Get(Get(heap, "Snapshot"), "RootFrames") == rootsBefore);

        long Measure(int capacity)
        {
            long before = GC.GetAllocatedBytesForCurrentThread();
            for (int count = 0; count < Iterations; count++) using (New(context, capacity)) { }

            return GC.GetAllocatedBytesForCurrentThread() - before;
        }
        using (New(context, Small)) { }
        using (New(context, Full)) { }
        long small = Measure(Small), full = Measure(Full);
        Require(small < full);
        Require((int)Get(Get(heap, "Snapshot"), "RootFrames") == rootsBefore);
        Console.WriteLine(JsonSerializer.Serialize(new { RegisterCapacityAllocation273 = true, Iterations, SmallCapacity = Small, FullCapacity = Full, SmallAllocatedBytes = small, FullAllocatedBytes = full }));
    }

    private static IDisposable New(ProcessContext context, int count)
        => (IDisposable)State.GetConstructors(Members).Single(ctor => ctor.GetParameters().Length == 3)
            .Invoke([null, context, count]);

    private static Array Values(object state) => (Array)State.GetField("values", Members)!.GetValue(state)!;

    private static object Get(object target, string name) => target.GetType().GetProperty(name, Members)!.GetValue(target)!;

    private static MethodInfo Method(object target, string name, params Type[] types)
        => target.GetType().GetMethod(name, Members, types)!;

    private static void Write(object state, int index, Term value)
        => Method(
            state,
            "Write",
            typeof(int),
            typeof(Term)
        ).Invoke(state, [index, value]);

    private static Term Read(object state, int index)
        => (Term)Method(state, "Read", typeof(int)).Invoke(state, [index])!;

    private static int Bound(
        object code,
        object entry,
        int arguments,
        int environment
    )
    {
        try
        {
            return (int)Capacity.GetMethod("ForEntry", StaticMembers)!.Invoke(null, [code, entry, arguments, environment])!;
        }
        catch (TargetInvocationException error) when (error.InnerException is { } actual)
        {
            ExceptionDispatchInfo.Capture(actual).Throw();
            throw;
        }
    }

    private static void Require(bool condition)
    {
        if (!condition) throw new InvalidOperationException(Failure);
    }
}
