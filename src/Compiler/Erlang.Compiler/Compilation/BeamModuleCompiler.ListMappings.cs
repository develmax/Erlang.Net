using static Erlang.Compiler.BeamEmissionFormat;

namespace Erlang.Compiler;

internal sealed partial class BeamModuleCompiler
{
    private abstract record MappingPlan;
    private sealed record ListMapping(Expr.Call Convert, Expr.Call Collect, long? Filter, Expr.Match? Discard = null) : MappingPlan;
    private sealed record TaggedEncoding(ListMapping Map, Expr.Call Wrap, Expr.Call Encode);
    private sealed record TaggedListJoin(Atom Tag, Term Separator, ListMapping Map);
    private sealed record SplitListJoin(Expr.Call Split, Term Delimiter, Term Mode, Term Separator, ListMapping Map);

    private string activeFunction = string.Empty;

    private static string MappingName(string name, int arity) => $"-{name}/{arity}-lc$^0/1-0-";

    private static MappingPlan? ReadListMappingPlan(IReadOnlyList<Clause> clauses)
    {
        MappingPlan? map = ReadOctetMapping(clauses) ?? ReadTaggedEncoding(clauses)?.Map
            ?? ReadTaggedListJoin(clauses)?.Map ?? ReadTableProjection(clauses)?.Mapping
            ?? ReadSplitListJoin(clauses)?.Map;
        map ??= ReadColumnMapping(clauses);

        return map ?? ReadAllocatorRows(clauses)?.Mapping;
    }

    private static SplitListJoin? ReadSplitListJoin(IReadOnlyList<Clause> clauses)
    {
        if (clauses is not [
            { Guard: null, Patterns: [Pattern.Variable input], Body: Expr.Call
                { Module: "lists", Function: "join", DynamicRemote: false, Arguments: [
                    var separator, Expr.ListComprehension { Items: [
                        Expr.Call { Module: not null, DynamicRemote: false, Arguments: [Expr.Variable selected] } convert],
                        Qualifiers: [ComprehensionQualifier.Generator { Strict: false, Pattern: Pattern.Variable element,
                            Source: Expr.Call { Module: "string", Function: "split", DynamicRemote: false,
                                Arguments: [Expr.Variable source, var delimiter, var mode] } split } generator] }] } join }
        ] || !DistinctBindings(input, element) || input.Name != source.Name || element.Name != selected.Name
            || !AttributeLiteral.TryNormalizeConstant(delimiter, out Term delimiterValue)
            || !AttributeLiteral.TryNormalizeConstant(mode, out Term modeValue)
            || !AttributeLiteral.TryNormalizeConstant(separator, out Term separatorValue))
        {
            return null;
        }

        var collect = join with
        {
            SourceLine = generator.GeneratorLine == 0 ? join.SourceLine : generator.GeneratorLine,
            PreprocessorFile = generator.GeneratorFile ?? join.PreprocessorFile
        };

        return new(split, delimiterValue, modeValue, separatorValue, new(convert, collect, null));
    }

    private void EmitSplitListJoin(SplitListJoin flow, BeamSourceLocation location,
        IReadOnlyDictionary<(string Name, int Arity), int> entries)
    {
        Op(Allocate, BeamWire.UnsignedTag, 0);
        Operand(BeamWire.UnsignedTag, 1);
        EmitConstant(flow.Mode, 2);
        EmitConstant(flow.Delimiter, 1);
        EmitCallLine(flow.Split, location);
        EmitStaticRemoteCall(flow.Split);
        EmitCallLine(flow.Map.Collect, CallLocation(flow.Split, location));
        EmitFieldCall(1, entries[(MappingName(activeFunction, 1), 1)]);
        MoveRegister(XRegisterTag, 0, XRegisterTag, 1);
        EmitConstant(flow.Separator, 0);
        Op(TailExternalCallWithFrame, BeamWire.UnsignedTag, 2);
        Operand(BeamWire.UnsignedTag, Import("lists", "join", 2));
        Operand(BeamWire.UnsignedTag, 0);
    }

