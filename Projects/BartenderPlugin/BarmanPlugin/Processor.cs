using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BeOpen.BartenderPlugin.API.ApiProcessor;

namespace BeOpen.BartenderPlugin.Processor
{
    class BartenderProcessor:IDisposable
    {
        private ApiProcessor bartenderProcessor = new ApiProcessor();
        public BartenderProcessor()
        {
        }
        public void Dispose()
        {
        }
    }
}
