using System;

namespace KSPAlternateResourcePanel
{
    public partial class KSPAlternateResourcePanel
    {
        public delegate void AlarmStateChangedHandler(AlarmStateChangedEventArgs e);

        public delegate void MonitorStateChangedHandler(MonitorStateChangedEventArgs e);
        //For API Access

        /// <summary>
        ///     This is the Alternate Resource Panel object we hook from the wrapper
        /// </summary>
        public static KSPAlternateResourcePanel APIInstance;

        public static bool APIReady;

        //Init the API Hooks
        private void APIAwake()
        {
            //set up the hookable object
            APIInstance = this;

            //set up the events we need
            APIInstance.lstResourcesVessel.OnMonitorStateChanged += API_lstResourcesVessel_OnMonitorStateChanged;
            APIInstance.lstResourcesVessel.OnAlarmStateChanged += API_lstResourcesVessel_OnAlarmStateChanged;

            //flag it ready
            LogFormatted("API Ready");
            APIReady = true;
        }

        private void APIDestroy()
        {
            //tear it down
            APIInstance = null;
            try
            {
                APIInstance.lstResourcesVessel.OnMonitorStateChanged -= API_lstResourcesVessel_OnMonitorStateChanged;
            }
            catch (Exception)
            {
            }

            try
            {
                APIInstance.lstResourcesVessel.OnAlarmStateChanged -= API_lstResourcesVessel_OnAlarmStateChanged;
            }
            catch (Exception)
            {
            }

            LogFormatted("API Cleaned up");
            APIReady = false;
        }

        //Raise the API event with the aggregated eventargs object
        private void API_lstResourcesVessel_OnMonitorStateChanged(ARPResource sender,
            ARPResource.MonitorStateEnum oldValue, ARPResource.MonitorStateEnum newValue,
            ARPResource.AlarmStateEnum AlarmState)
        {
            if (onMonitorStateChanged != null)
                onMonitorStateChanged(new MonitorStateChangedEventArgs(sender, oldValue, newValue, AlarmState));
        }

        //Raise the API event with the aggregated eventargs object
        private void API_lstResourcesVessel_OnAlarmStateChanged(ARPResource sender, ARPResource.AlarmStateEnum oldValue,
            ARPResource.AlarmStateEnum newValue, ARPResource.MonitorStateEnum MonitorState)
        {
            if (onAlarmStateChanged != null)
                onAlarmStateChanged(new AlarmStateChangedEventArgs(sender, oldValue, newValue, MonitorState));
        }

        //API Public Events
        public event MonitorStateChangedHandler onMonitorStateChanged;

        public event AlarmStateChangedHandler onAlarmStateChanged;

        /// <summary>
        ///     Method to allow API to acknowledge alarms
        /// </summary>
        /// <param name="ResourceID">UniqueID of resource</param>
        /// <returns></returns>
        public bool AcknowledgeAlarm(int ResourceID)
        {
            if (lstResourcesVessel.ContainsKey(ResourceID))
            {
                lstResourcesVessel[ResourceID].SetAlarmAcknowledged();
            }
            else
            {
                LogFormatted("API unable to Ack Alarm. ResourceID does not exist:{0}", ResourceID);
                return false;
            }

            return true;
        }

        public class MonitorStateChangedEventArgs
        {
            public ARPResource.AlarmStateEnum AlarmState;
            public ARPResource.MonitorStateEnum newValue;
            public ARPResource.MonitorStateEnum oldValue;

            public ARPResource sender;

            public MonitorStateChangedEventArgs(ARPResource sender, ARPResource.MonitorStateEnum oldValue,
                ARPResource.MonitorStateEnum newValue, ARPResource.AlarmStateEnum AlarmState)
            {
                this.sender = sender;
                this.oldValue = oldValue;
                this.newValue = newValue;
                this.AlarmState = AlarmState;
            }
        }

        public class AlarmStateChangedEventArgs
        {
            public ARPResource.MonitorStateEnum MonitorState;
            public ARPResource.AlarmStateEnum newValue;
            public ARPResource.AlarmStateEnum oldValue;

            public ARPResource sender;

            public AlarmStateChangedEventArgs(ARPResource sender, ARPResource.AlarmStateEnum oldValue,
                ARPResource.AlarmStateEnum newValue, ARPResource.MonitorStateEnum MonitorState)
            {
                this.sender = sender;
                this.oldValue = oldValue;
                this.newValue = newValue;
                this.MonitorState = MonitorState;
            }
        }
    }
}