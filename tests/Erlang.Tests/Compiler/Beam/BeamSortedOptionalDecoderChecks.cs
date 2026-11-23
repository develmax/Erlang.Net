using System.Collections;
using System.Numerics;
using Erlang;
using Erlang.Compiler;
using static Erlang.Compiler.BeamEmissionFormat;
using static Erlang.Tests.BeamBinaryHostAccess;

namespace Erlang.Tests;

internal static class BeamSortedOptionalDecoderChecks
{
    internal const string TestName = "compiler/beam/retains-sorted-optional-decoder-remainders";
    private const string Module = "beam_sorted_optional_decoder";
    private const string Fixture = "beam_sorted_optional_decoder.erl";
    private const string WrongLayout = "The sorted decoder did not use its expected required and optional frame.";
    private const string WrongValue = "The sorted optional decoder changed an output or malformed input.";

    private const string Source = """
        -module(beam_sorted_optional_decoder).
        -export([decode/2, plain/2, required/2, prepared/2, prepared_optional/2, selected/2, select/1, standalone/2, terminal/2, retain_bytes/2]).
        decode(Input, Tags) ->
            Fields = prepare(Input, Tags),
            Rank = fun(Item) -> case Item of
                Pair = {19,_} -> {1,Pair};
                Pair = {20,_} -> {2,Pair};
                Other -> {3,Other}
            end end,
            Ranked = [Rank(V) || V <- Fields],
            Sorted = [Value || {_,Value} <- lists:sort(Ranked)],
            {First,R1} = case Sorted of
                [{19,A}|Tail1] -> {begin
                    Bin1 = read(A, []),
                    Len1 = byte_size(Bin1),
                    if 1 =< Len1, Len1 =< 2 -> binary_to_list(Bin1);
                       true -> exit({error,{format,first}})
                    end
                end,Tail1};
                _ -> {absent,Sorted}
            end,
            {Second,R2} = case R1 of
                [{20,B}|Tail2] -> {begin
                    Bin2 = read(B, []),
                    Len2 = byte_size(Bin2),
                    if 1 =< Len2, Len2 =< 3 -> binary_to_list(Bin2);
                       true -> exit({error,{format,second}})
                    end
                end,Tail2};
                _ -> {absent,R1}
            end,
            case R2 of [] -> true; _ -> exit({error,{format,{unexpected,R2}}}) end,
            Result = {fields,First,Second},
            Result.
        plain(Input, Tags) ->
            Fields = prepare(Input, Tags),
            Rank = fun(Item) -> case Item of
                Pair = {16,_} -> {1,Pair};
                Pair = {20,_} -> {2,Pair};
                Other -> {3,Other}
            end end,
            Ranked = [Rank(V) || V <- Fields],
            Sorted = [Value || {_,Value} <- lists:sort(Ranked)],
            {First,R1} = case Sorted of [{16,A}|Tail1] -> {unchanged(A, []),Tail1}; _ -> {absent,Sorted} end,
            {Second,R2} = case R1 of
                [{20,B}|Tail2] -> {begin
                    Bin2 = read(B, []),
                    Len2 = byte_size(Bin2),
                    if 1 =< Len2, Len2 =< 3 -> binary_to_list(Bin2);
                       true -> exit({error,{format,second}})
                    end
                end,Tail2};
                _ -> {absent,R1}
            end,
            case R2 of [] -> true; _ -> exit({error,{format,{unexpected,R2}}}) end,
            Result = {fields,First,Second},
            Result.
        required(Input, Tags) ->
            Fields = prepare(Input, Tags),
            Rank = fun(Item) -> case Item of
                Pair = {18,_} -> {1,Pair};
                Pair = {19,_} -> {2,Pair};
                Pair = {20,_} -> {3,Pair};
                Pair = {21,_} -> {4,Pair};
                Other -> {5,Other}
            end end,
            Ranked = [Rank(V) || V <- Fields],
            Sorted = [Value || {_,Value} <- lists:sort(Ranked)],
            [Head|Rest] = Sorted,
            Initial = begin
                Bin = read_required(Head, [18]),
                Len = byte_size(Bin),
                if 1 =< Len, Len =< 2 -> binary_to_list(Bin);
                   true -> exit({error,{format,required_value}})
                end
            end,
            {First,R1} = case Rest of
                [{19,A}|Tail1] -> {begin
                    Bin1 = read(A, []),
                    Len1 = byte_size(Bin1),
                    if 1 =< Len1, Len1 =< 2 -> binary_to_list(Bin1);
                       true -> exit({error,{format,first}})
                    end
                end,Tail1};
                _ -> {absent,Rest}
            end,
            {Second,R2} = case R1 of
                [{20,B}|Tail2] -> {begin
                    Bin2 = read(B, []),
                    Len2 = byte_size(Bin2),
                    if 1 =< Len2, Len2 =< 3 -> binary_to_list(Bin2);
                       true -> exit({error,{format,second}})
                    end
                end,Tail2};
                _ -> {absent,R1}
            end,
            {Third,R3} = case R2 of
                [{21,C}|Tail3] -> {begin
                    Bin3 = read(C, []),
                    Len3 = byte_size(Bin3),
                    if 1 =< Len3, Len3 =< 3 -> binary_to_list(Bin3);
                       true -> exit({error,{format,third}})
                    end
                end,Tail3};
                _ -> {absent,R2}
            end,
            case R3 of [] -> true; _ -> exit({error,{format,{unexpected,R3}}}) end,
            Result = {fields,Initial,First,Second,Third},
            Result.
        prepared(Input, Tags) ->
            Fields = prepare(Input, Tags),
            [Head|Rest] = Fields,
            Initial = begin
                Bin = read_required(Head, [18]),
                Len = byte_size(Bin),
                if 1 =< Len, Len =< 2 -> binary_to_list(Bin);
                   true -> exit({error,{format,required_value}})
                end
            end,
            {Optional,Tail} = case Rest of
                [{20,Payload}|Following] -> {begin
                    Bytes = read(Payload, []),
                    Count = byte_size(Bytes),
                    if 1 =< Count, Count =< 3 -> binary_to_list(Bytes);
                       true -> exit({error,{format,optional_value}})
                    end
                end,Following};
                _ -> {absent,Rest}
            end,
            case Tail of [] -> true; _ -> exit({error,{format,{unexpected,Tail}}}) end,
            Result = {fields,Initial,Optional},
            Result.
        prepared_optional(Input, Tags) ->
            Fields = prepare(Input, Tags),
            {First,Tail1} = case Fields of
                [{19,A}|Following1] -> {unchanged(A, []),Following1};
                _ -> {absent,Fields}
            end,
            {Second,Tail2} = case Tail1 of
                [{20,B}|Following2] -> {begin
                    Bytes = read(B, []),
                    Count = byte_size(Bytes),
                    if 1 =< Count, Count =< 3 -> binary_to_list(Bytes);
                       true -> exit({error,{format,optional_value}})
                    end
                end,Following2};
                _ -> {absent,Tail1}
            end,
            {Third,Tail3} = case Tail2 of
                [{21,C}|Following3] -> {unchanged(C, []),Following3};
                _ -> {absent,Tail2}
            end,
            case Tail3 of [] -> true; _ -> exit({error,{format,{unexpected,Tail3}}}) end,
            Result = {fields,First,Second,Third},
            Result.
        selected(Input, Tags) ->
            Fields = prepare(Input, Tags),
            [Key|Tail1] = Fields,
            Identifier = begin
                Number = read_integer(Key, []),
                if 0 =< Number, Number =< 8 -> Number;
                   true -> exit({error,{format,key_range}})
                end
            end,
            [Payload|Tail2] = Tail1,
            Raw = unchanged(Payload, []),
            Callback = beam_sorted_optional_decoder:select(Identifier),
            Value = try Callback(field, Raw, [])
                catch Class:Reason when Class =:= error; Class =:= exit ->
                    exit({selected_error,Reason})
                end,
            case Tail2 of [] -> true; _ -> exit({error,{format,{unexpected,Tail2}}}) end,
            Result = {fields,Identifier,Value},
            Result.
        standalone(Input, Tags) ->
            List = prepare(Input, Tags),
            [unchanged(V, []) || V <- List].
        terminal(Input, Tags) ->
            begin
                Bytes = read(Input, Tags),
                Count = byte_size(Bytes),
                if 1 =< Count, Count =< 3 -> binary_to_list(Bytes);
                   true -> exit({error,{format,optional_value}})
                end
            end.
        retain_bytes(Input, Tags) ->
            begin
                Bytes = read(Input, Tags),
                Count = byte_size(Bytes),
                if 1 =< Count, Count =< 3 -> Bytes;
                   true -> exit({error,{format,optional_value}})
                end
            end.
        select(5) -> fun selected_payload/3;
        select(_) -> fun(_, Reason, _) -> throw(Reason) end.
        selected_payload(field, fail, []) -> error(failed);
        selected_payload(field, Value, []) -> Value.
        read_integer(Value, Tags) ->
            Buf = prepare(Value, Tags),
            Width = byte_size(Buf),
            <<Number:Width/signed-unit:8>> = Buf,
            Number.
        read_required({Tag, Value}, [Tag]) -> iolist_to_binary(Value).
        prepare(Input, []) -> Input.
        read(Value, []) -> iolist_to_binary(Value).
        unchanged(Value, []) -> Value.
        """;

