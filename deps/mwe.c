#include <stdint.h>
#include <stdio.h>

typedef void (*GC_finalizer_t)(void*, void*);

void* GC_malloc(int64_t size);
void* GC_gcollect(void);
void GC_register_finalizer(void* object, GC_finalizer_t fn, void* data, void*, void*);

void finalizer(void* obj, void* data)
{
    printf("finalizer called\n");
}

struct S
{
    struct S* self;
};

void test()
{
    struct S* s = (struct S*) GC_malloc(sizeof(struct S));

    // comment this line will cause the finalizers
    // to get called after 'GC_gcollect()'
    s->self = s;

    GC_register_finalizer(s, finalizer, NULL, NULL, NULL);
}

int main()
{
    for(int i = 0; i < 10000; i++)
        test();
    GC_gcollect();
    printf("done\n");
}