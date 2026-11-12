#!/usr/bin/env escript
-mode(compile).

main([Reference, Generated]) ->
    Targets = [
        {"lib/public_key-1.21.7/ebin/AttributeCertificateVersion1-2009.beam",
            dec_AttributeCertificateInfoV1_attributes_AttributeSet_values, 3},
        {"lib/public_key-1.21.7/ebin/CryptographicMessageSyntax-2009.beam",
            dec_AuthAttributes_Attribute_attrValues, 3},
        {"lib/public_key-1.21.7/ebin/CryptographicMessageSyntax-2009.beam",
            dec_EncryptedData_unprotectedAttrs_Attribute_attrValues, 3}
    ],
    lists:foreach(fun({Path, Function, Arity}) ->
        io:format("~s ~p/~p~n", [Path, Function, Arity]),
        lists:foreach(fun({Label, Base}) ->
            {beam_file, _, _, _, _, Functions} = beam_disasm:file(filename:join(Base, Path)),
            [Instructions] = [I || {function, N, A, _, I} <- Functions, N =:= Function, A =:= Arity],
            io:format("~s ~0p~n", [Label, Instructions])
        end, [{"OTP", Reference}, {"C#", Generated}])
    end, Targets).
