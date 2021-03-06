﻿//-----------------------------------------------------------------------
// <copyright file="AlarmsEvents.cs" company="EMS-Team">
//     Company copyright tag.
// </copyright>
//-----------------------------------------------------------------------

namespace EMS.Services.AlarmsEventsService
{
    using System;
    using Common;
    using ServiceContracts;
    using PubSub;
    using System.Collections.Generic;
    using CommonMeasurement;
    using System.ServiceModel;
    using System.Data.SqlClient;
    using System.Data;

    /// <summary>
    /// Class for ICalculationEngineContract implementation
    /// </summary>
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single, ConcurrencyMode = ConcurrencyMode.Multiple)]
    public class AlarmsEvents : IAlarmsEventsContract, IAesIntegirtyContract
    {
        /// <summary>
        /// entity for storing PublisherService instance
        /// </summary>
        private PublisherService publisher;

        /// <summary>
        /// list for storing AlarmHelper entities
        /// </summary>
        private List<AlarmHelper> alarms;

        /// <summary>
        /// Temp list for alarms from database
        /// </summary>
        private List<AlarmHelper> alarmsFromDatabase;

        private readonly int DEADBAND_VALUE = 20;
        public object alarmLock = new object();
        private Dictionary<long, bool> isNormalCreated = new Dictionary<long, bool>(10);

        /// <summary>
        /// Initializes a new instance of the <see cref="AlarmsEvents" /> class
        /// </summary>
        public AlarmsEvents()
        {
            this.Publisher = new PublisherService();
            this.Alarms = new List<AlarmHelper>();
            alarmsFromDatabase = SelectAlarmsFromDatabase();
            if (alarmsFromDatabase != null)
            {
                //this.Alarms = alarmsFromDatabase;
            }
        }

        /// <summary>
        /// Gets or sets Alarms of the entity
        /// </summary>
        public List<AlarmHelper> Alarms
        {
            get
            {
                return this.alarms;
            }

            set
            {
                this.alarms = value;
            }
        }

        /// <summary>
        /// Gets or sets Publisher of the entity
        /// </summary>
        public PublisherService Publisher
        {
            get
            {
                return this.publisher;
            }

            set
            {
                this.publisher = value;
            }
        }

        /// <summary>
        /// Adds new alarm
        /// </summary>
        /// <param name="alarm">alarm to add</param>
        public void AddAlarm(AlarmHelper alarm)
        {
            bool normalAlarm = false;
            if (Alarms.Count == 0 && alarm.Type.Equals(AlarmType.NORMAL))
            {
                return;
            }

            PublishingStatus publishingStatus = PublishingStatus.INSERT;
            bool updated = false;
            try
            {
                alarm.AckState = AckState.Unacknowledged;
                if (string.IsNullOrEmpty(alarm.CurrentState))
                {
                    alarm.CurrentState = string.Format("{0} | {1}", State.Active, alarm.AckState);
                }

                // cleared status check
                foreach (AlarmHelper item in Alarms)
                {
                    if (item.Gid.Equals(alarm.Gid) && item.CurrentState.Contains(State.Active.ToString()))
                    {
                        item.Severity = alarm.Severity;
                        item.Value = alarm.Value;
                        item.Message = alarm.Message;
                        item.TimeStamp = alarm.TimeStamp;
                        publishingStatus = PublishingStatus.UPDATE;
                        updated = true;
                        break;
                    }
                    else if (item.Gid.Equals(alarm.Gid) && item.CurrentState.Contains(State.Cleared.ToString()))
                    {
                        if (alarm.Type.Equals(AlarmType.NORMAL) && !item.Type.Equals(AlarmType.NORMAL.ToString()))
                        {
                            bool normalCreated = false;
                            if (this.isNormalCreated.TryGetValue(alarm.Gid, out normalCreated))
                            {
                                if (!normalCreated)
                                {
                                    normalAlarm = true;
                                }
                            }

                            break;
                        }
                    }
                }

                // ako je insert dodaj u listu - inace je updateovan
                if (publishingStatus.Equals(PublishingStatus.INSERT) && !updated && !alarm.Type.Equals(AlarmType.NORMAL))
                {
                    RemoveFromAlarms(alarm.Gid);
                    this.Alarms.Add(alarm);
                    if (InsertAlarmIntoDb(alarm))
                    {
                        Console.WriteLine("Alarm with GID:{0} recorded into alarms database.", alarm.Gid);
                    }
                    this.isNormalCreated[alarm.Gid] = false;
                }
                if (alarm.Type.Equals(AlarmType.NORMAL) && normalAlarm)
                {
                    RemoveFromAlarms(alarm.Gid);
                    this.Alarms.Add(alarm);
                    this.Publisher.PublishAlarmsEvents(alarm, publishingStatus);
                    this.isNormalCreated[alarm.Gid] = true;
                    
                }
                else if (!alarm.Type.Equals(AlarmType.NORMAL))
                {
                    this.Publisher.PublishAlarmsEvents(alarm, publishingStatus);
                }

                //Console.WriteLine("AlarmsEvents: AddAlarm method");
                string message = string.Format("Alarm on Analog Gid: {0} - Value: {1}", alarm.Gid, alarm.Value);
                CommonTrace.WriteTrace(CommonTrace.TraceInfo, message);
            }
            catch (Exception ex)
            {
                string message = string.Format("Greska ", ex.Message);
                CommonTrace.WriteTrace(CommonTrace.TraceError, message);
                //throw new Exception(message);
            }
        }


        private void RemoveFromAlarms(long gid)
        {
            
            lock(alarmLock)
            {
                List<AlarmHelper> alarmsToRemove = new List<AlarmHelper>(1);
                foreach (AlarmHelper ah in Alarms)
                {
                    if (ah.Gid == gid)
                    {
                        alarmsToRemove.Add(ah);
                    }
                }

                foreach(AlarmHelper ah in alarmsToRemove)
                {
                    Alarms.Remove(ah);
                }
            }
        }



        public void UpdateStatus(AnalogLocation analogLoc, State state)
        {
            long powerSystemResGid = analogLoc.Analog.PowerSystemResource;
            List<AlarmHelper> alarmsToAdd = new List<AlarmHelper>(2);
            foreach (AlarmHelper alarm in this.Alarms)
            {
                if (alarm.Gid.Equals(powerSystemResGid) && alarm.CurrentState.Contains(State.Active.ToString()))
                {
                    alarm.CurrentState = string.Format("{0} | {1}", state, alarm.AckState);
                    alarm.PubStatus = PublishingStatus.UPDATE;
                    if (UpdateAlarmStatusIntoDb(alarm))
                    {
                        Console.WriteLine("Alarm status with GID:{0} updated into database.", alarm.Gid);
                    }

                    try
                    {
                        this.Publisher.PublishStateChange(alarm);
                        string message = string.Format("Alarm on Gid: {0} - Changed status: {1}", alarm.Gid, alarm.CurrentState);
                        CommonTrace.WriteTrace(CommonTrace.TraceInfo, message);
                    }
                    catch (Exception ex)
                    {
                        string message = string.Format("Greska ", ex.Message);
                        CommonTrace.WriteTrace(CommonTrace.TraceError, message);
                        // throw new Exception(message);
                    }
                }
            }
        }

        public List<AlarmHelper> InitiateIntegrityUpdate()
        {
            string message = string.Format("UI client requested integirty update for existing alarms.");
            CommonTrace.WriteTrace(CommonTrace.TraceInfo, message);

            return Alarms;
        }

        private bool InsertAlarmIntoDb(AlarmHelper alarm)
        {
            bool success = true;

            using (SqlConnection connection = new SqlConnection(Config.Instance.ConnectionString))
            {
                try
                {
                    connection.Open();

                    using (SqlCommand cmd = new SqlCommand("InsertAlarm", connection))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        cmd.Parameters.Add("@gid", SqlDbType.BigInt).Value = alarm.Gid;
                        cmd.Parameters.Add("@alarmValue", SqlDbType.Float).Value = alarm.Value;
                        cmd.Parameters.Add("@minValue", SqlDbType.Float).Value = alarm.MinValue;
                        cmd.Parameters.Add("@maxValue", SqlDbType.Float).Value = alarm.MaxValue;
                        cmd.Parameters.Add("@timeStamp", SqlDbType.DateTime).Value = alarm.TimeStamp.ToString("yyyy-MM-dd HH:mm:ss.fff");
                        cmd.Parameters.Add("@severity", SqlDbType.Int).Value = alarm.Severity;
                        cmd.Parameters.Add("@initiatingValue", SqlDbType.Float).Value = alarm.InitiatingValue;
                        cmd.Parameters.Add("@lastChange", SqlDbType.DateTime).Value = alarm.LastChange.ToString("yyyy-MM-dd HH:mm:ss.fff");
                        cmd.Parameters.Add("@currentState", SqlDbType.NText, 100).Value = alarm.CurrentState;
                        cmd.Parameters.Add("@ackState", SqlDbType.Int).Value = alarm.AckState;
                        cmd.Parameters.Add("@pubStatus", SqlDbType.Int).Value = alarm.PubStatus;
                        cmd.Parameters.Add("@alarmType", SqlDbType.Int).Value = alarm.Type;
                        cmd.Parameters.Add("@persistent", SqlDbType.Int).Value = alarm.Persistent;
                        cmd.Parameters.Add("@inhibit", SqlDbType.Int).Value = alarm.Inhibit;
                        cmd.Parameters.Add("@message", SqlDbType.NText, 200).Value = alarm.Message;

                        cmd.ExecuteNonQuery();
                        cmd.Parameters.Clear();
                    }

                    connection.Close();
                }
                catch (Exception e)
                {
                    success = false;
                    string message = string.Format("Failed to insert alarm into database. {0}", e.Message);
                    CommonTrace.WriteTrace(CommonTrace.TraceError, message);
                    Console.WriteLine(message);
                }
            }

            return success;
        }

        private List<AlarmHelper> SelectAlarmsFromDatabase()
        {
            List<AlarmHelper> alarms = new List<AlarmHelper>();

            using (SqlConnection connection = new SqlConnection(Config.Instance.ConnectionString))
            {
                try
                {
                    connection.Open();

                    using (SqlCommand cmd = new SqlCommand("SELECT * FROM Alarms;", connection))
                    {
                        cmd.CommandType = CommandType.Text;

                        SqlDataReader reader = cmd.ExecuteReader();

                        while (reader.Read())
                        {
                            long gid = Convert.ToInt64(reader[1]);
                            float alarmValue = (float)Convert.ToDouble(reader[2]);
                            float minValue = (float)Convert.ToDouble(reader[3]);
                            float maxValue = (float)Convert.ToDouble(reader[4]);
                            DateTime timestamp = Convert.ToDateTime(reader[5]);
                            int severity = Convert.ToInt32(reader[6]);
                            float initiatingValue = (float)Convert.ToDouble(reader[7]);
                            DateTime lastChange = Convert.ToDateTime(reader[8]);
                            string currentState = Convert.ToString(reader[9]);
                            int ackState = Convert.ToInt32(reader[10]);
                            int pubStatus = Convert.ToInt32(reader[11]);
                            int alarmType = Convert.ToInt32(reader[12]);
                            int persistent = Convert.ToInt32(reader[13]);
                            int inhibit = Convert.ToInt32(reader[14]);
                            string alarmMessage = Convert.ToString(reader[15]);

                            AlarmHelper alarm = new AlarmHelper
                            {
                                Gid = gid,
                                Severity = (SeverityLevel)severity,
                                Value = alarmValue,
                                InitiatingValue = initiatingValue,
                                MinValue = minValue,
                                MaxValue = maxValue,
                                TimeStamp = timestamp,
                                LastChange = lastChange,
                                CurrentState = currentState,
                                AckState = (AckState)ackState,
                                PubStatus = (PublishingStatus)pubStatus,
                                Type = (AlarmType)alarmType,
                                Persistent = (PersistentState)persistent,
                                Inhibit = (InhibitState)inhibit,
                                Message = alarmMessage
                            };

                            alarms.Add(alarm);
                        }
                    }

                    connection.Close();
                }
                catch (Exception e)
                {
                    string message = string.Format("Failed read alarms from database. {0}", e.Message);
                    CommonTrace.WriteTrace(CommonTrace.TraceError, message);
                    Console.WriteLine(message);
                }
            }

            return alarms;
        }

        private bool UpdateAlarmIntoDb(AlarmHelper alarm)
        {
            bool success = true;

            using (SqlConnection connection = new SqlConnection(Config.Instance.ConnectionString))
            {
                try
                {
                    connection.Open();

                    using (SqlCommand cmd = new SqlCommand("UpdateAlarm", connection))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        cmd.Parameters.Add("@gid", SqlDbType.BigInt).Value = alarm.Gid;
                        cmd.Parameters.Add("@alarmValue", SqlDbType.Float).Value = alarm.Value;
                        cmd.Parameters.Add("@lastChange", SqlDbType.DateTime).Value = alarm.LastChange.ToString("yyyy-MM-dd HH:mm:ss.fff");
                        cmd.Parameters.Add("@currentState", SqlDbType.NText, 100).Value = alarm.CurrentState;
                        cmd.ExecuteNonQuery();
                        cmd.Parameters.Clear();
                    }

                    connection.Close();
                }
                catch (Exception e)
                {
                    success = false;
                    string message = string.Format("Failed to update alarm into database. {0}", e.Message);
                    CommonTrace.WriteTrace(CommonTrace.TraceError, message);
                    Console.WriteLine(message);
                }
            }

            return success;
        }

        private bool UpdateAlarmStatusIntoDb(AlarmHelper alarm)
        {
            bool success = true;

            using (SqlConnection connection = new SqlConnection(Config.Instance.ConnectionString))
            {
                try
                {
                    connection.Open();

                    using (SqlCommand cmd = new SqlCommand("UpdateAlarmStatus", connection))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        cmd.Parameters.Add("@gid", SqlDbType.BigInt).Value = alarm.Gid;
                        cmd.Parameters.Add("@currentState", SqlDbType.NText, 100).Value = alarm.CurrentState;
                        cmd.Parameters.Add("@pubStatus", SqlDbType.Int).Value = alarm.PubStatus;
                        cmd.ExecuteNonQuery();
                        cmd.Parameters.Clear();
                    }

                    connection.Close();
                }
                catch (Exception e)
                {
                    success = false;
                    string message = string.Format("Failed to update alarm status into database. {0}", e.Message);
                    CommonTrace.WriteTrace(CommonTrace.TraceError, message);
                    Console.WriteLine(message);
                }
            }

            return success;
        }
    }
}