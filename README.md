AOT-Compatlyzer
===============

Transforms .NET DLLs for use in AOT (for use on iOS platforms, including Unity3D.)  Fixes Unity's .NET 3.5 event add/remove code and a workaround for generic virtual methods. 

STATUS
======

My status: This tool only addresses some of the issues in AOT compatibility.  Unfortunately, despite putting in a lot of time, I have not been able to successfully port all of my own codebase to Unity3D's Mono.  There are issues with generic interface dispatch, including calls to foreach(IEnumerable&lt;T&gt;).  I like generic interfaces and foreach and have a lot of both, so I appear to be somewhat doomed until I can make a comprehensive sweep of my codebase (and hope I have covered all the issues, which is still a gamble.)  

So, I have put my iOS project on hold and am instead targeting PC (a non-Unity3D project), Mac, and Android first.  Perhaps by then Unity3D will have a newer version of mono that is less broken, or perhaps another viable cross platform C# engine will exist by then.

PREREQUISITES
=============

Mono.Cecil.dll  (DLL version 0.9.4.0, from package Cecil 2.10.9)

What it does
============

Bringing a .NET codebase onto iOS via Mono?  (Perhaps via Unity3D?)  This tool can help ease the pain!

### The Events CompareExchange problem
----------------------------------

Unity 3.5 (and I believe 4) ships with a modified version of Mono 2.6.5 that spits out Interlocked.CompareExchange<>() virtual methods inside of auto-generated add_/remove_ properties for C# events.  This is a problem in AOT code.  AOT-Compatlyzer replaces this autogenerated code with a version that uses an alternate implementation.

 WARNING: Currently, I do not add the Synchronized attribute to the new add_/remove_ methods, which should be present, according to the example code I was working from.
 
 UPDATE: There is a better fix that preserves thread safety, using the version of CompareExchange/Exchange that accepts parameters of objects instead of value types.  For details see [Issue 1](https://github.com/jaredthirsk/AOT-Compatlyzer/issues/1)

### The Virtual Methods problem
---------------------------

AOT does not support virtual methods.  

AOTCompatlyzer introduces some workarounds for some scenarios:

    #define AOT   // Comment this to hide code only needed for AOT-only mode

    class Abc 
    {
        // Compiles, but throws ExecutionEngineException in AOT-only mode if invoked!
        T MyMethod<T>(int p1, int p2) { …  }

    #if AOT // AOTCompatlyzer will detect and use this instead!
        object MyMethod(int p1, int p2, Type T) { …  }
    #endif

        void Run()
        {
            ...
            T myObj = MyMethod<T>(1, 2);
            ...
        }
    }

AOTCopatlyzer will replace Run's MyMethod invocation with:

    void Run() //
    {
        ...
        T myObj = (T) MyMethod(1, 2, typeof(T));
        ...
    }

These cases are also supported:
  - both methods return void
  - both method return same type (in this case, casting to T is not necessary).

#### LIMITATIONS:
  - Only one generic parameter is supported: i.e. MyMethod&lt;T&gt; and not MyMethod&lt;T1,T2&gt;.  (TODO: This could be easily supported.)

### Other problems
--------------

 I am not finished porting my codebase to an AOT-only environment yet.  As I run into more problems I will either adapt my codebase, or extend this utility to morph my DLL to something that works in AOT-only mode.

 - I believe Action&lt;T1,T2&gt; events fail.

