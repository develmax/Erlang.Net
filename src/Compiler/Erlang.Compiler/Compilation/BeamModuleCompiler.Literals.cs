using System.Numerics;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Compiler.BeamContainerWriter;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private static readonly BigInteger InlineIntegerLimit = BigInteger.One << CompactIntegerMagnitudeBits;

    private readonly Dictionary<string, int> literalIndices = new(StringComparer.Ordinal);
    private readonly List<byte[]> literalBytes = [];
    private readonly HashSet<int> nonnumericLiteralIndices = [];
    private readonly Dictionary<int, (string Tag, int Arity)> taggedTupleLiterals = [];

    private static FunctionTerm? ReadExternalFunctionLiteral(Expr expression)
    {
        if (expression is not Expr.Call
            {
                Module: ErlangModuleNames.Module,
                Function: FunctionReferenceLoweringNames.MakeFunction,
                DynamicRemote: false,
                Arguments: [
                    Expr.Literal { Value: Atom module },
                    Expr.Literal { Value: Atom function },
                    Expr.Literal { Value: Integer arity }
                ]
            } || arity.Value < 0 || arity.Value > BeamWire.MaximumArity)
        {
            return null;
        }

        return ExternalFunctionLiteral(module, function, (int)arity.Value);
    }

    private FunctionTerm? ReadAutoImportedFunctionLiteral(
        Expr expression,
        IReadOnlyDictionary<(string Name, int Arity), int> entries
    )
    {
        if (expression is not Expr.FunctionReference reference
            || entries.ContainsKey((reference.Name, reference.Arity))
            || SourceCallResolution.ExternalModule(sourceModule, reference.Name, reference.Arity) != ErlangModuleNames.Module)
        {
            return null;
        }

        return ExternalFunctionLiteral(Term.A(ErlangModuleNames.Module), Term.A(reference.Name), reference.Arity);
    }

    private static FunctionTerm ExternalFunctionLiteral(Atom module, Atom function, int arity) =>
        new(
            new ExternalFunctionIdentity(module, function, arity),
            (context, arguments) => ((ProcessContext)context).Runtime.Modules.Call(
                (ProcessContext)context,
                module.Name,
                function.Name,
                arguments.ToArray()
            )
        );

    private void EmitConstant(Expr expression, int register)
    {
        if (!AttributeLiteral.TryNormalizeConstant(expression, out Term literal))
        {
            throw new NotSupportedException(Unsupported);
        }

        EmitConstant(literal, register);
    }

    private void EmitConstant(Term literal, int register, int registerTag = XRegisterTag)
    {
        instructions.WriteByte(Move);
        LiteralOperand(literal);
        Operand(registerTag, register);
    }

    private void LiteralOperand(Term literal)
    {
        switch (literal)
        {
            case Atom atom:
                Operand(AtomTag, Intern(atom.Name));

                return;
            case Nil:
                Operand(AtomTag, NilAtomIndex);

                return;
            case Integer integer:
                WriteIntegerOperand(instructions, integer.Value);

                return;
        }

        WriteLiteralPoolOperand(instructions, literal);
    }

    private void WriteIntegerOperand(Stream output, BigInteger value)
    {
        if (BigInteger.Abs(value) < InlineIntegerLimit)
        {
            BeamCompactWriter.Write(output, BeamWire.IntegerTag, value);
        }
        else
        {
            WriteLiteralPoolOperand(output, new Integer(value));
        }
    }

    private void WriteLiteralPoolOperand(Stream output, Term literal)
    {
        byte[] etf = ExternalTermFormat.Encode(literal, new EtfEncodingOptions { Deterministic = true });
        string key = Convert.ToBase64String(etf);
        if (!literalIndices.TryGetValue(key, out int index))
        {
            index = literalBytes.Count;
            literalIndices.Add(key, index);
            literalBytes.Add(etf);
            if (HasNonnumericLiteralType(literal))
            {
                nonnumericLiteralIndices.Add(index);
            }

            if (literal is TupleTerm { Items: [Atom tag, ..] } tuple)
            {
                taggedTupleLiterals.Add(index, (tag.Name, tuple.Items.Count));
            }
        }
        output.WriteByte(ExtendedLiteralOperand);
        BeamCompactWriter.Write(output, BeamWire.UnsignedTag, index);
    }

    private void EmitLiterals(Stream output)
    {
        if (sourceModule?.LegacyBeamTables == true)
        {
            byte[] table = Payload(EmitLiteralTable);
            Word(output, table.Length);
            using var compressed = new System.IO.Compression.ZLibStream(
                output, System.IO.Compression.CompressionLevel.Optimal, leaveOpen: true);
            compressed.Write(table);

            return;
        }

        Word(output, 0);
        EmitLiteralTable(output);
    }

    private void EmitLiteralTable(Stream output)
    {
        Word(output, literalBytes.Count);
        foreach (byte[] literal in literalBytes)
        {
            Word(output, literal.Length);
            output.Write(literal);
        }
    }
}
