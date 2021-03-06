using EMS.Common;
using EMS.CommonMeasurement;
using EMS.ServiceContracts;
using EMS.Services.NetworkModelService.DataModel.Core;
using EMS.Services.NetworkModelService.DataModel.Meas;
using EMS.Services.NetworkModelService.DataModel.Production;
using EMS.Services.NetworkModelService.DataModel.Wires;
using System;
using System.Collections.Generic;
using System.Threading;

namespace UIClient.ViewModel
{
	public class MainWindowViewModel : ViewModelBase
	{

		private Dictionary<long, IdentifiedObject> nmsModelMap;

		private object lockObj = new object();

		private DashboardViewModel dashboardViewModel;
		private AlarmSummaryViewModel alarmSummaryViewModel;
        private HistoryViewModel historyViewModel;

        public DockManagerViewModel DockManagerViewModel { get; private set; }

		public MainWindowViewModel()
		{
			InitiateIntegrityUpdate();

			var documents = new List<ViewModelBase>();

			DashboardViewModel = new DashboardViewModel();
			DashboardViewModel.Title = "Dashboard";
			AlarmSummaryViewModel = new AlarmSummaryViewModel();
			AlarmSummaryViewModel.Title = "Alarm Summary";

            HistoryViewModel = new HistoryViewModel() { Title = "History" };


            documents.Add(DashboardViewModel);
			documents.Add(new NMSViewModel(new View.NMSView()) { Title = "NMS" });
			documents.Add(new ImporterViewModel() { Title = "Importer" });
			documents.Add(HistoryViewModel);
			documents.Add(new DMSOptionsViewModel(new View.DMSOptionsView()) { Title = "DMS" });
			documents.Add(AlarmSummaryViewModel);

			this.DockManagerViewModel = new DockManagerViewModel(documents);
		}

