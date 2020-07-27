using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;

// TODO: Change this namespace to something specific to your plugin here.
//EG:
// namespace MyPlugin_ARPWrapper
namespace ARPWrapper
{
    ///////////////////////////////////////////////////////////////////////////////////////////
    // BELOW HERE SHOULD NOT BE EDITED - this links to the loaded ARP module without requiring a Hard Dependancy
    ///////////////////////////////////////////////////////////////////////////////////////////

    /// <summary>
    ///     The Wrapper class to access ARP from another plugin
    /// </summary>
    public class ARPWrapper
    {
        protected static Type KSPARPType;

        protected static Type KSPARPResourceType;
        //protected static System.Type KSPARPResourceListType;

        protected static object actualARP;

        /// <summary>
        ///     This is the Alternate Resource Panel object
        ///     SET AFTER INIT
        /// </summary>
        public static KSPARPAPI KSPARP;

        /// <summary>
        ///     Whether we managed to wrap all the methods/functions from the instance.
        ///     SET AFTER INIT
        /// </summary>
        private static bool _KSPARPWrapped;

        /// <summary>
        ///     Whether we found the KSPAlternateResourcePanel assembly in the loadedassemblies.
        ///     SET AFTER INIT
        /// </summary>
        public static bool AssemblyExists => KSPARPType != null;

        /// <summary>
        ///     Whether we managed to hook the running Instance from the assembly.
        ///     SET AFTER INIT
        /// </summary>
        public static bool InstanceExists => KSPARP != null;

        /// <summary>
        ///     Whether the object has been wrapped and the APIReady flag is set in the real ARP
        /// </summary>
        public static bool APIReady => _KSPARPWrapped && KSPARP.APIReady;

        /// <summary>
        ///     This method will set up the KSPARP object and wrap all the methods/functions
        /// </summary>
        /// <param name="Force">This option will force the Init function to rebind everything</param>
        /// <returns></returns>
        public static bool InitKSPARPWrapper()
        {
            //if (!_KSPARPWrapped )
            //{
            //reset the internal objects
            _KSPARPWrapped = false;
            actualARP = null;
            KSPARP = null;
            LogFormatted("Attempting to Grab KSPARP Types...");

            //find the base type
            KSPARPType = AssemblyLoader.loadedAssemblies
                .Select(a => a.assembly.GetExportedTypes())
                .SelectMany(t => t)
                .FirstOrDefault(t => t.FullName == "KSPAlternateResourcePanel.KSPAlternateResourcePanel");

            if (KSPARPType == null) return false;

            //now the resource Type
            KSPARPResourceType = AssemblyLoader.loadedAssemblies
                .Select(a => a.assembly.GetExportedTypes())
                .SelectMany(t => t)
                .FirstOrDefault(t => t.FullName == "KSPAlternateResourcePanel.ARPResource");

            if (KSPARPResourceType == null) return false;

            //now grab the running instance
            LogFormatted("Got Assembly Types, grabbing Instance");
            actualARP = KSPARPType.GetField("APIInstance", BindingFlags.Public | BindingFlags.Static).GetValue(null);

            if (actualARP == null)
            {
                LogFormatted("Failed grabbing Instance");
                return false;
            }

            //If we get this far we can set up the local object and its methods/functions
            LogFormatted("Got Instance, Creating Wrapper Objects");
            KSPARP = new KSPARPAPI(actualARP);
            //}
            _KSPARPWrapped = true;
            return true;
        }


        /// <summary>
        ///     The Type that is an analogue of the real ARP. This lets you access all the API-able properties and Methods of the
        ///     ARP
        /// </summary>
        public class KSPARPAPI
        {
            private readonly object actualARP;

            private readonly FieldInfo APIReadyField;

