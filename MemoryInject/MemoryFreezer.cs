using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace MemoryEngine
{
    public class MemoryFreezer
    {
        private readonly Engine _engine;
        private readonly ConcurrentDictionary<IntPtr, int> _frozenAddresses = new();
        private CancellationTokenSource _cts;

        public MemoryFreezer(Engine engine)
        {
            _engine = engine;
        }

        public void Freeze(IntPtr address, int value)
        {
            _frozenAddresses[address] = value;

            if (_cts == null)
            {
                _cts = new CancellationTokenSource();
                Task.Run(() => FreezeLoop(_cts.Token));
            }
        }

        public void Unfreeze(IntPtr address)
        {
            _frozenAddresses.TryRemove(address, out _);
        }

        private void FreezeLoop(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                foreach (var kvp in _frozenAddresses)
                {
               
                    _engine.WriteInt(kvp.Key, kvp.Value);
                }

      
                Thread.Sleep(10);
            }
        }

        public void StopAll()
        {
            _cts?.Cancel();
            _cts = null;
            _frozenAddresses.Clear();
        }
    }
}