using KSPARPAPITester_ARPWrapper;
using KSPPluginFramework;

namespace KSPARPAPITester
{
    [KSPAddon(KSPAddon.Startup.Flight, false)]
    [WindowInitials(Visible = true, Caption = "KSP ARP API Tester", DragEnabled = true)]
    public class KSPARPAPITester : MonoBehaviourWindow
    {
        internal override void Start()
        {
            LogFormatted("Start");
            ARPWrapper.InitKSPARPWrapper();

            ARPWrapper.KSPARP.onMonitorStateChanged += KSPARP_onMonitorStateChanged;
            ARPWrapper.KSPARP.onAlarmStateChanged += KSPARP_onAlarmStateChanged;
        }

        private void KSPARP_onMonitorStateChanged(ARPWrapper.KSPARPAPI.MonitorStateChangedEventArgs e)
        {
            LogFormatted("{0}:{1}->{2} ({3})", e.resource.ResourceDef.name, e.oldValue, e.newValue, e.AlarmState);
        }

        private void KSPARP_onAlarmStateChanged(ARPWrapper.KSPARPAPI.AlarmStateChangedEventArgs e)
        {
            LogFormatted("{0}:{1}", e.resource.ResourceDef.name, e.newValue);
        }

        internal override void Awake()
        {
            WindowRect = new Rect(600, 100, 300, 200);
        }

        internal override void OnDestroy()
        {
        }

        internal override void DrawWindow(int id)
        {
            GUILayout.Label("Assembly: " + ARPWrapper.AssemblyExists);
            GUILayout.Label("Instance: " + ARPWrapper.InstanceExists);
            GUILayout.Label("APIReady: " + ARPWrapper.APIReady);

            if (ARPWrapper.APIReady)
                foreach (var r in ARPWrapper.KSPARP.VesselResources.Values)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Label(
                        string.Format("{0}:  {1}/{2}", r.ResourceDef.name, r.AmountFormatted, r.MaxAmountFormatted),
                        GUILayout.Width(200));
                    if (r.AlarmState == ARPWrapper.ARPResource.AlarmStateEnum.Unacknowledged)
                        if (GUILayout.Button("Acknowledge"))
                            ARPWrapper.KSPARP.AcknowledgeAlarm(r.ResourceDef.id);
                    GUILayout.EndHorizontal();
                }
        }
    }
}