            internal KSPARPAPI(object ARP)
            {
                //store the actual object
                actualARP = ARP;

                //these sections get and store the reflection info and actual objects where required. Later in the properties we then read the values from the actual objects
                //for events we also add a handler
                LogFormatted("Getting APIReady Object");
                APIReadyField = KSPARPType.GetField("APIReady", BindingFlags.Public | BindingFlags.Static);
                LogFormatted("Success: " + (APIReadyField != null));

                LogFormatted("Getting Vessel Resources Object");
                VesselResourcesField =
                    KSPARPType.GetField("lstResourcesVessel", BindingFlags.Public | BindingFlags.Instance);
                actualVesselResources = VesselResourcesField.GetValue(actualARP);
                LogFormatted("Success: " + (actualVesselResources != null));

                LogFormatted("Getting Last Stage Resources Object");
                LastStageResourcesField =
                    KSPARPType.GetField("lstResourcesLastStage", BindingFlags.Public | BindingFlags.Instance);
                actualLastStageResources = LastStageResourcesField.GetValue(actualARP);
                LogFormatted("Success: " + (actualLastStageResources != null));

                LogFormatted("Getting State Change Event");
                OnMonitorStateChangedEvent =
                    KSPARPType.GetEvent("onMonitorStateChanged", BindingFlags.Public | BindingFlags.Instance);
                AddHandler(OnMonitorStateChangedEvent, actualARP, MonitorStateChanged);
                LogFormatted("Getting Acknowledged Event");
                onAlarmStateChangedEvent =
                    KSPARPType.GetEvent("onAlarmStateChanged", BindingFlags.Public | BindingFlags.Instance);
                AddHandler(onAlarmStateChangedEvent, actualARP, AlarmStateChanged);

                LogFormatted("Getting Acknowledge Method");
                AcknowledgeAlarmMethod =
                    KSPARPType.GetMethod("AcknowledgeAlarm", BindingFlags.Public | BindingFlags.Instance);
            }

            /// <summary>
            ///     Whether the APIReady flag is set in the real ARP
            /// </summary>
            public bool APIReady
            {
                get
                {
                    if (APIReadyField == null)
                        return false;

                    return (bool) APIReadyField.GetValue(null);
                }
            }

            #region Resources

            private readonly object actualVesselResources;
            private readonly FieldInfo VesselResourcesField;
            private readonly object actualLastStageResources;
            private readonly FieldInfo LastStageResourcesField;

            /// <summary>
            ///     The list of all resources in the Vessel
            /// </summary>
            internal ARPResourceList VesselResources => ExtractARPResourceList(actualVesselResources);

            /// <summary>
            ///     The list of all resources in the last stage of the vessel
            /// </summary>
            internal ARPResourceList LastStageResources => ExtractARPResourceList(actualLastStageResources);

            /// <summary>
            ///     This converts the ARPResourceList actual object to a new dictionary for consumption
            /// </summary>
            /// <param name="actualResourceList"></param>
            /// <returns></returns>
            private ARPResourceList ExtractARPResourceList(object actualResourceList)
            {
                var ListToReturn = new ARPResourceList();
                try
                {
                    //iterate each "value" in the dictionary
                    foreach (var item in (IDictionary) actualResourceList)
                    {
                        var pi = item.GetType().GetProperty("Value");
                        var oVal = pi.GetValue(item, null);
                        var r1 = new ARPResource(oVal);
                        ListToReturn.Add(r1.ResourceDef.id, r1);
                    }
                }
                catch (Exception)
                {
                    //
                }

                return ListToReturn;
            }

            #endregion

            #region Events

            /// <summary>
            ///     Takes an EventInfo and binds a method to the event firing
            /// </summary>
            /// <param name="Event">EventInfo of the event we want to attach to</param>
            /// <param name="ARPObject">actual object the eventinfo is gathered from</param>
            /// <param name="Handler">Method that we are going to hook to the event</param>
            protected void AddHandler(EventInfo Event, object ARPObject, Action<object> Handler)
            {
                //build a delegate
                var d = Delegate.CreateDelegate(Event.EventHandlerType, Handler.Target, Handler.Method);
                //get the Events Add method
                var addHandler = Event.GetAddMethod();
                //and add the delegate
                addHandler.Invoke(ARPObject, new object[] {d});
            }

            //the info about the event;
            private readonly EventInfo OnMonitorStateChangedEvent;

            /// <summary>
            ///     Event that fires when the MonitorState of a vessel resource changes
            /// </summary>
            public event MonitorStateChangedHandler onMonitorStateChanged;

            /// <summary>
            ///     Structure of the event delegeate
            /// </summary>
            /// <param name="e"></param>
            public delegate void MonitorStateChangedHandler(MonitorStateChangedEventArgs e);

            /// <summary>
            ///     This is the structure that holds the event arguments
            /// </summary>
            public class MonitorStateChangedEventArgs
            {
                /// <summary>
                ///     What the current Alarm state is of the vessel resource
                /// </summary>
                public ARPResource.AlarmStateEnum AlarmState;

                /// <summary>
                ///     What the state of the monitor is now
                /// </summary>
                public ARPResource.MonitorStateEnum newValue;

