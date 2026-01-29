#if !NET9_0_OR_GREATER
using System.Runtime.CompilerServices;

namespace System.Threading;

public sealed class Lock
{
    private readonly object _lock = new();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Enter() => Monitor.Enter(_lock);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Exit() => Monitor.Exit(_lock);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryEnter() => Monitor.TryEnter(_lock);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryEnter(int millisecondsTimeout) => Monitor.TryEnter(_lock, millisecondsTimeout);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryEnter(TimeSpan timeout) => Monitor.TryEnter(_lock, timeout);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Scope EnterScope() => new(_lock);

    public readonly ref struct Scope : IDisposable
    {
        private readonly object _lock;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal Scope(object @lock)
        {
            _lock = @lock;
            Monitor.Enter(_lock);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Dispose() => Monitor.Exit(_lock);
    }
}
#endif