		/// <summary>
		/// Implements integrity update logic for scada cr component
		/// </summary>
		/// <returns></returns>
		public bool InitiateIntegrityUpdate()
		{
			//lock (lockObj)
			{
				Thread.Sleep(5000); //just for testing, remove
				NmsModelMap = new Dictionary<long, IdentifiedObject>();
				ModelResourcesDesc modelResourcesDesc = new ModelResourcesDesc();

				List<ModelCode> properties = new List<ModelCode>(10);
				ModelCode modelCodeEmsFuel = ModelCode.EMSFUEL;
				ModelCode modelCodeAnalog = ModelCode.ANALOG;
                ModelCode modelCodeSynchronousMachine = ModelCode.SYNCHRONOUSMACHINE;
				ModelCode modelCodeEnergyConsumer = ModelCode.ENERGYCONSUMER;

				int iteratorId = 0;
				int resourcesLeft = 0;
				int numberOfResources = 2;
				string message = string.Empty;

				List<ResourceDescription> retList = new List<ResourceDescription>(5);

				#region getting SynchronousMachine
				try
				{
					// first get all synchronous machines from NMS
					properties = modelResourcesDesc.GetAllPropertyIds(modelCodeSynchronousMachine);

					iteratorId = NetworkModelGDAProxy.Instance.GetExtentValues(modelCodeSynchronousMachine, properties);
					resourcesLeft = NetworkModelGDAProxy.Instance.IteratorResourcesLeft(iteratorId);

					while (resourcesLeft > 0)
					{
						List<ResourceDescription> rds = NetworkModelGDAProxy.Instance.IteratorNext(numberOfResources, iteratorId);
						retList.AddRange(rds);
						resourcesLeft = NetworkModelGDAProxy.Instance.IteratorResourcesLeft(iteratorId);
					}
					NetworkModelGDAProxy.Instance.IteratorClose(iteratorId);

				}
				catch (Exception e)
				{
					message = string.Format("Getting extent values method failed for {0}.\n\t{1}", modelCodeSynchronousMachine, e.Message);
					Console.WriteLine(message);
					CommonTrace.WriteTrace(CommonTrace.TraceError, message);

					Console.WriteLine("Trying again...");
					CommonTrace.WriteTrace(CommonTrace.TraceError, "Trying again...");
					NetworkModelGDAProxy.Instance = null;
					Thread.Sleep(1000);
					InitiateIntegrityUpdate();
				}

				foreach (var resDesc in retList)
				{
					if (NmsModelMap.ContainsKey(resDesc.Id))
					{
						NmsModelMap[resDesc.Id] = ResourcesDescriptionConverter.ConvertTo<SynchronousMachine>(resDesc);
					}
					else
					{
						NmsModelMap.Add(resDesc.Id, ResourcesDescriptionConverter.ConvertTo<SynchronousMachine>(resDesc));
					}
				}

				// clear retList for getting new model from NMS
				retList.Clear();

				properties.Clear();
				iteratorId = 0;
				resourcesLeft = 0;
				#endregion
				#region getting EMSFuel
				try
				{
					// second get all ems fuels from NMS
					properties = modelResourcesDesc.GetAllPropertyIds(modelCodeEmsFuel);

					iteratorId = NetworkModelGDAProxy.Instance.GetExtentValues(modelCodeEmsFuel, properties);
					resourcesLeft = NetworkModelGDAProxy.Instance.IteratorResourcesLeft(iteratorId);

					while (resourcesLeft > 0)
					{
						List<ResourceDescription> rds = NetworkModelGDAProxy.Instance.IteratorNext(numberOfResources, iteratorId);
						retList.AddRange(rds);
						resourcesLeft = NetworkModelGDAProxy.Instance.IteratorResourcesLeft(iteratorId);
					}
					NetworkModelGDAProxy.Instance.IteratorClose(iteratorId);

				}
				catch (Exception e)
				{
					message = string.Format("Getting extent values method failed for {0}.\n\t{1}", modelCodeEmsFuel, e.Message);
					Console.WriteLine(message);
					CommonTrace.WriteTrace(CommonTrace.TraceError, message);
					return false;
				}

				foreach (var resDesc in retList)
				{
					if (NmsModelMap.ContainsKey(resDesc.Id))
					{
						NmsModelMap[resDesc.Id] = ResourcesDescriptionConverter.ConvertTo<EMSFuel>(resDesc);
					}
					else
					{
						NmsModelMap.Add(resDesc.Id, ResourcesDescriptionConverter.ConvertTo<EMSFuel>(resDesc));
					}
				}

				// clear retList for getting new model from NMS
				retList.Clear();
				#endregion
				#region getting EnergyConsumer
				try
				{
					// third get all enenrgy consumers from NMS
					properties = modelResourcesDesc.GetAllPropertyIds(modelCodeEnergyConsumer);

					iteratorId = NetworkModelGDAProxy.Instance.GetExtentValues(modelCodeEnergyConsumer, properties);
					resourcesLeft = NetworkModelGDAProxy.Instance.IteratorResourcesLeft(iteratorId);

					while (resourcesLeft > 0)
					{
						List<ResourceDescription> rds = NetworkModelGDAProxy.Instance.IteratorNext(numberOfResources, iteratorId);
						retList.AddRange(rds);
						resourcesLeft = NetworkModelGDAProxy.Instance.IteratorResourcesLeft(iteratorId);
					}
					NetworkModelGDAProxy.Instance.IteratorClose(iteratorId);
				}
				catch (Exception e)
				{
					message = string.Format("Getting extent values method failed for {0}.\n\t{1}", modelCodeEnergyConsumer, e.Message);
					Console.WriteLine(message);
					CommonTrace.WriteTrace(CommonTrace.TraceError, message);
					return false;
				}

				foreach (var resDesc in retList)
				{
					if (NmsModelMap.ContainsKey(resDesc.Id))
					{
						NmsModelMap[resDesc.Id] = ResourcesDescriptionConverter.ConvertTo<EnergyConsumer>(resDesc);
					}
					else
					{
						NmsModelMap.Add(resDesc.Id, ResourcesDescriptionConverter.ConvertTo<EnergyConsumer>(resDesc));
					}
				}

				// clear retList
				retList.Clear();
                #endregion

                #region getting Analog
                try
                {
                    // third get all enenrgy consumers from NMS
                    properties = modelResourcesDesc.GetAllPropertyIds(modelCodeAnalog);

                    iteratorId = NetworkModelGDAProxy.Instance.GetExtentValues(modelCodeAnalog, properties);
                    resourcesLeft = NetworkModelGDAProxy.Instance.IteratorResourcesLeft(iteratorId);

                    while (resourcesLeft > 0)
                    {
                        List<ResourceDescription> rds = NetworkModelGDAProxy.Instance.IteratorNext(numberOfResources, iteratorId);
                        retList.AddRange(rds);
                        resourcesLeft = NetworkModelGDAProxy.Instance.IteratorResourcesLeft(iteratorId);
                    }
                    NetworkModelGDAProxy.Instance.IteratorClose(iteratorId);
                }
                catch (Exception e)
                {
                    message = string.Format("Getting extent values method failed for {0}.\n\t{1}", modelCodeEnergyConsumer, e.Message);
                    Console.WriteLine(message);
                    CommonTrace.WriteTrace(CommonTrace.TraceError, message);
                    return false;
                }

                foreach (var resDesc in retList)
                {
                    if (NmsModelMap.ContainsKey(resDesc.Id))
                    {
                        NmsModelMap[resDesc.Id] = ResourcesDescriptionConverter.ConvertTo<Analog>(resDesc);
                    }
                    else
                    {
                        NmsModelMap.Add(resDesc.Id, ResourcesDescriptionConverter.ConvertTo<Analog>(resDesc));
                    }
                }

                // clear retList
                retList.Clear();
                #endregion

                OnPropertyChanged(nameof(NmsModelMap));
                return true;
			}
		}

		public DashboardViewModel DashboardViewModel
		{
			get
			{
				return dashboardViewModel;
			}

			set
			{
				dashboardViewModel = value;
				OnPropertyChanged();
			}
		}

		public AlarmSummaryViewModel AlarmSummaryViewModel
		{
			get
			{
				return alarmSummaryViewModel;
			}

			set
			{
				alarmSummaryViewModel = value;
				OnPropertyChanged();
			}
		}

		public Dictionary<long, IdentifiedObject> NmsModelMap
		{
			get
			{
				return nmsModelMap;
			}

			set
			{
				nmsModelMap = value;
			}
		}

        public HistoryViewModel HistoryViewModel
        {
            get
            {
                return historyViewModel;
            }

            set
            {
                historyViewModel = value;
            }
        }
    }
}
