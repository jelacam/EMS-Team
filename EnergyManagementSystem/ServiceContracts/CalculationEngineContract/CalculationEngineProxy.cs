﻿//-----------------------------------------------------------------------
// <copyright file="CalculationEngineProxy.cs" company="EMS-Team">
//     Company copyright tag.
// </copyright>
//-----------------------------------------------------------------------

namespace EMS.ServiceContracts
{
    using System;
    using System.Collections.Generic;
    using System.ServiceModel;
    using CommonMeasurement;

    /// <summary>
    /// Class for ICalculationEngineContract and IDisposable implementation
    /// </summary>
    public class CalculationEngineProxy : ICalculationEngineContract, IDisposable
    {
        /// <summary>
        /// proxy object
        /// </summary>
        private static ICalculationEngineContract proxy;

        /// <summary>
        /// ChannelFactory object
        /// </summary>
        private static ChannelFactory<ICalculationEngineContract> factory;

        /// <summary>
        /// Gets or sets instance of ICalculationEngineContract
        /// </summary>
        public static ICalculationEngineContract Instance
        {
            get
            {
                if (proxy == null)
                {
                    factory = new ChannelFactory<ICalculationEngineContract>("*");
                    proxy = factory.CreateChannel();
                    IContextChannel cc = proxy as IContextChannel;
                }

                return proxy;
            }

            set
            {
                if (proxy == null)
                {
                    proxy = value;
                }
            }
        }

        /// <summary>
        /// Dispose method
        /// </summary>
        public void Dispose()
        {
            try
            {
                if (factory != null)
                {
                    factory = null;
                }
            }
            catch (CommunicationException ce)
            {
                Console.WriteLine("Communication exception: {0}", ce.Message);
            }
            catch (Exception e)
            {
                Console.WriteLine("CE proxy exception: {0}", e.Message);
            }
        }

		/// <summary>
		/// Optimization algorithm
		/// </summary>
		/// <param name="measEnergyConsumers">list of measurements for energyConsumers</param>
		/// <param name="measGenerators">list of measurements for generators</param>
		/// <param name="windSpeed">speed of wind</param>
		/// <param name="sunlight">sunlight percent</param>
		/// <returns>returns true if optimization was successful</returns>
		public bool OptimisationAlgorithm(List<MeasurementUnit> measEnergyConsumers, List<MeasurementUnit> measGenerators, float windSpeed, float sunlight)
        {
            return proxy.OptimisationAlgorithm(measEnergyConsumers,measGenerators, windSpeed, sunlight);
        }
    }
}