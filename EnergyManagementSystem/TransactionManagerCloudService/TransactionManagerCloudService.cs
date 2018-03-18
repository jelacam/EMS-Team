﻿using System;
using System.Collections.Generic;
using System.Fabric;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CloudCommon;
using EMS.Common;
using EMS.ServiceContracts;
using EMS.Services.TransactionManagerService;
using Microsoft.ServiceFabric.Services.Communication.Runtime;
using Microsoft.ServiceFabric.Services.Communication.Wcf.Runtime;
using Microsoft.ServiceFabric.Services.Remoting.Runtime;
using Microsoft.ServiceFabric.Services.Runtime;

namespace TransactionManagerCloudService
{
    /// <summary>
    /// An instance of this class is created for each service instance by the Service Fabric runtime.
    /// </summary>
    internal sealed class TransactionManagerCloudService : StatelessService, IImporterAsyncContract
    {
        private TransactionManager transactionManager = null;

        public TransactionManagerCloudService(StatelessServiceContext context)
            : base(context)
        {
            transactionManager = new TransactionManager();
        }

        /// <summary>
        /// Optional override to create listeners (e.g., TCP, HTTP) for this service replica to handle client or user requests.
        /// </summary>
        /// <returns>A collection of listeners.</returns>
        protected override IEnumerable<ServiceInstanceListener> CreateServiceInstanceListeners()
        {
            return new List<ServiceInstanceListener>
            {
                new ServiceInstanceListener(context => this.CreateTransactionManagerListener(context), "TransactionManagerEndpoint"),
                new ServiceInstanceListener(context => this.CreateServiceRemotingListener(context), "TransactionManagerAsyncEndpoint")
            };
        }

        private ICommunicationListener CreateTransactionManagerListener(StatelessServiceContext context)
        {
            var listener = new WcfCommunicationListener<IImporterContract>(
                    listenerBinding: Binding.CreateCustomNetTcp(),
                    endpointResourceName: "TransactionManagerEndpoint",
                    serviceContext: context,
                    wcfServiceObject: new TransactionManager()
                );
            return listener;
        }

        /// <summary>
        /// This is the main entry point for your service instance.
        /// </summary>
        /// <param name="cancellationToken">Canceled when Service Fabric needs to shut down this service instance.</param>
        protected override async Task RunAsync(CancellationToken cancellationToken)
        {
            // TODO: Replace the following sample code with your own logic
            //       or remove this RunAsync override if it's not needed in your service.

            long iterations = 0;

            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();

                //ServiceEventSource.Current.ServiceMessage(this.Context, "Working-{0}", ++iterations);

                await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
            }
        }

        public Task<UpdateResult> ModelUpdate(Delta delta)
        {
            return Task.FromResult(transactionManager.ModelUpdate(delta));
        }
    }
}