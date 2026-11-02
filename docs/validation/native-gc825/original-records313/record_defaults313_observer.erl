-module(record_defaults313_observer).
-export([run/1]).
run(NewBeam) ->
    OldFun = record_defaults313_owner:capture(),
    OldValue = record_defaults313_owner:make(),
    Before = record_defaults313_consumer:payload(record_defaults313_consumer:make()),
    {module,record_defaults313_owner} = code:load_binary(record_defaults313_owner,"record_defaults313_owner.erl",NewBeam),
    Current = record_defaults313_owner:payload(record_defaults313_owner:make()),
    External = record_defaults313_consumer:payload(record_defaults313_consumer:make()),
    Captured = record_defaults313_consumer:payload(OldFun()),
    Retained = record_defaults313_consumer:payload(OldValue),
    Explicit = record_defaults313_owner:payload(record_defaults313_owner:make_explicit({explicit,[value],<<7,8>>})),
    Definition = records:get_definition(record_defaults313_owner,r),
    {Before,Current,External,Captured,Retained,Explicit,Definition}.