    private static TaggedListJoin? ReadTaggedListJoin(IReadOnlyList<Clause> clauses)
    {
        if (clauses is not [
            {
                Guard: null, Patterns: [Pattern.Tuple { Items: [Pattern.Literal { Value: Atom tag }, Pattern.Variable source] }],
                Body: Expr.Call
                {
                    Module: "lists", Function: "flatten", DynamicRemote: false, Arguments: [
                    Expr.Call
                    {
                        Module: "lists", Function: "join", DynamicRemote: false, Arguments: [
                        var separator, Expr.ListComprehension
                        {
                            Items: [Expr.Call { Module: null, Function: IntegerToList, Arguments: [Expr.Variable selected] } convert],
                            Qualifiers: [ComprehensionQualifier.Generator { Strict: false, Pattern: Pattern.Variable element, Source: Expr.Variable list }]
                        }]
                    } join]
                }
            },
            { Guard: null, Patterns: [Pattern.Variable fallback], Body: Expr.Variable identity }
        ] || !DistinctBindings(source, element) || !DistinctBindings(fallback) || source.Name != list.Name
            || element.Name != selected.Name || fallback.Name != identity.Name
            || !AttributeLiteral.TryNormalizeConstant(separator, out Term literal))
        {
            return null;
        }

        return new(tag, literal, new(convert with { Module = ErlangModuleNames.Module }, join, null));
    }

    private void EmitTaggedListJoin(TaggedListJoin flow, BeamSourceLocation location,
        IReadOnlyDictionary<(string Name, int Arity), int> entries)
    {
        if (!ResolvesIntegerFormattingBif())
        {
            throw new NotSupportedException(Unsupported);
        }

        int fallback = nextLabel++;
        Op(IsTaggedTuple, LabelTag, fallback);
        Operand(XRegisterTag, 0);
        Operand(BeamWire.UnsignedTag, 2);
        Operand(AtomTag, Intern(flow.Tag.Name));
        Op(Allocate, BeamWire.UnsignedTag, 0);
        Operand(BeamWire.UnsignedTag, 1);
        EmitTuplePart(0, 1, 0);
        EmitCallLine(flow.Map.Collect, location);
        EmitFieldCall(1, entries[(MappingName(activeFunction, 1), 1)]);
        MoveRegister(
            XRegisterTag,
            0,
            XRegisterTag,
            1
        );
        instructions.WriteByte(Move);
        LiteralOperand(flow.Separator);
        Operand(XRegisterTag, 0);
        Op(ExternalCall, BeamWire.UnsignedTag, 2);
        Operand(BeamWire.UnsignedTag, Import("lists", "join", 2));
        EmitTailRemote("lists", "flatten", framed: true);
        Op(Label, BeamWire.UnsignedTag, fallback);
        instructions.WriteByte(Return);
    }

    private static ListMapping? ReadOctetMapping(IReadOnlyList<Clause> clauses)
    {
        if (clauses is [var binary, { Guard: null, Patterns: [Pattern.Variable children], Body: var body }]
            && ReadIdentityGuard(binary) == IsBinary
            && ReadListMapping(body, children.Name, true) is { } map)
        {
            return map;
        }

        return null;
    }

    private static TaggedEncoding? ReadTaggedEncoding(IReadOnlyList<Clause> clauses)
    {
        if (clauses is not [
            {
                Patterns: [Pattern.Literal { Value: Atom { Name: "false" } },
                Pattern.Tuple { Items: [Pattern.Variable { Name: VariableScopeNames.Wildcard }, Pattern.Variable content] }, var empty],
                Guard: Expr.Call
                {
                    Module: null or ErlangModuleNames.Module, Function: "is_list",
                    DynamicRemote: false, Arguments: [Expr.Variable tested]
                }, Body: var mapped
            },
            {
                Guard: null, Patterns: [Pattern.Literal { Value: Atom { Name: "false" } },
                Pattern.Tuple { Items: [Pattern.Variable { Name: VariableScopeNames.Wildcard }, Pattern.Variable direct] }, var nil],
                Body: Expr.Variable returned
            },
            {
                Guard: null, Patterns: [Pattern.Variable choice, Pattern.Variable tlv, Pattern.Variable tags],
                Body: Expr.Call
                {
                    Module: not null, DynamicRemote: false, Arguments: [
                    Expr.Call { Module: null, Arguments: [Expr.Variable first, Expr.Variable second, Expr.Variable third] } wrap]
                } encode
            }
        ] || !IsEmptyListPattern(empty) || !IsEmptyListPattern(nil) || content.Name != tested.Name
            || !DistinctBindings(content) || !DistinctBindings(direct) || direct.Name != returned.Name
            || !DistinctBindings(choice, tlv, tags) || choice.Name != first.Name || tlv.Name != second.Name || tags.Name != third.Name
            || ReadListMapping(mapped, content.Name, false) is not { } map)
        {
            return null;
        }

        return new(map, wrap, encode);
    }

