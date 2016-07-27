using System;
using System.Collections.Generic;
using System.Fabric;
using Microsoft.ServiceFabric.Services.Communication.Runtime;
using Microsoft.ServiceFabric.Services.Runtime;
using CryptBitLibrary;

namespace CryptBitWeb
{
    /// <summary>
    /// The FabricRuntime creates an instance of this class for each service type instance. 
    /// </summary>
    internal sealed class CryptBitWeb : StatelessService
    {
        public CryptBitWeb(StatelessServiceContext context)
            : base(context)
        {

           var configPkg = context.CodePackageActivationContext.GetConfigurationPackageObject("Config");

            foreach (var setting in configPkg.Settings.Sections["CryptBitConfig"].Parameters)
            {
                CommonHelper.SetSetting(setting.Name, setting.Value);
            }

        }

        /// <summary>
        /// Optional override to create listeners (like tcp, http) for this service instance.
        /// </summary>
        /// <returns>The collection of listeners.</returns>
        protected override IEnumerable<ServiceInstanceListener> CreateServiceInstanceListeners()
        {
            return new ServiceInstanceListener[]
            {
                new ServiceInstanceListener(serviceContext => new OwinCommunicationListener(Startup.ConfigureApp, serviceContext, ServiceEventSource.Current, "ServiceEndpoint"))
            };
        }
    }
}