                /// <summary>
                ///     What the state was before the event
                /// </summary>
                public ARPResource.MonitorStateEnum oldValue;

                /// <summary>
                ///     Resource that has had the monitor state change
                /// </summary>
                public ARPResource resource;

                public MonitorStateChangedEventArgs(object actualEvent, KSPARPAPI arp)
                {
                    var type = actualEvent.GetType();
                    resource = new ARPResource(type.GetField("sender").GetValue(actualEvent));
                    oldValue = (ARPResource.MonitorStateEnum) type.GetField("oldValue").GetValue(actualEvent);
                    ;
                    newValue = (ARPResource.MonitorStateEnum) type.GetField("newValue").GetValue(actualEvent);
                    ;
                    AlarmState = (ARPResource.AlarmStateEnum) type.GetField("AlarmState").GetValue(actualEvent);
                    ;
                }
            }

            /// <summary>
            ///     private function that grabs the actual event and fires our wrapped one
            /// </summary>
            /// <param name="actualEvent">actual event from the ARP</param>
            private void MonitorStateChanged(object actualEvent)
            {
                if (onMonitorStateChanged != null)
                    onMonitorStateChanged(new MonitorStateChangedEventArgs(actualEvent, this));
            }

            //the info about the event;
            private readonly EventInfo onAlarmStateChangedEvent;

            /// <summary>
            ///     Event that fires when the AlarmState of a vessel resource changes
            /// </summary>
            public event AlarmStateChangedHandler onAlarmStateChanged;

            /// <summary>
            ///     Structure of the event delegeate
            /// </summary>
            /// <param name="e"></param>
            public delegate void AlarmStateChangedHandler(AlarmStateChangedEventArgs e);

            /// <summary>
            ///     This is the structure that holds the event arguments
            /// </summary>
            public class AlarmStateChangedEventArgs
            {
                /// <summary>
                ///     What the current Monitor state is of the vessel resource
                /// </summary>
                public ARPResource.MonitorStateEnum MonitorState;

                /// <summary>
                ///     What the state of the monitor is now
                /// </summary>
                public ARPResource.AlarmStateEnum newValue;

                /// <summary>
                ///     What the state was before the event
                /// </summary>
                public ARPResource.AlarmStateEnum oldValue;

                /// <summary>
                ///     Resource that has had the monitor state change
                /// </summary>
                public ARPResource resource;

                //public AlarmChangedEventArgs(ARPResource sender, ARPResource.AlarmType alarmType, Boolean TurnedOn, Boolean Acknowledged)
                public AlarmStateChangedEventArgs(object actualEvent, KSPARPAPI arp)
                {
                    var type = actualEvent.GetType();
                    resource = new ARPResource(type.GetField("sender").GetValue(actualEvent));
                    oldValue = (ARPResource.AlarmStateEnum) type.GetField("oldValue").GetValue(actualEvent);
                    ;
                    newValue = (ARPResource.AlarmStateEnum) type.GetField("newValue").GetValue(actualEvent);
                    ;
                    MonitorState = (ARPResource.MonitorStateEnum) type.GetField("MonitorState").GetValue(actualEvent);
                    ;
                }
            }


            /// <summary>
            ///     private function that grabs the actual event and fires our wrapped one
            /// </summary>
            /// <param name="actualEvent">actual event from the ARP</param>
            private void AlarmStateChanged(object actualEvent)
            {
                if (onAlarmStateChanged != null) onAlarmStateChanged(new AlarmStateChangedEventArgs(actualEvent, this));
            }

            #endregion

            #region Methods

            private readonly MethodInfo AcknowledgeAlarmMethod;

            /// <summary>
            ///     Acknowledge the alarm for this resource
            /// </summary>
            /// <param name="ResourceID">Which resourceID is to be acknowledged</param>
            internal void AcknowledgeAlarm(int ResourceID)
            {
                AcknowledgeAlarmMethod.Invoke(actualARP, new object[] {ResourceID});
            }

            #endregion
        }

        /// <summary>
        ///     Definition of a resource as grabbed by the ARP
        /// </summary>
        public class ARPResource
        {
            /// <summary>
            ///     Possible states the Alarm of this resource can be at
            /// </summary>
            public enum AlarmStateEnum
            {
                None,
                Unacknowledged,
                Acknowledged
            }

            /// <summary>
            ///     Possible states the Monitor of this resource can be at
            /// </summary>
            public enum MonitorStateEnum
            {
                None,
                Warn,
                Alert
            }

