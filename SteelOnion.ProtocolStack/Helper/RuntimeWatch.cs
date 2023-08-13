using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SteelOnion.ProtocolStack.Helper
{
    internal class RuntimeWatch
    {
        string _name;
        long[] _records;
        int _count;
        Stopwatch _stopwatch;
        public RuntimeWatch(string name)
        {
            _name = name;
            _stopwatch = new Stopwatch();
            _records= new long[20];
        }
        public void Start()
        {
            _stopwatch.Restart();
            _count= 0;
        }
        public void Record() 
        {
            _records[_count]=_stopwatch.ElapsedTicks;
            _count++;
        }
        public void Stop()
        {
            _stopwatch.Stop();
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < _count; i++)
            {
                sb.AppendLine($"{_name}{i} {_records[i]}");
            }
            return sb.ToString();
        }
    }
}