    internal static async Task Run()
    {
        byte[] image = CodeGeneration.CompileBeamModule(Source, Fixture);
        byte[] unproven = CodeGeneration.CompileBeamModule(
            Source.Replace("Other -> {3,Other}", "_ -> different_shape", StringComparison.Ordinal)
                .Replace("Other -> {5,Other}", "_ -> different_shape", StringComparison.Ordinal)
                .Replace("    Number.", "    Buf.", StringComparison.Ordinal),
            Fixture
        );
        var module = BeamModule.Prepare(image, new BeamDependencySelection(["lists", Module]));
        var selection = new RuntimeLibrarySelection(
            [RuntimeModuleSelection.Managed("erlang"), RuntimeModuleSelection.Managed("lists")]
        );
        await using var runtime = await ProcessRuntime.CreateAsync(selection, RuntimeNativePrimitives.InternalScalars413, CancellationToken.None);
        runtime.Modules.Register(
            Module,
            "select",
            1,
            (context, args) => module.Invoke(context, "select", args[0])
        );
        await using var actor = new RuntimeNodeFoundationHostActor(runtime);
        await actor.InvokeAsync(async context =>
        {
            Term absent = Term.A("absent");
            Term first = Term.List(Term.I(65), Term.I(66));
            Term second = Term.List(Term.I(67), Term.I(68), Term.I(69));
            Term left = Field(19, first);
            Term right = Field(20, second);
            await Check(
                "decode",
                Term.List(right, left),
                first,
                second
            );
            await Check(
                "plain",
                Term.List(right, Field(16, first)),
                first,
                second
            );
            await Check(
                "decode",
                Nil.Value,
                absent,
                absent
            );
            await Check(
                "decode",
                Term.List(right),
                absent,
                second
            );
            Term initial = Term.List(Term.I(70));
            Term requiredFields = Term.List(right, Field(18, initial), left);
            Term expected = Term.Tuple(
                Term.A("fields"),
                initial,
                first,
                second,
                absent
            );
            if (!expected.Equals(await module.Invoke(
                context,
                "required",
                requiredFields,
                Nil.Value
            )))
            {
                throw new InvalidOperationException(WrongValue);
            }

            try
            {
                await module.Invoke(
                    context,
                    "required",
                    Nil.Value,
                    Nil.Value
                );
                throw new InvalidOperationException(WrongValue);
            }
            catch (ErlangException error) when (error.ExceptionClass == ErlangExceptionClasses.Error
                && error.Reason.Equals(Term.Tuple(Term.A("badmatch"), Nil.Value)))
            {
            }

            await Reject(Term.List(Field(19, Nil.Value), Field(20, Nil.Value)), Reason(Term.A("first")));
            await Reject(Term.List(left, Field(20, Nil.Value)), Reason(Term.A("second")));
            Term extra = Field(21, Term.A("extra"));
            Term prepared = Field(18, initial);
            Term nonlist = Term.A("nonlist");
            await Check(
                "prepared",
                Term.List(prepared, right),
                initial,
                second
            );
            await Check(
                "prepared",
                Term.List(prepared),
                initial,
                absent
            );
            await RejectPrepared(
                "prepared",
                nonlist,
                ErlangExceptionClasses.Error,
                Term.Tuple(Term.A("badmatch"), nonlist)
            );
            await RejectPrepared(
                "prepared",
                Term.List(prepared, Field(20, Nil.Value)),
                ErlangExceptionClasses.Exit,
                Reason(Term.A("optional_value"))
            );
            await RejectPrepared(
                "prepared",
                Term.List(prepared, extra),
                ErlangExceptionClasses.Exit,
                Reason(Term.Tuple(Term.A("unexpected"), Term.List(extra)))
            );
            Term tag21 = Field(21, initial);
            Term layoutValue = Term.Tuple(
                Term.A("fields"),
                first,
                second,
                initial
            );
            if (!layoutValue.Equals(await module.Invoke(
                context,
                "prepared_optional",
                Term.List(left, right, tag21),
                Nil.Value
            )))
            {
                throw new InvalidOperationException(WrongValue);
            }

            await RejectPrepared(
                "prepared_optional",
                Term.List(left, Field(20, Nil.Value), tag21),
                ErlangExceptionClasses.Exit,
                Reason(Term.A("optional_value"))
            );
            Term selectedKey = new BitString([5]);
            await Check(
                "selected",
                Term.List(selectedKey, initial),
                Term.I(5),
                initial
            );
            Term mapped = Term.List(initial, first);
            if (!mapped.Equals(await module.Invoke(
                context,
                "standalone",
                mapped,
                Nil.Value
            )) || !first.Equals(await module.Invoke(
                context,
                "terminal",
                first,
                Nil.Value
            )))
            {
                throw new InvalidOperationException(WrongValue);
            }

            await RejectPrepared(
                "selected",
                Term.List(selectedKey, Term.A("fail")),
                ErlangExceptionClasses.Exit,
                Term.Tuple(Term.A("selected_error"), Term.A("failed"))
            );
            await RejectPrepared(
                "selected",
                Term.List(new BitString([4]), initial),
                ErlangExceptionClasses.Throw,
                initial
            );
            await RejectPrepared(
                "selected",
                Term.List(new BitString([9]), initial),
                ErlangExceptionClasses.Exit,
                Reason(Term.A("key_range"))
            );
            await RejectPrepared(
                "selected",
                Term.List(selectedKey),
                ErlangExceptionClasses.Error,
                Term.Tuple(Term.A("badmatch"), Nil.Value)
            );
            await Reject(Term.List(right, extra, left), Reason(Term.Tuple(Term.A("unexpected"), Term.List(extra))));

            return Nil.Value;

            async Task Check(
                string entry,
                Term input,
                Term a,
                Term b
            )
            {
                if (!Term.Tuple(Term.A("fields"), a, b).Equals(await module.Invoke(
                    context,
                    entry,
                    input,
                    Nil.Value
                )))
                {
                    throw new InvalidOperationException(WrongValue);
                }
            }

            async Task RejectPrepared(
                string entry,
                Term input,
                string exceptionClass,
                Term expected
            )
            {
                try
                {
                    await module.Invoke(
                        context,
                        entry,
                        input,
                        Nil.Value
                    );
                }
                catch (ErlangException error) when (error.ExceptionClass == exceptionClass && error.Reason.Equals(expected))
                {
                    return;
                }

                throw new InvalidOperationException(WrongValue);
            }

            async Task Reject(Term input, Term expected)
            {
                try
                {
                    await module.Invoke(
                        context,
                        "decode",
                        input,
                        Nil.Value
                    );
                }
                catch (ErlangException error) when (error.ExceptionClass == ErlangExceptionClasses.Exit && error.Reason.Equals(expected))
                {
                    return;
                }

                throw new InvalidOperationException(WrongValue);
            }
        });

        if (Frame(image, "decode") != 3 || Frame(image, "plain") != 3 || Frame(unproven, "decode") <= 3
            || Frame(image, "required", requireNilFailure: true) != 5 || Frame(unproven, "required") <= 5
            || Frame(image, "prepared", requireRegisterFailure: true) != 3
            || Frame(image, "prepared_optional") != 4 || Frame(image, "selected", requireRegisterFailure: true) != 4
            || Frame(unproven, "selected") <= 4
            || MeasurementUnit(image, "terminal") != BitSyntaxDefaults.BinaryUnit
            || MeasurementUnit(image, "retain_bytes") != BitSyntaxDefaults.BitStringUnit
            || !HasOrderedHelpers(image))
        {
            throw new InvalidOperationException(WrongLayout);
        }
    }

