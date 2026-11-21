// SPDX-License-Identifier: Apache-2.0
// Assembly term and operand formats follow OTP-29.1.1 lib/compiler/src/beam_asm.erl.

using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Compiler.BeamContainerWriter;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    internal byte[] EmitAssembly(
        IReadOnlyList<Term> forms,
        BeamReleaseBuildProfile profile,
        BeamMacroPreprocessor? companion,
        string? companionName
    )
    {
        if (forms is not
            [
                TupleTerm { Items: [Atom { Name: "module" }, Atom module] },
                TupleTerm { Items: [Atom { Name: "exports" }, var exportList] },
                TupleTerm { Items: [Atom { Name: "attributes" }, var attributeList] },
                TupleTerm { Items: [Atom { Name: "labels" }, Integer labelCount] },
                ..
            ] || labelCount.Value < 2 || labelCount.Value > int.MaxValue)
        {
            throw BeamAssemblyDiagnostics.InvalidHeader;
        }

        if (module.Name != name)
        {
            throw BeamAssemblyDiagnostics.InvalidModule;
        }

        var pendingExports = new HashSet<(string Name, int Arity)>();
        foreach (Term exported in ReadAssemblyList(exportList))
        {
            if (exported is not TupleTerm { Items: [Atom function, Integer arity] }
                || !IsAssemblyArity(arity) || !pendingExports.Add((function.Name, (int)arity.Value)))
            {
                throw BeamAssemblyDiagnostics.InvalidHeader;
            }
        }

        var attributes = new List<ModuleAttribute>();
        foreach (Term attribute in ReadAssemblyList(attributeList))
        {
            if (attribute is not TupleTerm { Items: [Atom key, var value] })
            {
                throw BeamAssemblyDiagnostics.InvalidHeader;
            }

            if (key.Name == ModuleAttributeNames.OnLoad)
            {
                throw BeamAssemblyDiagnostics.Instruction(ModuleAttributeNames.OnLoad, 1);
            }

            attributes.Add(new(key.Name, value));
        }

        int labelsCount = (int)labelCount.Value;
        var labels = new HashSet<int>();
        var functions = new HashSet<(string Name, int Arity)>();
        var entries = new HashSet<int>();
        using var code = instructions;
        for (int index = 4; index < forms.Count; index++)
        {
            Term term = forms[index];
            if (term is TupleTerm { Items: [Atom { Name: "function" }, Atom function, Integer arity, Integer entry] })
            {
                if (!IsAssemblyArity(arity) || entry.Value < 1 || entry.Value >= labelsCount
                    || !functions.Add((function.Name, (int)arity.Value)) || !entries.Add((int)entry.Value))
                {
                    throw BeamAssemblyDiagnostics.InvalidFunctions;
                }

                var table = pendingExports.Remove((function.Name, (int)arity.Value)) ? exports : locals;
                table.Add((Intern(function.Name), (int)arity.Value, (int)entry.Value));
                continue;
            }

            if (functions.Count == 0)
            {
                throw BeamAssemblyDiagnostics.InvalidFunctions;
            }

            if (term is TupleTerm { Items: [Atom { Name: "label" }, Integer label] }
                && (label.Value < 1 || label.Value >= labelsCount || !labels.Add((int)label.Value)))
            {
                throw BeamAssemblyDiagnostics.InvalidFunctions;
            }

            EmitAssemblyInstruction(term);
        }

        if (pendingExports.Count != 0 || entries.Count == 0 || !entries.IsSubsetOf(labels))
        {
            throw BeamAssemblyDiagnostics.InvalidFunctions;
        }

        code.WriteByte(EndCode);
        byte[] image = WriteImage(
            BeamReleaseMetadataWriter.BuildAssembly(
                attributes,
                profile,
                companion,
                companionName,
                module.Name
            ),
            output => WriteCode(output, code.GetBuffer().AsMemory(0, checked((int)code.Length)), labelsCount)
        );
        try
        {
            _ = BeamModule.Prepare(image);
        }
        catch (InvalidDataException error)
        {
            throw BeamAssemblyDiagnostics.InvalidImage(error.Message);
        }

        return image;
    }

    private static bool IsAssemblyArity(Integer arity) => arity.Value >= 0 && arity.Value <= BeamWire.MaximumArity;

    private static Term[] ReadAssemblyList(Term items)
    {
        var result = new List<Term>();
        while (items is Cons cons)
        {
            result.Add(cons.Head);
            items = cons.Tail;
        }

        return items is Nil ? result.ToArray() : throw BeamAssemblyDiagnostics.Operand(items);
    }

    private void EmitAssemblyInstruction(Term instruction)
    {
        switch (instruction)
        {
            case Atom name:
                EmitAssemblyOpcode(name.Name, []);

                return;
            case TupleTerm { Items: [Atom { Name: "%" }, _] }:
                return;
            case TupleTerm { Items: [Atom { Name: "line" }, var locations] }:
                var location = ReadAssemblyList(locations).FirstOrDefault(item =>
                    item is TupleTerm { Items: [Atom { Name: "location" }, _, _] });
                if (location is TupleTerm { Items: [_, var file, Integer line] }
                    && line.Value > 0 && line.Value <= int.MaxValue)
                {
                    EmitLine(new(BeamDocumentationText.ReadText(file), (int)line.Value));
                }
                else if (location is null)
                {
                    EmitLine(default);
                }
                else
                {
                    throw BeamAssemblyDiagnostics.Operand(location);
                }

                return;
            case TupleTerm { Items: [Atom { Name: "test" }, Atom condition, var failure, var arguments] }
                when arguments is Cons or Nil:
                EmitAssemblyOpcode(condition.Name, [failure, .. ReadAssemblyList(arguments)]);

                return;
            case TupleTerm { Items: [Atom name, ..] } tuple:
                EmitAssemblyOpcode(name.Name, tuple.Items.Skip(1).ToArray());

                return;
            default:
                throw BeamAssemblyDiagnostics.Operand(instruction);
        }
    }

    private void EmitAssemblyOpcode(string instruction, IReadOnlyList<Term> operands)
    {
        var op = BeamOpcodeCatalog.All.FirstOrDefault(item => item.Name == instruction && item.Arity == operands.Count);
        if (op is null || op.Obsolete || op.Opcode == EndCode)
        {
            throw BeamAssemblyDiagnostics.Instruction(instruction, operands.Count);
        }

        highestOpcode = Math.Max(highestOpcode, op.Opcode);
        instructions.WriteByte(op.Opcode);
        foreach (Term operand in operands)
        {
            EmitAssemblyOperand(operand);
        }
    }

    private void EmitAssemblyOperand(Term operand)
    {
        switch (operand)
        {
            case Integer plain when plain.Value >= 0:
                Operand(BeamWire.UnsignedTag, plain.Value);

                return;
            case Atom { Name: "nil" }:
                Operand(AtomTag, NilAtomIndex);

                return;
            case TupleTerm { Items: [Atom { Name: "x" }, Integer x] } when x.Value >= 0:
                Operand(XRegisterTag, x.Value);

                return;
            case TupleTerm { Items: [Atom { Name: "y" }, Integer y] } when y.Value >= 0:
                Operand(YRegisterTag, y.Value);

                return;
            case TupleTerm { Items: [Atom { Name: "f" }, Integer label] } when label.Value >= 0 && label.Value <= int.MaxValue:
                Operand(LabelTag, label.Value);

                return;
            case TupleTerm { Items: [Atom { Name: "atom" }, Atom literal] }:
                LiteralOperand(literal);

                return;
            case TupleTerm { Items: [Atom { Name: "integer" }, Integer literal] }:
                LiteralOperand(literal);

                return;
            case TupleTerm { Items: [Atom { Name: "float" }, FloatTerm literal] }:
                LiteralOperand(literal);

                return;
            case TupleTerm { Items: [Atom { Name: "literal" }, var literal] }:
                LiteralOperand(literal);

                return;
            case TupleTerm { Items: [Atom { Name: "string" }, BitString { IsBinary: true } text] }:
                Operand(BeamWire.UnsignedTag, InternBinaryString(text.ToArray()));

                return;
            case TupleTerm { Items: [Atom { Name: "fr" }, Integer register] } when register.Value >= 0:
                instructions.WriteByte(ExtendedFloatRegisterOperand);
                Operand(BeamWire.UnsignedTag, register.Value);

                return;
            case TupleTerm { Items: [Atom { Name: "extfunc" }, Atom module, Atom name, Integer arity] }
                when IsAssemblyArity(arity):
                Operand(BeamWire.UnsignedTag, Import(module.Name, name.Name, (int)arity.Value));

                return;
            case TupleTerm { Items: [Atom { Name: "list" }, var list] }:
                Term[] values = ReadAssemblyList(list);
                instructions.WriteByte(ExtendedListOperand);
                Operand(BeamWire.UnsignedTag, values.Length);
                foreach (Term value in values)
                {
                    EmitAssemblyOperand(value);
                }

                return;
            default:
                throw BeamAssemblyDiagnostics.Operand(operand);
        }
    }
}
