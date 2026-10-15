#define STATIC_ERLANG_NIF
#include "erl_nif.h"
#include <stddef.h>

int main(void)
{
    printf("{\"PointerBytes\":%zu,\"LongBytes\":%zu,\"TermBytes\":%zu,", sizeof(void*), sizeof(long), sizeof(ERL_NIF_TERM));
    printf("\"EntryBytes\":%zu,\"FunctionBytes\":%zu,\"BinaryBytes\":%zu,\"CallbackBytes\":%zu,", sizeof(ErlNifEntry), sizeof(ErlNifFunc), sizeof(ErlNifBinary), sizeof(TWinDynNifCallbacks));
    printf("\"EntryOffsets\":{");
#define ENTRY_FIELD(NAME) printf("\"" #NAME "\":%zu,", offsetof(ErlNifEntry, NAME))
    ENTRY_FIELD(major);
    ENTRY_FIELD(minor);
    ENTRY_FIELD(name);
    ENTRY_FIELD(num_of_funcs);
    ENTRY_FIELD(funcs);
    ENTRY_FIELD(load);
    ENTRY_FIELD(reload);
    ENTRY_FIELD(upgrade);
    ENTRY_FIELD(unload);
    ENTRY_FIELD(vm_variant);
    ENTRY_FIELD(options);
    ENTRY_FIELD(sizeof_ErlNifResourceTypeInit);
#undef ENTRY_FIELD
    printf("\"min_erts\":%zu},", offsetof(ErlNifEntry, min_erts));
    printf("\"FunctionOffsets\":{\"name\":%zu,\"arity\":%zu,\"fptr\":%zu,\"flags\":%zu},", offsetof(ErlNifFunc, name), offsetof(ErlNifFunc, arity), offsetof(ErlNifFunc, fptr), offsetof(ErlNifFunc, flags));
    printf("\"CallbackOffsets\":{");
#define ERL_NIF_API_FUNC_DECL(RET, NAME, ARGS) printf("\"" #NAME "\":%zu,", offsetof(TWinDynNifCallbacks, NAME))
#include "erl_nif_api_funcs.h"
#undef ERL_NIF_API_FUNC_DECL
    printf("\"erts_alc_test\":%zu,\"erts_internal_test_ptr\":%zu}}\n", offsetof(TWinDynNifCallbacks, erts_alc_test), offsetof(TWinDynNifCallbacks, erts_internal_test_ptr));
    return 0;
}