            /// <summary>
            ///     Stored actual object
            /// </summary>
            private readonly object actualResource;

            private readonly PropertyInfo AlarmStateInfo;

            private readonly PropertyInfo AmountFormattedProperty;

            private readonly PropertyInfo AmountValueProperty;

            private readonly PropertyInfo MaxAmountFormattedProperty;

            private readonly PropertyInfo MaxAmountValueProperty;

            private readonly PropertyInfo MonitorStateInfo;

            private readonly PropertyInfo ResourceDefProperty;

            internal ARPResource(object r)
            {
                actualResource = r;

                AmountValueProperty = KSPARPResourceType.GetProperty("AmountValue");
                MaxAmountValueProperty = KSPARPResourceType.GetProperty("MaxAmountValue");
                AmountFormattedProperty = KSPARPResourceType.GetProperty("AmountFormatted");
                MaxAmountFormattedProperty = KSPARPResourceType.GetProperty("MaxAmountFormatted");
                ResourceDefProperty = KSPARPResourceType.GetProperty("ResourceDef");

                MonitorStateInfo = KSPARPResourceType.GetProperty("MonitorState");
                AlarmStateInfo = KSPARPResourceType.GetProperty("AlarmState");
            }

            /// <summary>
            ///     How much of this resource is there
            /// </summary>
            public double AmountValue => (double) AmountValueProperty.GetValue(actualResource, null);

            /// <summary>
            ///     How much of this resource can be stored
            /// </summary>
            public double MaxAmountValue => (double) MaxAmountValueProperty.GetValue(actualResource, null);

            /// <summary>
            ///     The amount of resource, formatted for display
            /// </summary>
            public string AmountFormatted => (string) AmountFormattedProperty.GetValue(actualResource, null);

            /// <summary>
            ///     The amount of resource storage, formatted for display
            /// </summary>
            public string MaxAmountFormatted => (string) MaxAmountFormattedProperty.GetValue(actualResource, null);

            /// <summary>
            ///     The KSP definitio for this resource, here you can find all the info about the resource itself
            /// </summary>
            public PartResourceDefinition ResourceDef =>
                (PartResourceDefinition) ResourceDefProperty.GetValue(actualResource, null);

            /// <summary>
            ///     What the Monitor state of the resource is - whether it is healthy, warning or alert level based on the config in
            ///     ARP
            /// </summary>
            public MonitorStateEnum MonitorState => (MonitorStateEnum) MonitorStateInfo.GetValue(actualResource, null);

            /// <summary>
            ///     What the Alarm state of the resource is - whether it is not alarmed, unacknowledged or acknowledged
            /// </summary>
            public AlarmStateEnum AlarmState => (AlarmStateEnum) AlarmStateInfo.GetValue(actualResource, null);
        }

        public class ARPResourceList : Dictionary<int, ARPResource>
        {
            /// <summary>
            ///     Return a list of the Resources that have unacknowledged alarms
            /// </summary>
            private Dictionary<int, ARPResource> UnacknowledgedAlarms
            {
                get
                {
                    return Values.Where(x => x.AlarmState == ARPResource.AlarmStateEnum.Unacknowledged)
                        .ToDictionary(x => x.ResourceDef.id);
                }
            }
        }

        #region Logging Stuff

        /// <summary>
        ///     Some Structured logging to the debug file - ONLY RUNS WHEN DLL COMPILED IN DEBUG MODE
        /// </summary>
        /// <param name="Message">Text to be printed - can be formatted as per String.format</param>
        /// <param name="strParams">Objects to feed into a String.format</param>
        [Conditional("DEBUG")]
        internal static void LogFormatted_DebugOnly(string Message, params object[] strParams)
        {
            LogFormatted(Message, strParams);
        }

        /// <summary>
        ///     Some Structured logging to the debug file
        /// </summary>
        /// <param name="Message">Text to be printed - can be formatted as per String.format</param>
        /// <param name="strParams">Objects to feed into a String.format</param>
        internal static void LogFormatted(string Message, params object[] strParams)
        {
            Message = string.Format(Message, strParams);
            var strMessageLine = string.Format("{0},{2}-{3},{1}",
                DateTime.Now, Message, Assembly.GetExecutingAssembly().GetName().Name,
                MethodBase.GetCurrentMethod().DeclaringType.Name);
            UnityEngine.Debug.Log(strMessageLine);
        }

        #endregion
    }
}