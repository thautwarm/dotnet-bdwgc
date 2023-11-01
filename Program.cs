using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;

public static class Memory
{
    [DllImport("libgc", CallingConvention = CallingConvention.Cdecl)]
    static unsafe extern void* GC_malloc(long size);

    [DllImport("libgc", CallingConvention = CallingConvention.Cdecl)]
    static unsafe extern void* GC_malloc_atomic(long size);
    [DllImport("libgc", CallingConvention = CallingConvention.Cdecl)]
    static unsafe extern void* GC_malloc_uncollectable(long size);

    [DllImport("libgc", CallingConvention = CallingConvention.Cdecl)]
    static unsafe extern void GC_register_finalizer(void* obj, delegate* unmanaged[Cdecl]<void*, void*, void> fn, void* client_data, void* _1, void* _2);

    [DllImport("libgc", CallingConvention = CallingConvention.Cdecl)]
    static unsafe extern void GC_gcollect();
    [DllImport("libgc", CallingConvention = CallingConvention.Cdecl)]
    static unsafe extern long GC_get_total_bytes();
    [DllImport("libgc", CallingConvention = CallingConvention.Cdecl)]
    static unsafe extern void GC_init();
    [DllImport("libgc", CallingConvention = CallingConvention.Cdecl)]
    static unsafe extern void GC_enable_incremental();


    public static void Collect() => GC_gcollect();
    public static long TotalBytes => GC_get_total_bytes();
    public static unsafe T* Malloc<T>() where T : unmanaged => (T*)GC_malloc(sizeof(T));
    public static unsafe T* MallocAtomic<T>(long size) where T : unmanaged => (T*)GC_malloc_atomic(size);
    public static unsafe T* MallocUncollectable<T>(long size) where T : unmanaged => (T*)GC_malloc_uncollectable(size);
    public static unsafe void RegisterFinalizer(void* obj, Action fn)
    {
        lock (finalizers)
        {
            if (unusedSlots.Count > 0)
            {
                var slot = unusedSlots.Pop();
                finalizers[slot] = fn;
                GC_register_finalizer(obj, &GenericFinalizer, (void*)(long)slot, null, null);
            }
            else
            {
                finalizers.Add(fn);
                GC_register_finalizer(obj, &GenericFinalizer, (void*)(long)(finalizers.Count - 1), null, null);
            }
        }
    }



    static readonly List<Action?> finalizers = new();
    static readonly Stack<int> unusedSlots = new();

    public static void Init()
    {
        finalizers.Clear();
        unusedSlots.Clear();
        GC_init();
        GC_enable_incremental();
    }

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
    public static unsafe void GenericFinalizer(void* obj, void* client_data)
    {
        var fnId = (int)(long)client_data;

        lock (finalizers)
        {
            if (finalizers.Count > fnId)
            {
                finalizers[fnId]?.Invoke();
                finalizers[fnId] = null;
                unusedSlots.Push(fnId);
            }
        }
    }

}

public static unsafe class MyApp
{

    public struct S
    {
        public int x;
        public int y;
    }

    public struct X
    {
        public int x;
        public int y;
        public S* s;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void Test(int i)
    {
        var s = Memory.Malloc<S>();
        s->x = 2;
        s->x = 3;

        Memory.RegisterFinalizer(s, () =>
        {
            Console.WriteLine($"Finalizer called {i}");
        });
    }

    public struct Circ
    {
        public int x;
        public Circ* selfRef;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void TestCircRef(int i)
    {
        var c = Memory.Malloc<Circ>();
        c->selfRef = c;
        c->x = 0;

        Memory.RegisterFinalizer(c, () =>
        {
            Console.WriteLine($"Finalizing circular reference: {i}");
        });
    }

    public static void Main()
    {
        Memory.Init();

        for (int i = 0; i < 20; i++)
            Test(i);

        Console.WriteLine($"GC_get_total_bytes(): {Memory.TotalBytes}");

        for (int i = 0; i < 10000; i++)
        {
            TestCircRef(i);
        }
        Console.WriteLine($"GC_get_total_bytes(): {Memory.TotalBytes}");

        // it seems that circular references are not well handled?
        Memory.Collect();

        // wait for finalizers to run
        // Thread.Sleep(10000);
    }
}
