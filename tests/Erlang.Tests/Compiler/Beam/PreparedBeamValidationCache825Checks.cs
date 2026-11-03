using System.Collections;
using System.Reflection;
using Erlang;
using static Erlang.Tests.PreparedBeamValidationCache825Names;

namespace Erlang.Tests;

// Optional Root-owned harness enrollment: Test(TestCaseName, PreparedBeamValidationCache825Checks.Run);
internal static class PreparedBeamValidationCache825Checks
{
    private const BindingFlags Static = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
    private const BindingFlags Instance = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

    internal static async Task Run()
    {
        byte[] bytes = (byte[])typeof(BeamPreparedHostChecks).GetMethod(ResourceMethod, Static)!
            .Invoke(null, [BeamPreparedHostNames.Constant + BeamPreparedHostNames.BeamExtension])!;
        await using var runtime = new ProcessRuntime();
        await using var image = runtime.Modules.PrepareBeam(bytes);
        await using var foreign = new ProcessRuntime();
        await using var otherImage = foreign.Modules.PrepareBeam(bytes);
        object code = Get(image, CodeProperty);
        object otherCode = Get(otherImage, CodeProperty);
        object entry = ((IEnumerable)Get(code, ExportsProperty)).Cast<object>().Single(value => (string)Get(value, NameProperty) == BeamPreparedHostNames.Run);
        object otherEntry = ((IEnumerable)Get(otherCode, ExportsProperty)).Cast<object>().Single(value => (string)Get(value, NameProperty) == BeamPreparedHostNames.Run);
        Type cache = typeof(ProcessRuntime).Assembly.GetType(CacheTypeName, true)!;
        MethodInfo lookup = cache.GetMethod(TryGetMethod, Static)!;
        MethodInfo validate = typeof(ProcessRuntime).Assembly.GetType(ValidationTypeName, true)!
            .GetMethod(ValidateMethod, Static)!;

        object? Certificate(object value)
        {
            object?[] arguments = [value, null];

            return (bool)lookup.Invoke(null, arguments)! ? arguments[1] : null;
        }

        bool Has(object certificate, object function)
            => (bool)certificate.GetType().GetMethod(HasSuccessMethod, Instance)!.Invoke(certificate, [function])!;
        object certificate = Certificate(code) ?? throw new InvalidOperationException(MissingCertificateMessage);
        Require(Has(certificate, entry));
        Require(!ReferenceEquals(code, otherCode));
        Require(!ReferenceEquals(certificate, Certificate(otherCode)));
        Require(!Has(certificate, otherEntry));

        CheckPreparationBatch477(code, entry, Certificate);

        object clone = Clone(code);
        Require(Certificate(clone) is null); // Equal record / with clone has no certificate.
        validate.Invoke(null, [clone, entry]);
        Require(Certificate(clone) is null); // A successful raw call cannot enroll itself.

        object alias = Clone(entry);
        Require(!Has(certificate, alias));
        validate.Invoke(null, [code, alias]); // Full validation succeeds, but membership remains reference exact.
        Require(!Has(certificate, alias));

        object malformed = Clone(code);
        Type instruction = typeof(ProcessRuntime).Assembly.GetType(InstructionTypeName, true)!;
        malformed.GetType().GetProperty(InstructionsProperty, Instance)!.SetValue(malformed, Array.CreateInstance(instruction, 0));
        Require(Certificate(malformed) is null);
        bool refused = false;
        try
        {
            validate.Invoke(null, [malformed, entry]);
        }
        catch (TargetInvocationException failure) when (failure.InnerException is not null)
        {
            refused = true;
        }
        Require(refused && Certificate(malformed) is null);

        // Preparation owns its input bytes. Mutating caller storage grants no new certificate.
        bytes[0] = 0;
        Require(ReferenceEquals(certificate, Certificate(code)) && Has(certificate, entry));
        validate.Invoke(null, [code, entry]);
    }