    private static ListMapping? ReadListMapping(Expr body, string source, bool filtered)
    {
        if (body is not Expr.Call
            {
                Module: null or ErlangModuleNames.Module, DynamicRemote: false, Function: "iolist_to_binary",
                Arguments: [Expr.ListComprehension
                {
                    Items: [Expr.Call { DynamicRemote: false, Arguments: [Expr.Variable selected] } convert],
                    Qualifiers: [ComprehensionQualifier.Generator { Source: Expr.Variable children, Strict: false, Pattern: var pattern }]
                }]
            } collect || source != children.Name)
        {
            return null;
        }

        if (filtered && convert.Module is null
            && pattern is Pattern.Tuple { Items: [Pattern.Literal { Value: Integer tag }, Pattern.Variable value] }
            && tag.Value >= int.MinValue && tag.Value <= int.MaxValue && DistinctBindings(value) && value.Name == selected.Name)
        {
            return new(convert, collect, (int)tag.Value);
        }
        if (!filtered && convert.Module is not null && pattern is Pattern.Variable child
            && DistinctBindings(child) && child.Name == selected.Name)
        {
            return new(convert, collect, null);
        }

        return null;
    }

    private void EmitOctetMapping(ListMapping map, BeamSourceLocation location, IReadOnlyDictionary<(string Name, int Arity), int> entries)
    {
        int fallback = nextLabel++;
        Op(IsBinary, LabelTag, fallback);
        Operand(XRegisterTag, 0);
        instructions.WriteByte(Return);
        Op(Label, BeamWire.UnsignedTag, fallback);
        EmitListCollection(
            map,
            1,
            location,
            entries
        );
    }

    private void EmitTaggedEncoding(TaggedEncoding flow, BeamSourceLocation location,
        IReadOnlyDictionary<(string Name, int Arity), int> entries)
    {
        if (!entries.TryGetValue((flow.Wrap.Function, 3), out int wrap))
        {
            throw new NotSupportedException(Unsupported);
        }
        int direct = nextLabel++;
        int tagged = nextLabel++;
        EmitAtomTest(0, "false", tagged);
        EmitTupleCheck(1, 2, tagged);
        EmitNilTest(XRegisterTag, 2, tagged);
        EmitTuplePart(1, 1, 0);
        Op(IsList, LabelTag, direct);
        Operand(XRegisterTag, 0);
        EmitListCollection(
            flow.Map,
            3,
            location,
            entries
        );

        Op(Label, BeamWire.UnsignedTag, direct);
        instructions.WriteByte(Return);
        Op(Label, BeamWire.UnsignedTag, tagged);
        Op(Allocate, BeamWire.UnsignedTag, 0);
        Operand(BeamWire.UnsignedTag, 3);
        EmitCallLine(flow.Wrap, CallLocation(flow.Map.Collect, location));
        EmitFieldCall(3, wrap);
        EmitTailRemote(flow.Encode.Module!, flow.Encode.Function, framed: true);
    }

    private void EmitListCollection(
        ListMapping map,
        int parentArity,
        BeamSourceLocation location,
        IReadOnlyDictionary<(string Name, int Arity), int> entries
    )
    {
        if (!entries.TryGetValue((MappingName(activeFunction, parentArity), 1), out int helper)
            || map.Collect.Module is null && entries.ContainsKey(("iolist_to_binary", 1)))
        {
            throw new NotSupportedException(Unsupported);
        }
        Op(Allocate, BeamWire.UnsignedTag, 0);
        Operand(BeamWire.UnsignedTag, 1);
        EmitCallLine(map.Collect, location);
        EmitFieldCall(1, helper);
        EmitTailRemote(ErlangModuleNames.Module, "iolist_to_binary", framed: true);
    }

