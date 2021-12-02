using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Leayal.ApplicationController
{
    class NextInstanceData
    {
        public readonly int ProcessId;
        public readonly string[] Arguments;

        public NextInstanceData(int procId, string[] args)
        {
            this.ProcessId = procId;
            this.Arguments = args;
        }
    }
}