    private static void CheckPreparationBatch477(object source, object exemplarEntry, Func<object, object?> certificate)
    {
        // Raw structural calculator inputs only. No image publication, sealing
        // or VM execution is performed with these synthetic code objects.
        Assembly assembly = typeof(ProcessRuntime).Assembly;
        Type validation = assembly.GetType(ValidationTypeName, true)!;
        Type batchType = validation.GetNestedType(PreparationBatchTypeName, BindingFlags.NonPublic)!;
        Type operandType = assembly.GetType(OperandTypeName, true)!;
        object exemplarInstruction = ((IEnumerable)Get(source, InstructionsProperty)).Cast<object>().First();
        Type definitionType = Get(exemplarInstruction, DefinitionProperty).GetType();

        void Set(object value, string name, object data) => value.GetType().GetProperty(name, Instance)!.SetValue(value, data);

        Array Typed(Type type, object[] values)
        {
            Array result = Array.CreateInstance(type, values.Length);
            for (int index = 0; index < values.Length; index++) result.SetValue(values[index], index);

            return result;
        }

        object Op(string kind, int value) => Activator.CreateInstance(
            operandType,
            Instance,
            null,
            [kind, new System.Numerics.BigInteger(value), null],
            null
        )!;

        object Instruction(string name, params object[] operands)
        {
            object definition = Activator.CreateInstance(
                definitionType,
                Instance,
                null,
                [(byte)0, name, operands.Length, false],
                null
            )!;

            return Activator.CreateInstance(
                exemplarInstruction.GetType(),
                Instance,
                null,
                [0, definition, Typed(operandType, operands)],
                null
            )!;
        }

        object Function(int label, int first, int end)
        {
            object function = Clone(exemplarEntry);
            Set(function, EntryLabelProperty, label);
            Set(function, FirstInstructionProperty, first);
            Set(function, EndInstructionProperty, end);
            Set(function, ArityProperty, 0);

            return function;
        }

        object Code(object[] instructions, object[] functions)
        {
            object code = Clone(source);
            Set(code, InstructionsProperty, Typed(exemplarInstruction.GetType(), instructions));
            Set(code, FunctionsProperty, Typed(exemplarEntry.GetType(), functions));
            Type lambda = source.GetType().GetProperty(LambdasProperty, Instance)!.PropertyType.GetGenericArguments().Single();
            Set(code, LambdasProperty, Array.CreateInstance(lambda, 0));
            Set(code, LabelsProperty, new Dictionary<int, int> { [1] = 0, [2] = 1 });

            return code;
        }

        object Batch(object code) => Activator.CreateInstance(
            batchType,
            Instance,
            null,
            [code],
            null
        )!;

        void Validate(object batch, object function) => batchType.GetMethod(ValidateMethod, Instance)!.Invoke(batch, [function]);

        void Refused(object batch, object function, Type expected)
        {
            Exception? caught = null;
            try
            {
                Validate(batch, function);
            }
            catch (TargetInvocationException error)
            {
                caught = error.InnerException;
            }
            Require(caught is not null && expected.IsInstanceOfType(caught));
        }
        object first = Function(1, 0, 1), second = Function(2, 1, 2);
        object independent = Code([Instruction(ReturnOpcode), Instruction(UnsupportedOpcode)], [first, second]);
        object lateFailure = Batch(independent);
        Validate(lateFailure, first);
        Refused(lateFailure, second, typeof(NotSupportedException));
        Refused(lateFailure, first, typeof(InvalidOperationException));
        Require(certificate(independent) is null);

        object transitive = Code(
            [Instruction(LocalTailCallOpcode, Op(UnsignedOperand, 0), Op(LabelOperand, 2)), Instruction(UnsupportedOpcode)],
            [first, second]
        );
        object sharedFailure = Batch(transitive);
        Refused(sharedFailure, first, typeof(NotSupportedException));
        Refused(sharedFailure, second, typeof(InvalidOperationException));
        Require(certificate(transitive) is null);

        object successful = Code([Instruction(LocalTailCallOpcode, Op(UnsignedOperand, 0), Op(LabelOperand, 2)), Instruction(ReturnOpcode)], [first, second]);
        object sharedSuccess = Batch(successful);
        Validate(sharedSuccess, first);
        Validate(sharedSuccess, second); // Export already reached as a canonical callee.
        Require(certificate(successful) is null);

        // bs_match INTEGER flags actually traverse Structure.Literals through
        // BeamBinaryCommands.Read -> ListFlags. Both phases share the exact
        // function and instructions, but the second code has different literals.
        object phaseEntry = Function(1, 0, 3);
        object phaseOne = Code(
            [Instruction(MoveOpcode, Op(NilOperand, 0), Op(XOperand, 0)), Instruction(ReturnOpcode), Instruction(
                BinaryMatchOpcode,
                Op(LabelOperand, 1),
                Op(XOperand, 0),
                Op(CountOperand, 6),
                Op(AtomOperand, 1),
                Op(UnsignedOperand, 0),
                Op(LiteralOperand, 0),
                Op(UnsignedOperand, 8),
                Op(UnsignedOperand, 1),
                Op(XOperand, 0)
            )],
            [phaseEntry]
        );
        object structure = Clone(Get(source, StructureProperty));
        Set(structure, AtomsProperty, new string?[] { null, IntegerCommandAtom });
        Set(structure, LiteralsProperty, new Term[] { Term.List(Term.A(SignedFlagAtom)) });
        Set(phaseOne, StructureProperty, structure);
        Validate(Batch(phaseOne), phaseEntry);
        object phaseTwo = Clone(phaseOne);
        object changedStructure = Clone(structure);
        Set(changedStructure, LiteralsProperty, new Term[] { Term.List(Term.A(InvalidFlagAtom)) });
        Set(phaseTwo, StructureProperty, changedStructure);
        Refused(Batch(phaseTwo), phaseEntry, typeof(NotSupportedException));
        Require(certificate(phaseOne) is null && certificate(phaseTwo) is null);
    }

    private static object Get(object value, string name)
        => value.GetType().GetProperty(name, Instance)!.GetValue(value)!;

    private static object Clone(object value)
        => value.GetType().GetMethod(RecordCloneMethod, Instance)!.Invoke(value, null)!;

    private static void Require(bool condition)
    {
        if (!condition) throw new InvalidOperationException(BoundaryFailureMessage);
    }
}
