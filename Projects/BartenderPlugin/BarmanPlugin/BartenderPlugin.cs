using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting;
using Resto.Front.Api.V5;
using Resto.Front.Api.V5.Attributes;
using Resto.Front.Api.V5.Attributes.JetBrains;
using BeOpen.BartenderPlugin.Processor;



namespace BeOpen.BartenderPlugin
{
    [UsedImplicitly]
    [PluginLicenseModuleId(21005108)]
    public sealed class BartenderPlugin : IFrontPlugin
    {
        private readonly Stack<IDisposable> subscriptions = new Stack<IDisposable>();

        public BartenderPlugin()
        {
            var bartenderProcessor = new BartenderProcessor();
            subscriptions.Push(bartenderProcessor);
        }

        public void Dispose()
        {
            while (subscriptions.Any())
            {
                var subscription = subscriptions.Pop();
                try
                {
                    subscription.Dispose();
                }
                catch (RemotingException)
                {
                    // nothing to do with the lost connection
                }
            }

            PluginContext.Log.Info("BarmanPlugin stopped");
        }
    }
}