    private void EmitMappingHelper(
        string name,
        ListMapping map,
        BeamSourceLocation location,
        IReadOnlyDictionary<(string Name, int Arity), int> entries
    )
    {
        int entry = entries[(name, 1)];
        if (map.Discard is not null)
        {
            EmitTupleProjectionHelper(name, map, location, entry);

            return;
        }
        int skip = map.Filter.HasValue ? nextLabel + 2 : 0;
        int empty = nextLabel + (map.Filter.HasValue ? 3 : 2);
        int invalid = empty + 1;
        StartFunction(
            name,
            1,
            CallLocation(map.Collect, location),
            exported: false
        );
        nextLabel = invalid + 1;

        EmitNonemptyList(0, empty);
        if (map.Filter is long filter)
        {
            if (!entries.TryGetValue((map.Convert.Function, 1), out int convert))
            {
                throw new NotSupportedException(Unsupported);
            }
            EmitListParts(
                0,
                XRegisterTag,
                1,
                XRegisterTag,
                0
            );
            EmitTupleCheck(1, 2, skip);
            EmitTuplePart(1, 0, 2);
            Op(ExactEqualTest, LabelTag, skip);
            Operand(XRegisterTag, 2);
            Operand(BeamWire.IntegerTag, filter);
            Op(Allocate, BeamWire.UnsignedTag, 1);
            Operand(BeamWire.UnsignedTag, 2);
            MoveRegister(
                XRegisterTag,
                0,
                YRegisterTag,
                0
            );
            EmitTuplePart(1, 1, 0);
            EmitFieldCall(1, convert);
        }
        else
        {
            Op(Allocate, BeamWire.UnsignedTag, 1);
            Operand(BeamWire.UnsignedTag, 1);
            EmitListParts(
                0,
                XRegisterTag,
                0,
                YRegisterTag,
                0
            );
            Op(ExternalCall, BeamWire.UnsignedTag, 1);
            Operand(BeamWire.UnsignedTag, Import(map.Convert.Module ?? ErlangModuleNames.Module, map.Convert.Function, 1));
        }
        EmitMappingResult(entry);

        if (skip != 0)
        {
            Op(Label, BeamWire.UnsignedTag, skip);
            Op(TailLocalCall, BeamWire.UnsignedTag, 1);
            Operand(LabelTag, entry);
        }
        EmitMappingEnd(empty, invalid);
    }

    private void EmitMappingResult(int entry, BeamSourceLocation? recursionLocation = null)
    {
        Op(Swap, YRegisterTag, 0);
        Operand(XRegisterTag, 0);
        if (recursionLocation is { } line)
        {
            EmitLine(line);
        }
        EmitFieldCall(1, entry);
        Op(TestHeap, BeamWire.UnsignedTag, 2);
        Operand(BeamWire.UnsignedTag, 1);
        Op(PutList, YRegisterTag, 0);
        Operand(XRegisterTag, 0);
        Operand(XRegisterTag, 0);
        EmitFrameReturn(1);
    }

    private void EmitMappingEnd(int empty, int invalid)
    {
        Op(Label, BeamWire.UnsignedTag, empty);
        EmitNilTest(XRegisterTag, 0, invalid);
        instructions.WriteByte(Return);
        Op(Label, BeamWire.UnsignedTag, invalid);
        Op(TestHeap, BeamWire.UnsignedTag, 3);
        Operand(BeamWire.UnsignedTag, 1);
        EmitProtectedPair(AtomTag, Intern("bad_generator"));
        EmitTailRemote(ErlangModuleNames.Module, "error", framed: false);
    }

    private static Expr.Call? ReadMappingConversion(Expr expression) => expression is Expr.Call
    {
        Module: null or ErlangModuleNames.Module, Function: "binary_to_list", DynamicRemote: false,
        Arguments: [Expr.Call]
    } conversion ? conversion : null;