    private static Term Field(int tag, Term value) => Term.Tuple(Term.I(tag), value);

    private static Term Reason(Term detail) => Term.Tuple(Term.A("error"), Term.Tuple(Term.A("format"), detail));

    private static int Frame(
        byte[] image,
        string name,
        bool requireNilFailure = false,
        bool requireRegisterFailure = false
    )
    {
        object code = Code(image);
        string[] atoms = Items(Property(code, "Structure"), "Atoms").Cast<string>().ToArray();
        object[] instructions = Items(code, "Instructions");
        int start = Array.FindIndex(
            instructions,
            instruction => Opcode(instruction) == FunctionInfo
                && Items(instruction, "Operands") is [_, var function, var arity]
                && atoms[Value(function)] == name && Value(arity) == 2
        );
        if (start < 0)
        {
            throw new InvalidOperationException(WrongLayout);
        }

        var body = instructions.Skip(start + 1).TakeWhile(instruction => Opcode(instruction) != FunctionInfo).ToArray();
        if (requireNilFailure && !body.Any(instruction => Opcode(instruction) == BadMatch
            && Items(instruction, "Operands") is [var failed]
            && Equals(Property(failed, "Kind"), "nil") && Value(failed) == NilAtomIndex))
        {
            throw new InvalidOperationException(WrongLayout);
        }

        if (requireRegisterFailure && !body.Any(instruction => Opcode(instruction) == BadMatch
            && Items(instruction, "Operands") is [var failed]
            && Equals(Property(failed, "Kind"), "x") && Value(failed) == 0))
        {
            throw new InvalidOperationException(WrongLayout);
        }

        object frame = body.Single(instruction => Opcode(instruction) == Allocate);

        return Value(Items(frame, "Operands")[0]);
    }

