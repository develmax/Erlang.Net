#include <stddef.h>
#include <stdio.h>
#include "erl_nif.h"
int main(void)
{
    printf("{\"PointerBytes\":%zu,\"LongBytes\":%zu,\"EntryBytes\":%zu,\"FunctionBytes\":%zu,\"BinaryBytes\":%zu,\"EventBytes\":%zu,\"WindowsCallbacksBytes\":%zu,", sizeof(void *), sizeof(long), sizeof(ErlNifEntry), sizeof(ErlNifFunc), sizeof(ErlNifBinary), sizeof(ErlNifEvent), sizeof(TWinDynNifCallbacks));
    printf("\"NameOffset\":%zu,\"FunctionsOffset\":%zu,\"LoadOffset\":%zu,\"VariantOffset\":%zu,\"ResourceInitOffset\":%zu,\"MinimumErtsOffset\":%zu,", offsetof(ErlNifEntry, name), offsetof(ErlNifEntry, funcs), offsetof(ErlNifEntry, load), offsetof(ErlNifEntry, vm_variant), offsetof(ErlNifEntry, sizeof_ErlNifResourceTypeInit), offsetof(ErlNifEntry, min_erts));
    printf("\"ResourceInitBytes\":%zu,\"MapIteratorBytes\":%zu,\"PidBytes\":%zu,\"PortBytes\":%zu,\"MonitorBytes\":%zu,\"TimeBytes\":%zu}\n", sizeof(ErlNifResourceTypeInit), sizeof(ErlNifMapIterator), sizeof(ErlNifPid), sizeof(ErlNifPort), sizeof(ErlNifMonitor), sizeof(ErlNifTime));
    return 0;
}