    private static (Expr.Call Mapped, Term? Argument, Expr.Call? Conversion, Term? Suffix)? ReadClosedLocalMapping(FunctionDefinition function)
    {
        if (function.Clauses is not [
            {
                Guard: null, Patterns: [Pattern.List { Items: [Pattern.Variable item], Tail: Pattern.Variable tail }],
                Body: Expr.List
                {
                    Items: [var value],
                    Tail: Expr.Call { Module: null, DynamicRemote: false, Arguments: [Expr.Variable rest] } recurse
                }
            },
            { Guard: null, Patterns: [var nil], Body: Expr.Literal { Value: Nil } },
            {
                Guard: null, Patterns: [Pattern.Variable bad], Body: Expr.Call
                {
                    Module: ErlangModuleNames.Module, Function: "error", DynamicRemote: false,
                    Arguments: [Expr.Tuple
                    {
                        Items: [Expr.Literal { Value: Atom { Name: "bad_generator" } },
                          Expr.Variable rejected]
                    }]
                }
            }]
            || !DistinctBindings(item, tail) || rest.Name != tail.Name
            || recurse.Function != function.Name || !IsEmptyListPattern(nil)
            || bad.Name == VariableScopeNames.Wildcard || rejected.Name != bad.Name)
        {
            return null;
        }

        var conversion = ReadMappingConversion(value);
        var mapped = conversion is null ? value : conversion.Arguments[0];

        if (mapped is Expr.Call
            {
                Module: "unicode", Function: "characters_to_binary", DynamicRemote: false,
                Arguments: [var unicodeInput]
            } unicode && ReadUnicodeMappingInput(unicodeInput) is { } inputPlan
            && inputPlan.Name == item.Name && conversion is null)
        {
            return (unicode, null, null, inputPlan.Suffix);
        }

        return mapped is Expr.Call
        {
            Module: null, DynamicRemote: false,
            Arguments: [Expr.Variable input, var argument]
        } call
               && input.Name == item.Name && AttributeLiteral.TryNormalizeConstant(argument, out Term constant)
            ? (call, constant, conversion, null)
            : null;
    }

    private void EmitClosedLocalMapping(
        Expr.Call mapped,
        Term? argument,
        Expr.Call? conversion,
        int convert,
        int entry,
        BeamSourceLocation location,
        Term? suffix
    )
    {
        int empty = nextLabel++;
        int invalid = nextLabel++;
        EmitNonemptyList(0, empty);
        Op(suffix is null ? Allocate : AllocateHeap, BeamWire.UnsignedTag, 1);
        if (suffix is not null)
        {
            Operand(BeamWire.UnsignedTag, ConsHeapWords);
        }
        Operand(BeamWire.UnsignedTag, 1);
        EmitListParts(
            0,
            XRegisterTag,
            0,
            YRegisterTag,
            0
        );
        BeamSourceLocation? recursionLocation = null;
        if (argument is null)
        {
            if (suffix is not null)
            {
                Op(PutList, XRegisterTag, 0);
                LiteralOperand(suffix);
                Operand(XRegisterTag, 0);
            }

            EmitCallLine(mapped, location);
            Op(ExternalCall, BeamWire.UnsignedTag, 1);
            Operand(BeamWire.UnsignedTag, Import(mapped.Module!, mapped.Function, 1));
            if (CallLocation(mapped, location) != location)
            {
                recursionLocation = location;
            }
        }
        else
        {
            EmitConstant(argument, 1);
            EmitCallLine(mapped, location);
            EmitFieldCall(2, convert);
        }
        if (conversion is not null)
        {
            var previous = CallLocation(mapped, location);
            EmitCallLine(conversion, previous);
            Op(ExternalCall, BeamWire.UnsignedTag, 1);
            Operand(BeamWire.UnsignedTag, Import(ErlangModuleNames.Module, conversion.Function, 1));
            if (CallLocation(conversion, previous) != location)
            {
                recursionLocation = location;
            }
        }
        EmitMappingResult(entry, recursionLocation);
        EmitMappingEnd(empty, invalid);
    }

    private void EmitTailRemote(string module, string name, bool framed)
    {
        Op(framed ? TailExternalCallWithFrame : TailExternalCall, BeamWire.UnsignedTag, 1);
        Operand(BeamWire.UnsignedTag, Import(module, name, 1));
        if (framed)
        {
            Operand(BeamWire.UnsignedTag, 0);
        }
    }
}