    private static int? MeasurementUnit(byte[] image, string name)
    {
        object code = Code(image);
        string[] atoms = Items(Property(code, "Structure"), "Atoms").Cast<string>().ToArray();
        object[] instructions = Items(code, "Instructions");
        int start = Array.FindIndex(
            instructions,
            instruction => Opcode(instruction) == FunctionInfo
                && Items(instruction, "Operands") is [_, var function, var arity]
                && atoms[Value(function)] == name && Value(arity) == 2
        );
        if (start < 0)
        {
            throw new InvalidOperationException(WrongLayout);
        }

        var body = instructions.Skip(start + 1).TakeWhile(instruction => Opcode(instruction) != FunctionInfo);
        object measured = body.Single(instruction => Opcode(instruction) == GcBifOne);
        if (Property(Items(measured, "Operands")[3], "TypeIndex") is not int index)
        {
            return null;
        }

        object[] types = Items(Property(code, "Types"), "Entries");

        return (byte)Property(types[index], "SizeUnit");
    }

    private static bool HasOrderedHelpers(byte[] image)
    {
        object code = Code(image);
        string[] atoms = Items(Property(code, "Structure"), "Atoms").Cast<string>().ToArray();
        string[] names = Items(code, "Instructions")
            .Where(instruction => Opcode(instruction) == FunctionInfo)
            .Select(instruction => atoms[Value(Items(instruction, "Operands")[1])])
            .ToArray();
        int anonymous = Array.IndexOf(names, "-select/1-fun-0-");
        int mapping = Array.IndexOf(names, "-standalone/2-lc$^0/1-0-");

        return anonymous >= 0 && anonymous < mapping;
    }

    private static byte Opcode(object instruction) => (byte)Property(Property(instruction, "Definition"), "Opcode");

    private static int Value(object operand) => (int)(BigInteger)Property(operand, "Value");

    private static object[] Items(object owner, string name) => ((IEnumerable)Property(owner, name)).Cast<object>().ToArray();
}
