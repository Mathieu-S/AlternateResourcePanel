using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using ARPToolbarWrapper;
using KSPPluginFramework;

namespace KSPAlternateResourcePanel
{
    [KSPAddon(KSPAddon.Startup.Flight, false)]
    public partial class KSPAlternateResourcePanel : MonoBehaviourExtended
    {
        private const double baseRange = 2000;

        internal static Settings settings;

        internal static AudioController audioController;

        //Position of screen button
        private static Rect rectButton = new Rect(Screen.width - 405, 0, 80, 30);

        //Hover Status for mouse
        internal static bool HoverOn;

        //Hover Status for mouse
        internal static bool ShowAll;

        internal bool AutoStagingArmed;
        internal int AutoStagingMaxStage;
        internal bool AutoStagingRunning;
        internal string AutoStagingStatus;
        internal Color32 AutoStagingStatusColor;

        internal int AutoStagingTerminateAt = 1;

        //internal List<String> lstString;
        internal double AutoStagingTriggeredAt;

        /// <summary>flag to reset Window Position</summary>
        internal bool blnResetWindow;

        internal bool blnVesselIsControllable;

        internal IButton btnToolbar;
        internal AudioClip clipAlarmsAlert;
        internal AudioClip clipAlarmsWarning;

        internal int LastStage;
        internal List<ModuleEnginesFX> lstLastStageEngineFXModules;
        internal List<ModuleEngines> lstLastStageEngineModules;

        internal ARPPartDefList lstParts;

        internal ARPPartList lstPartsLastStageEngines;

        internal ARPPartWindowList lstPartWindows;
        public ARPResourceList lstResourcesLastStage;

        /// <summary>
        ///     ResourceIDs of ones to show in window
        /// </summary>
        internal List<int> lstResourcesToDisplay;

        public ARPResourceList lstResourcesVessel;

        public Dictionary<int, ARPResourceList> lstResourcesVesselPerStage;

        //List of resource transfers
        internal ARPTransferList lstTransfers;

        //variables
        internal PartResourceVisibleList SelectedResources;

        //use this to trigger a clean up of sound at the end of the repeating worker loop
        private bool StageCheckAlarmAudio;

        /// <summary>
        ///     Vector to the screenposition of the vessels center of mass
        /// </summary>
        internal Vector3 vectVesselCOMScreen;


#if DEBUG
        internal ARPWindowDebug windowDebug;
#endif

        //windows
        internal ARPWindow windowMain;

        internal Rect windowMainResetPos = new Rect(Screen.width - 339 - GameSettings.UI_SCALE_APPS * 42, 0, 299, 20);
        internal ARPWindowResourceConfig windowResourceConfig;
        internal ARPWindowSettings windowSettings;

        private KSPAlternateResourcePanel()
        {
            APIInstance = this;
        }

        internal override void OnAwake()
        {
            LogFormatted("Awakening the KSPAlternateResourcePanel (ARP)");

            LogFormatted("Checking for PluginData folder");
            if (!Directory.Exists(Resources.PathPlugin + "/PluginData"))
                Directory.CreateDirectory(Resources.PathPlugin + "/PluginData");

            LogFormatted("Loading Settings");
            settings = new Settings("PluginData/settings.cfg");
            var blnSettingsLoaded = settings.Load();
            if (!blnSettingsLoaded)
            {
                settings = new Settings("settings.cfg");
                blnSettingsLoaded = settings.Load();
                if (blnSettingsLoaded)
                {
                    settings.FilePath = "PluginData/settings.cfg";
                    if (!Directory.Exists(Resources.PathPlugin + "/PluginData"))
                        Directory.CreateDirectory(Resources.PathPlugin + "/PluginData");
                    File.Move(Resources.PathPlugin + "/settings.cfg",
                        Resources.PathPlugin + "/PluginData/settings.cfg");
                }
            }

            if (!blnSettingsLoaded)
            {
                settings.FilePath = "PluginData/settings.cfg";
                LogFormatted("Settings Load Failed");
            }

            //If the window is in the pre0.24 default then move it down so its not over the app launcher
            if (new Rect(Screen.width - 310, 0, 310, 40).Contains(settings.vectButtonPos))
            {
                settings.vectButtonPos = new Vector3(Screen.width - 405, 0, 0);
                settings.ButtonPosUpdatedv24 = true;
                settings.Save();
            }

            if (!settings.WindowPosUpdatedv24 &&
                settings.WindowPosition == new Rect(new Rect(Screen.width - 298, 19, 299, 20)))
            {
                LogFormatted("Moving window for 0.24");
                settings.WindowPosUpdatedv24 = true;
                settings.Save();
                blnResetWindow = true;
            }

            //Ensure settings.resources contains all the resources in the loaded game
            VerifyResources();

            //get the sounds and set things up
            Resources.LoadSounds();
            InitAudio();

            //Get whether the toolbar is there
            settings.BlizzyToolbarIsAvailable = ToolbarManager.ToolbarAvailable;

            //convert blizzy bool to display enum
            if (settings.UseBlizzyToolbarIfAvailable)
            {
                settings.UseBlizzyToolbarIfAvailable = false;
                settings.ButtonStyleChosen = ARPWindowSettings.ButtonStyleEnum.Toolbar;
            }

            //setup the Toolbar button if necessary
            if (settings.ButtonStyleToDisplay == ARPWindowSettings.ButtonStyleEnum.Toolbar)
                btnToolbar = InitToolbarButton();
            ////if requested use that button
            //if (settings.BlizzyToolbarIsAvailable && settings.UseBlizzyToolbarIfAvailable)
            //    btnToolbar = InitToolbarButton();

            //init the global variables
            lstPartWindows = new ARPPartWindowList();
            lstResourcesVessel = new ARPResourceList(ARPResourceList.ResourceUpdate.AddValues, settings.Resources);
            lstResourcesLastStage = new ARPResourceList(ARPResourceList.ResourceUpdate.AddValues, settings.Resources);

            lstResourcesVesselPerStage = new Dictionary<int, ARPResourceList>();

            lstPartsLastStageEngines = new ARPPartList();

            lstResourcesToDisplay = new List<int>();
            SelectedResources = new PartResourceVisibleList();

            lstTransfers = new ARPTransferList();

            SelectedResources.ResourceRemoved += SelectedResources_ResourceRemoved;

            lstResourcesVessel.OnMonitorStateChanged += lstResourcesVessel_OnMonitorStateChanged;
            lstResourcesVessel.OnAlarmStateChanged += lstResourcesVessel_OnAlarmStateChanged;

            //init the windows
            InitMainWindow();
            InitSettingsWindow();
            InitResourceConfigWindow();
            InitDebugWindow();

            //plug us in to the draw queue and start the worker
            //Disabled for Unity5
            //RenderingManager.AddToPostDrawQueue(1, DrawGUI);
            StartRepeatingWorker(10);

            //register for stage separation events - so we can cancel the noise on a sep
            GameEvents.onStageActivate.Add(OnStageActivate);
            GameEvents.onFlightReady.Add(OnFlightReady);

            //Hook the App Launcher
            GameEvents.onGUIApplicationLauncherReady.Add(OnGUIAppLauncherReady);
            GameEvents.onGUIApplicationLauncherDestroyed.Add(DestroyAppLauncherButton);
            GameEvents.onGUIApplicationLauncherUnreadifying.Add(OnGUIAppLauncherUnreadifying);

            //do the daily version check if required
            if (settings.DailyVersionCheck)
                settings.VersionCheck(this, false);

            APIAwake();
        }

        private void SelectedResources_ResourceRemoved(int ResourceID)
        {
            lstTransfers.RemoveResourceItems(ResourceID);
        }

        internal override void OnDestroy()
        {
            LogFormatted("Destroying the KSPAlternateResourcePanel (ARP)");

            lstResourcesVessel.OnMonitorStateChanged -= lstResourcesVessel_OnMonitorStateChanged;
            lstResourcesVessel.OnAlarmStateChanged -= lstResourcesVessel_OnAlarmStateChanged;

            //Disabled for Unity5
            //RenderingManager.RemoveFromPostDrawQueue(1, DrawGUI);

            GameEvents.onStageActivate.Remove(OnStageActivate);
            GameEvents.onFlightReady.Remove(OnFlightReady);

            GameEvents.onGUIApplicationLauncherReady.Remove(OnGUIAppLauncherReady);
            GameEvents.onGUIApplicationLauncherDestroyed.Remove(DestroyAppLauncherButton);
            GameEvents.onGUIApplicationLauncherUnreadifying.Remove(OnGUIAppLauncherUnreadifying);
            DestroyAppLauncherButton();

            DestroyToolbarButton(btnToolbar);

            APIDestroy();
        }


        internal override void Start()
        {
            if (settings.ToggleOn)
            {
                AppLauncherToBeSetTrue = true;
                AppLauncherToBeSetTrueAttemptDate = DateTime.Now;
            }

            if (settings.ButtonStyleToDisplay == ARPWindowSettings.ButtonStyleEnum.StockReplace)
                StartCoroutine(ReplaceStockAppButton());
        }

        private void OnStageActivate(int StageNum)
        {
            StageCheckAlarmAudio = true;

            //trigger the autostage
            if (AutoStagingArmed && !AutoStagingRunning)
                AutoStagingRunning = true;
        }

        private void OnFlightReady()
        {
            AutoStagingMaxStage = StageManager.StageCount - 1;
        }

//        void lstResourcesVessel_OnMonitorStateChange(ARPResource sender, ARPResource.MonitorStateEnum alarmType, bool TurnedOn,Boolean AlarmAcknowledged)
        private void lstResourcesVessel_OnMonitorStateChanged(ARPResource sender, ARPResource.MonitorStateEnum oldValue,
            ARPResource.MonitorStateEnum newValue, ARPResource.AlarmStateEnum AlarmState)
        {
            LogFormatted_DebugOnly("ARP-{0}:{1}->{2} ({3})", sender.ResourceDef.name, oldValue, newValue,
                sender.AlarmState);
            //Play a sound if necessary
            //if (TurnedOn && !AlarmAcknowledged && settings.AlarmsEnabled && settings.Resources[sender.ResourceDef.id].AlarmEnabled)
            if (newValue != ARPResource.MonitorStateEnum.None &&
                AlarmState == ARPResource.AlarmStateEnum.Unacknowledged && settings.AlarmsEnabled &&
                settings.Resources[sender.ResourceDef.id].AlarmEnabled)
                switch (newValue)
                {
                    case ARPResource.MonitorStateEnum.Alert:
                        if (clipAlarmsAlert != null)
                            audioController.Play(clipAlarmsAlert, settings.AlarmsAlertRepeats);
                        break;
                    case ARPResource.MonitorStateEnum.Warn:
                        //dont play the sound if we are coming down from alert
                        if (oldValue != ARPResource.MonitorStateEnum.Alert && clipAlarmsAlert != null)
                            audioController.Play(clipAlarmsWarning, settings.AlarmsWarningRepeats);
                        break;
                }
        }

        private void lstResourcesVessel_OnAlarmStateChanged(ARPResource sender, ARPResource.AlarmStateEnum oldValue,
            ARPResource.AlarmStateEnum newValue, ARPResource.MonitorStateEnum MonitorState)
        {
            LogFormatted_DebugOnly("ARP-{0}:{1}->{2} ({3})", sender.ResourceDef.name, oldValue, newValue,
                sender.MonitorState);
            if (newValue != ARPResource.AlarmStateEnum.Unacknowledged)
                audioController.Stop();
        }

        /// <summary>
        ///     This will add any missing resources to the settings objects befoe we run so we have records for all
        /// </summary>
        private void VerifyResources()
        {
            var AddedResources = false;

            if (settings.Resources.Count == 0)
            {
                // Set a Default config
                LogFormatted("Setting a Default Res Config");
                settings.Resources.Add(1576437329, new ResourceSettings(1576437329, "ElectricCharge"));
                settings.Resources.Add(2001413032, new ResourceSettings(2001413032, "MonoPropellant"));
                settings.Resources.Add(-792463147, new ResourceSettings(-792463147, "EVA Propellant"));
                settings.Resources.Add(34, new ResourceSettings(34, "") {IsSeparator = true});
                settings.Resources.Add(466598945, new ResourceSettings(466598945, "Ablator"));
                settings.Resources.Add(35, new ResourceSettings(35, "") {IsSeparator = true});
                settings.Resources.Add(374119730, new ResourceSettings(374119730, "LiquidFuel"));
                settings.Resources.Add(-1823983486, new ResourceSettings(-1823983486, "Oxidizer"));
                settings.Resources.Add(650317537, new ResourceSettings(650317537, "SolidFuel"));
                settings.Resources.Add(36, new ResourceSettings(36, "") {IsSeparator = true});
                settings.Resources.Add(-1909417378, new ResourceSettings(-1909417378, "IntakeAir"));
                settings.Resources.Add(79554, new ResourceSettings(79554, "Ore"));
                settings.Resources.Add(1447111193, new ResourceSettings(1447111193, "XenonGas"));
            }

            foreach (PartResourceDefinition item in PartResourceLibrary.Instance.resourceDefinitions)
                if (!settings.Resources.ContainsKey(item.id))
                {
                    LogFormatted_DebugOnly("Adding Resource to Settings - {0}", item.name);
                    AddedResources = true;

                    //see if the modder provided a default displayasvalue
                    var dispAs = GetResourceDisplayvaluefromDef(item.name);
                    settings.Resources.Add(item.id,
                        new ResourceSettings {id = item.id, name = item.name, DisplayValueAs = dispAs});
                }

            if (AddedResources) settings.Save();
        }

        /// <summary>
        ///     get the displayvalue from the resourcedef if there is one
        /// </summary>
        /// <returns></returns>
        private static ResourceSettings.DisplayUnitsEnum GetResourceDisplayvaluefromDef(string ResourceName)
        {
            var ret = ResourceSettings.DisplayUnitsEnum.Units;
            //Find All the RESOURCE_DEFINITION Nodes
            ConfigNode[] cns = GameDatabase.Instance.GetConfigNodes("RESOURCE_DEFINITION");
            foreach (ConfigNode cn in cns)
                if (cn.HasValue("name") && cn.GetValue("name") == ResourceName)
                    if (cn.HasValue("ksparpdisplayvalueas"))
                        //If it has a name and a ksparpicon
                        try
                        {
                            string DisplayAs = cn.GetValue("ksparpdisplayvalueas");

                            ret = (ResourceSettings.DisplayUnitsEnum) Enum.Parse(
                                typeof(ResourceSettings.DisplayUnitsEnum), DisplayAs);
                        }
                        catch (Exception)
                        {
                            LogFormatted("Unable to interpret ksparpdisplayvalueas {0}-{1}", cn.GetValue("name"),
                                cn.GetValue("ksparpdisplayvalueas"));
                        }

            return ret;
        }


        private void InitAudio()
        {
            audioController = AddComponent<AudioController>();
            audioController.mbARP = this;
            audioController.Init();
        }

        private void InitMainWindow()
        {
            // windowMain = gameObject.AddComponent<ARPWindow>();
            windowMain = AddComponent<ARPWindow>();
            windowMain.mbARP = this;
            windowMain.WindowRect = settings.WindowPosition;
            windowMain.DragEnabled = settings.ButtonStyleChosen == ARPWindowSettings.ButtonStyleEnum.StockReplace
                ? false
                : !settings.LockLocation;
            windowMain.ClampToScreenOffset = new RectOffset(-1, -1, -1, -1);
            windowMain.TooltipsEnabled = true;

            windowMain.WindowMoveEventsEnabled = true;
        }

        private void InitSettingsWindow()
        {
            // windowMain = gameObject.AddComponent<ARPWindow>();
            windowSettings = AddComponent<ARPWindowSettings>();
            windowSettings.mbARP = this;
            windowSettings.Visible = false;
            windowSettings.DragEnabled = true;
            windowSettings.ClampToScreenOffset = new RectOffset(-1, -1, -1, -1);
            windowSettings.TooltipsEnabled = true;
        }

        private void InitResourceConfigWindow()
        {
            // windowMain = gameObject.AddComponent<ARPWindow>();
            windowResourceConfig = AddComponent<ARPWindowResourceConfig>();
            windowResourceConfig.mbARP = this;
            windowResourceConfig.Visible = false;
            windowResourceConfig.DragEnabled = true;
            windowResourceConfig.ClampToScreenOffset = new RectOffset(-1, -1, -1, -1);
        }

        [Conditional("DEBUG")]
        private void InitDebugWindow()
        {
#if DEBUG
            windowDebug = AddComponent<ARPWindowDebug>();
            windowDebug.mbARP = this;
            windowDebug.Visible = true;
            windowDebug.WindowRect = new Rect(0, 50, 300, 200);
            windowDebug.DragEnabled = true;
            windowDebug.settings = settings;
#endif
        }


        internal override void OnGUIOnceOnly()
        {
            //Get the textures we need into Textures
            Resources.LoadTextures();

            //Set up the Styles
            Styles.InitStyles();

            //Set up the Skins
            Styles.InitSkins();

            //Set the current Skin
            SkinsLibrary.SetCurrent(settings.SelectedSkin.ToString());
        }


        internal override void OnGUIEvery()
        {
            //base.OnGUIEvery();


            //Draw the button - if we arent using blizzy's toolbar
            //if (!(settings.BlizzyToolbarIsAvailable && settings.UseBlizzyToolbarIfAvailable))
            if (settings.ButtonStyleToDisplay == ARPWindowSettings.ButtonStyleEnum.Basic)
            {
                //Set Button Rectangle position
                rectButton.x = settings.vectButtonPos.x;
                rectButton.y = settings.vectButtonPos.y;

                if (GUI.Button(rectButton, "Alternate", SkinsLibrary.CurrentSkin.GetStyle("ButtonMain")))
                {
                    settings.ToggleOn = !settings.ToggleOn;
                    settings.Save();
                }
            }

            //Test for mouse over any component - do this on repaint so it doesn't do it on layout and cause grouping errors
            if (Event.current.type == EventType.Repaint)
                HoverOn = IsMouseOver();

            if (!HoverOn)
                ShowAll = false;

            //Are we drawing the main window - hovering, or toggled or alarmsenabled and an unackowledged alarm - and theres resources
            //if ((HoverOn || settings.ToggleOn || (settings.AlarmsEnabled && lstResourcesVessel.UnacknowledgedAlarms())) && (lstResourcesVessel.Count > 0))
            if ((HoverOn || settings.ToggleOn || settings.AlarmsEnabled && lstResourcesVessel.UnacknowledgedAlarms()) &&
                lstResourcesVessel.Count > 0)
            {
                windowMain.Visible = true;
                if (blnResetWindow)
                {
                    windowMain.WindowRect = new Rect(windowMainResetPos);
                    blnResetWindow = false;
                    settings.WindowPosition = windowMain.WindowRect;
                    settings.Save();
                }
            }
            else
            {
                windowMain.Visible = false;
                windowSettings.Visible = false;
            }
        }

        /// <summary>
        ///     Is the mouse over the main or settings windows or the button
        /// </summary>
        /// <returns></returns>
        internal bool IsMouseOver()
        {
            if (settings.ButtonStyleToDisplay == ARPWindowSettings.ButtonStyleEnum.Toolbar)
                //return MouseOverToolbarBtn || (windowMain.Visible && windowMain.WindowRect.Contains(Event.current.mousePosition));
                return MouseOverToolbarBtn && !settings.DisableHover ||
                       windowMain.Visible && windowMain.WindowRect.Contains(Event.current.mousePosition) ||
                       windowSettings.Visible && windowSettings.WindowRect.Contains(Event.current.mousePosition);

            //App Launcher version's
            if (settings.ButtonStyleToDisplay == ARPWindowSettings.ButtonStyleEnum.Launcher ||
                settings.ButtonStyleToDisplay == ARPWindowSettings.ButtonStyleEnum.StockReplace)
                return MouseOverAppLauncherBtn && !settings.DisableHover ||
                       windowMain.Visible && windowMain.WindowRect.Contains(Event.current.mousePosition) ||
                       windowSettings.Visible && windowSettings.WindowRect.Contains(Event.current.mousePosition);

            //are we painting?
            bool blnRet = Event.current.type == EventType.Repaint;

            //And the mouse is over the button - if hovering is not disabled
            blnRet = blnRet && rectButton.Contains(Event.current.mousePosition) && !settings.DisableHover;

            //mouse in main window
            blnRet = blnRet || windowMain.Visible && windowMain.WindowRect.Contains(Event.current.mousePosition);

            ////or, the form was on the screen and the mouse is over that rectangle
            //blnRet = blnRet || (Drawing && rectPanel.Contains(Event.current.mousePosition));

            ////or, the settings form was on the screen and the mouse is over that rectangle
            blnRet = blnRet || windowSettings.Visible &&
                windowSettings.WindowRect.Contains(Event.current.mousePosition);

            return blnRet;
        }


        /// <summary>
        ///     Heres where the heavy lifting should occur
        /// </summary>
        internal override void RepeatingWorker()
        {
            if (StockAppToBeHidden)
                ReplaceStockAppButton();

            if (AppLauncherToBeSetTrue &&
                (settings.ButtonStyleToDisplay == ARPWindowSettings.ButtonStyleEnum.Launcher ||
                 settings.ButtonStyleToDisplay == ARPWindowSettings.ButtonStyleEnum.StockReplace)
            )
                SetAppButtonToTrue();

            List<Vessel> vessels;
            if (settings.ShowBase)
            {
                var currentPosition = FlightGlobals.ActiveVessel.GetWorldPos3D();
                vessels = FlightGlobals.Vessels.Where(v =>
                    v.mainBody == FlightGlobals.ActiveVessel.mainBody &&
                    Vector3d.Distance(currentPosition, v.GetWorldPos3D()) < baseRange).ToList();
            }
            else
            {
                vessels = new List<Vessel> {FlightGlobals.ActiveVessel};
            }

            var RatePeriod = RepeatingWorkerUTPeriod;
            if (!settings.RatesUseUT) RatePeriod = RepeatingWorkerUTPeriod / TimeWarp.CurrentRate;

            //trigger the start loop and store the UT that has passed - only calc rates where necessary
            lstResourcesVessel.StartUpdatingList(settings.ShowRates, RatePeriod);
            lstResourcesLastStage.StartUpdatingList(settings.ShowRates, RatePeriod);

            //sort out the per stage list
            for (var i = 0; i <= StageManager.StageCount; i++)
            {
                if (!lstResourcesVesselPerStage.ContainsKey(i))
                    lstResourcesVesselPerStage.Add(i,
                        new ARPResourceList(ARPResourceList.ResourceUpdate.AddValues, settings.Resources));

                lstResourcesVesselPerStage[i].StartUpdatingList(settings.ShowRates, RatePeriod);
            }

            //flush the temporary lists
            lstPartsLastStageEngines = new ARPPartList();
            lstParts = new ARPPartDefList();
            var lstVesselResourceIDs = new List<int>();

            //set the controllable flag
            blnVesselIsControllable = false;

            //Now loop through the vessels
            foreach (Vessel active in vessels)
            {
                LastStage = GetLastStage(active.parts);

                //Now loop through and update em
                foreach (Part p in active.parts)
                {
                    //Check if its a controllable vessel
                    if (active == FlightGlobals.ActiveVessel && active.IsControllable)
                        blnVesselIsControllable = true;

                    //is the part decoupled in the last stage
                    var DecoupledInLastStage = p.DecoupledAt() == LastStage;

                    foreach (PartResource pr in p.Resources)
                    {
                        //store a list of all resources in vessel so we can nuke resources from the other lists later
                        if (!lstVesselResourceIDs.Contains(pr.info.id)) lstVesselResourceIDs.Add(pr.info.id);

                        //Is this resource set to split on disabled parts instead of staging
                        if ((PartResourceLibrary.Instance.resourceDefinitions[pr.info.id].resourceFlowMode ==
                             ResourceFlowMode.ALL_VESSEL ||
                             PartResourceLibrary.Instance.resourceDefinitions[pr.info.id].resourceFlowMode ==
                             ResourceFlowMode.STAGE_PRIORITY_FLOW) &&
                            settings.Resources[pr.info.id].ShowReserveLevels)
                        {
                            if (pr.flowState)
                            {
                                //update the resource in the vessel list
                                lstResourcesVessel
                                    .UpdateResource(pr); //,InitialSettings:settings.Resources[pr.info.id]);

                                //if it dont exist in the last stage list - add a 0 value
                                if (!settings.ShowBase && !lstResourcesLastStage.ContainsKey(pr.info.id))
                                {
                                    LogFormatted_DebugOnly("Adding 0 value into last stage");
                                    PartResource prTemp = new PartResource(p)
                                        {info = pr.info, amount = 0, maxAmount = 0};
                                    lstResourcesLastStage.UpdateResource(prTemp);
                                }
                            }
                            else
                            {
                                if (!settings.ShowBase)
                                    //and if it needs to go in the last stage list
                                    lstResourcesLastStage.UpdateResource(pr);
                            }
                        }
                        else
                        {
                            //update the resource in the vessel list
                            lstResourcesVessel.UpdateResource(pr); //,InitialSettings:settings.Resources[pr.info.id]);

                            //and if it needs to go in the last stage list
                            if (DecoupledInLastStage && !settings.ShowBase) lstResourcesLastStage.UpdateResource(pr);
                        }

                        //Update the whole vessel list
                        lstResourcesVesselPerStage[p.DecoupledAt().Clamp(0, StageManager.StageCount)]
                            .UpdateResource(pr);

                        //is the resource in the selected list
                        if (SelectedResources.ContainsKey(pr.info.id) && SelectedResources[pr.info.id].AllVisible &&
                            !settings.Resources[pr.info.id].ShowReserveLevels)
                            lstPartWindows.AddPartWindow(p, pr, this, RatePeriod);
                        else if (SelectedResources.ContainsKey(pr.info.id) &&
                                 SelectedResources[pr.info.id].LastStageVisible && DecoupledInLastStage &&
                                 !settings.Resources[pr.info.id].ShowReserveLevels)
                            lstPartWindows.AddPartWindow(p, pr, this, RatePeriod);
                        else if (SelectedResources.ContainsKey(pr.info.id) &&
                                 SelectedResources[pr.info.id].AllVisible &&
                                 settings.Resources[pr.info.id].ShowReserveLevels && pr.flowState)
                            lstPartWindows.AddPartWindow(p, pr, this, RatePeriod);
                        else if (SelectedResources.ContainsKey(pr.info.id) &&
                                 SelectedResources[pr.info.id].LastStageVisible &&
                                 settings.Resources[pr.info.id].ShowReserveLevels && !pr.flowState)
                            lstPartWindows.AddPartWindow(p, pr, this, RatePeriod);
                        else if (lstPartWindows.ContainsKey(p.GetInstanceID()))
                            //or not,but the window is in the list
                            if (lstPartWindows[p.GetInstanceID()].ResourceList.ContainsKey(pr.info.id))
                                lstPartWindows[p.GetInstanceID()].ResourceList.Remove(pr.info.id);
                    }

                    if (!settings.ShowBase && DecoupledInLastStage &&
                        (p.Modules.OfType<ModuleEngines>().Any() || p.Modules.OfType<ModuleEnginesFX>().Any()))
                        //Add the part to the engines list for the active stage
                        lstPartsLastStageEngines.Add(p);
                }
            }

            //Destroy the windows that have no resources selected to display
            lstPartWindows.CleanWindows();

            //Remove Resources that no longer exist in vessel
            lstResourcesVessel.CleanResources(lstVesselResourceIDs);
            lstResourcesLastStage.CleanResources(lstVesselResourceIDs);

            //Finalise the list updates - calc rates and set alarm flags
            lstResourcesVessel.EndUpdatingList(settings.ShowRates);
            lstResourcesLastStage.EndUpdatingList(settings.ShowRates);

            foreach (var lstStage in lstResourcesVesselPerStage.Values)
            {
                lstStage.CleanResources(lstVesselResourceIDs);
                lstStage.EndUpdatingList(settings.ShowRates);
            }

            //List<Int32> StagesToDelete = lstResourcesVesselPerStage.Select(x => x.Key).Where(x => x > StageManager.StageCount).ToList();
            //foreach (Int32 stageID in StagesToDelete) {
            //    LogFormatted_DebugOnly("Removing Stage {0}", stageID);
            //    lstResourcesVesselPerStage.Remove(stageID);
            //}

//#if DEBUG
//            String File = "";
//            File += String.Format("Stage,Name,Amount\r\n");
//            for (int i = 0; i < lstResourcesVesselPerStage.Count; i++)
//            {
//                ARPResourceList tmp = lstResourcesVesselPerStage.OrderBy(x => x.Key).ToList()[i].Value;
//                foreach (ARPResource item in tmp.Values)
//                {
//                    String strline = "";

//                    strline += String.Format("{0},{1},{2:0}", i, item.ResourceDef.name, item.Amount);

//                    File += strline + "\r\n";
//                }
//            }

//            System.IO.File.WriteAllText(String.Format("{0}/AllStages.csv", Resources.PathPlugin), File);
//#endif 

            //Set the alarm flags
            foreach (var r in lstResourcesVessel.Values) r.SetMonitors();

            //work out if we have to kill the audio
            if (StageCheckAlarmAudio)
            {
                StageCheckAlarmAudio = false;
                if (audioController.isPlaying())
                {
                    var AudioShouldBePlaying = false;
                    foreach (var r in lstResourcesVessel.Values)
                        if (r.AlarmState == ARPResource.AlarmStateEnum.Unacknowledged &&
                            r.MonitorState != ARPResource.MonitorStateEnum.None)
                        {
                            AudioShouldBePlaying = true;
                            break;
                        }

                    if (!AudioShouldBePlaying)
                        audioController.Stop();
                }
            }

            //Build the displayList
            lstResourcesToDisplay = new List<int>();
            for (var i = 0; i < settings.Resources.Count; i++)
            {
                var ResourceID = settings.Resources.Keys.ElementAt(i);

                if (settings.Resources[ResourceID].IsSeparator)
                {
                    if (lstResourcesToDisplay.Count > 0 && lstResourcesToDisplay.Last() != 0) //Dont add double seps
                        lstResourcesToDisplay.Add(0);
                    continue;
                }

                //only add resources in the vessel
                if (!lstResourcesVessel.ContainsKey(ResourceID)) continue;

                //skip to next item if this one is a) Hidden b) Set to threshold and not flagged c)Empty and empty timer is done
                //show everything of showall is enabled
                if (!ShowAll)
                {
                    if (settings.Resources[ResourceID].Visibility == ResourceSettings.VisibilityTypes.Hidden)
                    {
                        continue;
                    }

                    if (settings.Resources[ResourceID].Visibility == ResourceSettings.VisibilityTypes.Threshold)
                    {
                        if (lstResourcesVessel[ResourceID].MonitorState == ARPResource.MonitorStateEnum.None)
                            continue;
                    }
                    else if (settings.HideEmptyResources && settings.Resources[ResourceID].HideWhenEmpty &&
                             lstResourcesVessel[ResourceID].IsEmpty)
                    {
                        //if the alarms not firing and the time has passed then hide it
                        if (lstResourcesVessel[ResourceID].AlarmState != ARPResource.AlarmStateEnum.Unacknowledged &&
                            lstResourcesVessel[ResourceID].EmptyAt < DateTime.Now.AddSeconds(-settings.HideAfter))
                            continue;
                    }
                    else if (settings.HideFullResources && settings.Resources[ResourceID].HideWhenFull &&
                             lstResourcesVessel[ResourceID].IsFull)
                    {
                        //if the alarms not firing and the time has passed then hide it
                        if (lstResourcesVessel[ResourceID].AlarmState != ARPResource.AlarmStateEnum.Unacknowledged &&
                            lstResourcesVessel[ResourceID].FullAt < DateTime.Now.AddSeconds(-settings.HideAfter))
                            continue;
                    }
                }

                //if we get this far then add it to the list
                lstResourcesToDisplay.Add(ResourceID);
            }

            //remove starting or ending seps
            if (lstResourcesToDisplay.Count > 0)
            {
                while (lstResourcesToDisplay.First() == 0)
                    lstResourcesToDisplay.RemoveAt(0);
                while (lstResourcesToDisplay.Last() == 0)
                    lstResourcesToDisplay.RemoveAt(lstResourcesToDisplay.Count - 1);
            }

            //Calc window widths/heights
            windowMain.IconAlarmOffset = 12;
            if (!settings.AlarmsEnabled)
                windowMain.IconAlarmOffset = 0;

            windowMain.WindowRect.width = 329 + windowMain.IconAlarmOffset; //was 299 - adding 30
            windowMain.Icon2BarOffset_Left = 40 + windowMain.IconAlarmOffset;
            windowMain.Icon2BarOffset_Right = 40 + 140 + windowMain.IconAlarmOffset;

            if (lstResourcesToDisplay.Count == 0)
                windowMain.WindowRect.height = 2 * windowMain.intLineHeight + 16;
            else
                //this is resources lines - separators diff + fixed value
                windowMain.WindowRect.height = (lstResourcesToDisplay.Count + 1) * windowMain.intLineHeight -
                    lstResourcesToDisplay.Count(x => x == 0) *
                    (15 - settings.SpacerPadding * 2) + 12;
            if (settings.AutoStagingEnabled)
                windowMain.WindowRect.height += 24;

            //now do the autostaging stuff
            lstLastStageEngineModules =
                lstPartsLastStageEngines.SelectMany(x => x.Modules.OfType<ModuleEngines>()).ToList();
            lstLastStageEngineFXModules =
                lstPartsLastStageEngines.SelectMany(x => x.Modules.OfType<ModuleEnginesFX>()).ToList();
            AutoStagingMaxStage = Mathf.Min(AutoStagingMaxStage, StageManager.StageCount - 1);
            AutoStagingTerminateAt = Mathf.Min(AutoStagingTerminateAt, AutoStagingMaxStage);
            AutoStagingStatusColor = Color.white;
            if (settings.AutoStagingEnabled)
            {
                if (AutoStagingArmed)
                {
                    if (!AutoStagingRunning)
                    {
                        AutoStagingStatusColor = Color.white;
                        AutoStagingStatus = "Armed... waiting for stage";
                        //when to set it to running
                        if (lstLastStageEngineModules.Any(x => x.staged) ||
                            lstLastStageEngineFXModules.Any(x => x.staged))
                            AutoStagingRunning = true;
                    }
                    else if (StageManager.CurrentStage > AutoStagingTerminateAt)
                    {
                        //we are running, so now what
                        AutoStagingStatusColor = new Color32(183, 254, 0, 255);
                        AutoStagingStatus = "Running";

                        //are all the engines that are active flamed out in the last stage
                        if (AutoStagingTriggeredAt == 0 && lstPartsLastStageEngines.Count > 0 &&
                            lstLastStageEngineModules.Where(x => x.staged).All(x => x.getFlameoutState) &&
                            lstLastStageEngineFXModules.Where(x => x.staged).All(x => x.getFlameoutState))
                        {
                            LogFormatted_DebugOnly("Engine Flameouts Detected");
                            AutoStagingTriggeredAt = Planetarium.GetUniversalTime();
                        }
                        else if (AutoStagingTriggeredAt == 0 && (lstResourcesLastStage.Sum(r => r.Value.Amount) <= 0) &
                            (lstPartsLastStageEngines.Count == 0))
                        {
                            //or is the last stage just emoty tanks?
                            LogFormatted_DebugOnly("Empty Resource Stage Detected");
                            AutoStagingTriggeredAt = Planetarium.GetUniversalTime();
                        }
                        else if (AutoStagingTriggeredAt != 0)
                        {
                            if (Planetarium.GetUniversalTime() - AutoStagingTriggeredAt >
                                (double) settings.AutoStagingDelayInTenths / 10)
                            {
                                //if (!settings.StagingIgnoreLock || StageManager.stackLocked || FlightInputHandler.fetch.stageLock)
                                LogFormatted("Autostage Triggered: {0}->{1}", StageManager.CurrentStage,
                                    StageManager.CurrentStage - 1);
                                StageManager.ActivateNextStage();
                                AutoStagingTriggeredAt = 0;
                            }
                            else
                            {
                                AutoStagingStatusColor = new Color32(232, 232, 0, 255);
                                AutoStagingStatus = string.Format("Delay:{0:0.0}s",
                                    (double) settings.AutoStagingDelayInTenths / 10 -
                                    (Planetarium.GetUniversalTime() - AutoStagingTriggeredAt));
                            }
                        }
                    }
                    else
                    {
                        LogFormatted("Autostaging Run Complete");
                        AutoStagingTriggeredAt = Planetarium.GetUniversalTime();
                        AutoStagingRunning = false;
                        AutoStagingArmed = false;
                    }
                }
                else
                {
                    if (AutoStagingTriggeredAt == 0)
                    {
                        AutoStagingStatusColor = new Color32(140, 140, 140, 255);
                        AutoStagingStatus = "Not Armed";
                    }
                    else
                    {
                        if (Planetarium.GetUniversalTime() - AutoStagingTriggeredAt > 3)
                        {
                            AutoStagingTriggeredAt = 0;
                        }
                        else
                        {
                            AutoStagingStatus = "Run Complete";
                            AutoStagingStatusColor = new Color32(183, 254, 0, 255);
                        }
                    }
                }
            }

            //Transfers??
            //lstString = new List<string>();
            if (lstTransfers.Count > 1)
                //look for matches
                foreach (var item in lstTransfers.GroupBy(x => x.ResourceID))
                    //for each resource
                    //String strTemp = item.First().resource.name;

                    //if there is an in and an out
                    if (item.Any(x => x.transferState == TransferStateEnum.In) &&
                        item.Any(x => x.transferState == TransferStateEnum.Out))
                        //set the transfer state for those parts to be active - and set the rate of this part
                        foreach (var t in item.Where(x => x.transferState != TransferStateEnum.None))
                        {
                            t.Active = true;
                            t.RatePerSec = (float) lstPartWindows[t.partID].ResourceList[t.ResourceID].MaxAmount / 20;
                        }
                    //strTemp += "Transfer";
                    else
                        //otherwise set the transfers to be inactive for all parts
                        foreach (var t in item)
                            t.Active = false;
                    //strTemp += "Waiting";
                //lstString.Add(strTemp);
        }

        internal override void Update()
        {
            //Activate Stage via Space Bar in MapView if enabled and possible
            if (settings.StagingEnabledSpaceInMapView && MapView.MapIsEnabled && windowMain.Visible &&
                blnVesselIsControllable && Input.GetKey(KeyCode.Space))
                StageManager.ActivateNextStage();
        }

        //internal String TestTrans;
        internal override void FixedUpdate()
        {
            //Do the transfers
            foreach (var tGroup in lstTransfers.Where(x => x.Active).GroupBy(x => x.ResourceID))
            {
                //If there is not at least one out and one in then try the next resource
                if (tGroup.FirstOrDefault(x => x.transferState == TransferStateEnum.Out) == null ||
                    tGroup.FirstOrDefault(x => x.transferState == TransferStateEnum.In) == null) continue;

                ////Now get the Outs and Ins for this grouping
                var OutTrans = tGroup.Where(x => x.transferState == TransferStateEnum.Out).ToList();
                var InTrans = tGroup.Where(x => x.transferState == TransferStateEnum.In).OrderBy(tr =>
                    tr.part.Resources.Get(tGroup.Key).maxAmount - tr.part.Resources.Get(tGroup.Key).amount).ToList();

                //Ensure there is at least one tank to transfer from
                if (OutTrans.Where(trans => trans.part.Resources.Get(tGroup.Key).amount > 0).Count() < 1) continue;
                //Ensure there is at least one tank to transfer into
                if (InTrans.Where(trans =>
                    trans.part.Resources.Get(tGroup.Key).maxAmount - trans.part.Resources.Get(tGroup.Key).amount >
                    0).Count() < 1) continue;

                //Transfer Rate - Max transfer we can do at that rate
                double TransferRate = Math.Max(InTrans.Max(t => t.RatePerSec), OutTrans.Max(t => t.RatePerSec));
                //How much stuff is that
                var RequestAmountPerPart = TransferRate * Time.deltaTime;
                var RequestAmount = RequestAmountPerPart * Math.Min(InTrans.Count, OutTrans.Count);

                //And therefore how much out and in per part at the max rate
                var RequestAmountOutPerPart = RequestAmount /
                                              OutTrans.Where(trans => trans.part.Resources.Get(tGroup.Key).amount > 0)
                                                  .Count();
                var RequestAmountInPerPart = RequestAmount / InTrans.Where(trans =>
                    trans.part.Resources.Get(tGroup.Key).maxAmount -
                    trans.part.Resources.Get(tGroup.Key).amount > 0).Count();

                //LogFormatted("!!-{0}-{1}-{2}-{3}-{4}", TransferRate, RequestAmountPerPart, RequestAmount, RequestAmountOutPerPart,RequestAmountInPerPart );

                //Loop through the out tanks to see how much in total we can get out at that rate (some tanks may not have the amount avail
                double AmountOutPossible = 0;
                foreach (var trans in OutTrans)
                    AmountOutPossible += Math.Min(trans.part.Resources.Get(tGroup.Key).amount,
                        RequestAmountOutPerPart);
                //Loop through the out tanks to see how much in total we can get in at that rate - some tanls may be empty
                double AmountInPossible = 0;
                foreach (var trans in InTrans)
                    AmountInPossible += Math.Min(
                        trans.part.Resources.Get(tGroup.Key).maxAmount - trans.part.Resources.Get(tGroup.Key).amount,
                        RequestAmountInPerPart);

                //Whats the Actual transfer amount
                var AmountPossible = Math.Min(AmountOutPossible, AmountInPossible);
                //Double AmountToTransfer = Math.Min(RequestAmount, AmountPossible);
                //And how much are we getting out of each tank
                var AmountToTransferOutPerPart = AmountPossible /
                                                 OutTrans.Where(
                                                     trans => trans.part.Resources.Get(tGroup.Key).amount > 0).Count();

                //LogFormatted("~~-{0}-{1}-{2}-{3}", AmountOutPossible, AmountInPossible, AmountPossible, AmountToTransferOutPerPart);

                double ResourceBus = 0;
                //Loop Through the Source Parts to put an Amount on "the bus"
                foreach (var trans in OutTrans)
                    //LogFormatted("A{0}-{1}", trans.part.Resources.Get(tGroup.Key).amount, ResourceBus);
                    if (trans.part.Resources.Get(tGroup.Key).amount <= AmountToTransferOutPerPart)
                    {
                        ResourceBus += trans.part.Resources.Get(tGroup.Key).amount;
                        trans.part.Resources.Get(tGroup.Key).amount = 0;
                    }
                    else
                    {
                        ResourceBus += AmountToTransferOutPerPart;
                        trans.part.Resources.Get(tGroup.Key).amount -= AmountToTransferOutPerPart;
                    }
                //LogFormatted("B{0}-{1}", trans.part.Resources.Get(tGroup.Key).amount, ResourceBus);

                var AmountToTransferInPerPart = ResourceBus / InTrans.Count;
                //LogFormatted("Bus={0}-{1}", ResourceBus,AmountToTransferInPerPart);

                //Loop through the Target Parts and Deliver from the bus
                for (var i = 0; i < InTrans.Count; i++)
                {
                    //get the transfer at i when ordered by remaining capacity
                    var trans = InTrans[i];
                    //Check how much we can jam in the current target part
                    double Capacity = trans.part.Resources.Get(tGroup.Key).maxAmount -
                                      trans.part.Resources.Get(tGroup.Key).amount;

                    //LogFormatted("C{0}-{1}", trans.part.Resources.Get(tGroup.Key).amount, ResourceBus);
                    //if there is space then jam it in
                    if (Capacity > AmountToTransferInPerPart)
                    {
                        ResourceBus -= AmountToTransferInPerPart;
                        //tGroup.First(t => t.partID == trans.partID).part.Resources.Get(tGroup.Key).amount += AmountToTransferInPerPart;
                        trans.part.Resources.Get(tGroup.Key).amount += AmountToTransferInPerPart;
                    }
                    else
                    {
                        //if not then fill the tank and then recalc the amount per part figure
                        ResourceBus -= Capacity;
                        //tGroup.First(t => t.partID == trans.partID).part.Resources.Get(tGroup.Key).amount += AmountToTransferInPerPart;
                        trans.part.Resources.Get(tGroup.Key).amount = trans.part.Resources.Get(tGroup.Key).maxAmount;

                        //Recal how much ofr the remaining parts to get off the bus
                        AmountToTransferInPerPart = ResourceBus / (InTrans.Count - i - 1);
                    }

                    //LogFormatted("D{0}-{1}", trans.part.Resources.Get(tGroup.Key).amount, ResourceBus);
                }

                if (ResourceBus > 0) LogFormatted("ERROR: The bus didnt get cleared ({0})", ResourceBus);
                //LogFormatted("BusEnd={0}", ResourceBus);


                //LogFormatted_DebugOnly("ResourceBusRemaining={0}", ResourceBus);
                //Loop through the Source Parts and Refund any that didnt fit

                //ARPTransfer OutTrans = t.First(x => x.transferState == TransferStateEnum.Out);
                //ARPTransfer InTrans = t.First(x => x.transferState == TransferStateEnum.In);

                //Single TransferRate = Mathf.Max(InTrans.RatePerSec, OutTrans.RatePerSec);

                //Single RequestAmount = TransferRate * Time.deltaTime;
                //Single AmountOutPossible = (Single)OutTrans.part.Resources.Get(t.Key).amount;
                //Single AmountInPossible = (Single)InTrans.part.Resources.Get(t.Key).maxAmount - (Single)InTrans.part.Resources.Get(t.Key).amount;
                //Single AmountPossible = Mathf.Min(AmountOutPossible, AmountInPossible);

                //Single AmountToTransfer = Mathf.Min(RequestAmount, AmountPossible);

                //OutTrans.part.Resources.Get(t.Key).amount -= AmountToTransfer;
                //InTrans.part.Resources.Get(t.Key).amount += AmountToTransfer;
                //OutTrans.part.RequestResource(t.Key, AmountToTransfer);
                //InTrans.part.RequestResource(t.Key, -AmountToTransfer);
                //TestTrans = String.Format("{0}->{1}", OutTrans.partID, InTrans.partID);
            }
        }

        internal override void LateUpdate()
        {
            //position the part windows
            if (lstPartWindows.Count > 0)
            {
                vectVesselCOMScreen = FlightCamera.fetch.mainCamera.WorldToScreenPoint(FlightGlobals.ActiveVessel.CoM);

                var dteStart = DateTime.Now;

                var LeftWindows = lstPartWindows.Values.Where(x => x.LeftSide).OrderByDescending(x => x.PartScreenPos.y)
                    .ToList();
                if (LeftWindows.Count > 0)
                {
                    double LeftPos = lstPartWindows.Values.Where(x => x.LeftSide).Min(x => x.PartScreenPos.x) -
                                     ARPPartWindow.WindowOffset - ARPPartWindow.WindowWidthForBars / 2;
                    foreach (var pwTemp in LeftWindows)
                    {
                        pwTemp.WindowRect.y =
                            Screen.height - (float) pwTemp.PartScreenPos.y -
                            pwTemp.WindowRect.height / 2; //this sets an initial y used for sorting later
                        pwTemp.WindowRect.x = (int) LeftPos;
                    }

                    UpdateWindowYs(LeftWindows);
                }

                var RightWindows = lstPartWindows.Values.Where(x => !x.LeftSide)
                    .OrderByDescending(x => x.PartScreenPos.y).ToList();
                if (RightWindows.Count > 0)
                {
                    double RightPos = lstPartWindows.Values.Where(x => !x.LeftSide).Max(x => x.PartScreenPos.x) +
                        ARPPartWindow.WindowOffset - ARPPartWindow.WindowWidthForBars / 2;
                    foreach (var pwTemp in RightWindows)
                    {
                        pwTemp.WindowRect.y =
                            Screen.height - (float) pwTemp.PartScreenPos.y -
                            pwTemp.WindowRect.height / 2; //this sets an initial y used for sorting later
                        pwTemp.WindowRect.x = (int) RightPos;
                    }

                    UpdateWindowYs(RightWindows);
                }

                //Now update the lines
                //foreach (ARPPartWindow pwTemp in lstPartWindows.Values)
                //    pwTemp.UpdateLinePos();
            }
        }

        /// <summary>
        ///     Worker that spaces out the windows in the Y axis to pad em
        /// </summary>
        /// <param name="ListOfWindows"></param>
        private static void UpdateWindowYs(List<ARPPartWindow> ListOfWindows)
        {
            var MiddleNum = ListOfWindows.Count() / 2;
            for (var i = MiddleNum + 1; i < ListOfWindows.Count(); i++)
                if (ListOfWindows[i - 1].ScreenNextWindowY > ListOfWindows[i].WindowRect.y)
                    ListOfWindows[i].WindowRect.y = ListOfWindows[i - 1].ScreenNextWindowY;
            for (var i = MiddleNum - 1; i >= 0; i--)
                if (ListOfWindows[i].ScreenNextWindowY > ListOfWindows[i + 1].WindowRect.y)
                    ListOfWindows[i].WindowRect.y = ListOfWindows[i + 1].WindowRect.y -
                                                    ListOfWindows[i].WindowRect.height -
                                                    ARPPartWindow.WindowSpaceOffset;
        }


        /// <summary>
        ///     Should be self explanatory
        /// </summary>
        /// <param name="Parts"></param>
        /// <returns>Largest Stage # in Parts list</returns>
        private int GetLastStage(List<Part> Parts)
        {
            if (Parts.Count > 0)
                return Parts.Max(x => x.DecoupledAt());
            return -1;
        }

        #region Toolbar Stuff

        /// <summary>
        ///     initialises a Toolbar Button for this mod
        /// </summary>
        /// <returns>The ToolbarButtonWrapper that was created</returns>
        internal IButton InitToolbarButton()
        {
            IButton btnReturn;
            try
            {
                LogFormatted("Initialising the Toolbar Icon");
                btnReturn = ToolbarManager.Instance.add(_ClassName, "btnToolbarIcon");
                SetToolbarIcon(btnReturn);
                btnReturn.ToolTip = "Alternate Resource Panel";
                btnReturn.OnClick += e =>
                {
                    settings.ToggleOn = !settings.ToggleOn;
                    SetToolbarIcon(e.Button);
                    MouseOverToolbarBtn = true;
                    settings.Save();
                };
                btnReturn.OnMouseEnter += btnReturn_OnMouseEnter;
                btnReturn.OnMouseLeave += btnReturn_OnMouseLeave;
            }
            catch (Exception ex)
            {
                btnReturn = null;
                LogFormatted("Error Initialising Toolbar Button: {0}", ex.Message);
            }

            return btnReturn;
        }

        private static void SetToolbarIcon(IButton btnReturn)
        {
            //if (settings.ToggleOn) 
            //btnReturn.TexturePath = "TriggerTech/KSPAlternateResourcePanel/ToolbarIcons/KSPARPa_On";
            //else
            //btnReturn.TexturePath = "TriggerTech/KSPAlternateResourcePanel/ToolbarIcons/KSPARPa";
            var strToolbarIcon =
                Resources.PathPluginToolbarIcons.Substring(
                    Resources.PathPluginToolbarIcons.ToLower().IndexOf("/gamedata/") + 10) + "/KSPARPa";
            btnReturn.TexturePath = strToolbarIcon; // "TriggerTech/TransferWindowPlanner/ToolbarIcons/TWPIcon";
        }

        private void btnReturn_OnMouseLeave(MouseLeaveEvent e)
        {
            MouseOverToolbarBtn = false;
        }

        internal bool MouseOverToolbarBtn;

        private void btnReturn_OnMouseEnter(MouseEnterEvent e)
        {
            MouseOverToolbarBtn = true;
        }

        /// <summary>
        ///     Destroys theToolbarButtonWrapper object
        /// </summary>
        /// <param name="btnToDestroy">Object to Destroy</param>
        internal void DestroyToolbarButton(IButton btnToDestroy)
        {
            if (btnToDestroy != null)
            {
                LogFormatted("Destroying Toolbar Button");
                btnToDestroy.Destroy();
            }

            btnToDestroy = null;
        }

        #endregion
    }


#if DEBUG
    ////This will kick us into the save called default and set the first vessel active
    //[KSPAddon(KSPAddon.Startup.MainMenu, false)]
    //public class Debug_AutoLoadPersistentSaveOnStartup : MonoBehaviour
    //{
    //    //use this variable for first run to avoid the issue with when this is true and multiple addons use it
    //    public static bool first = true;
    //    public void Start()
    //    {
    //        //only do it on the first entry to the menu
    //        if (first)
    //        {
    //            first = false;
    //            HighLogic.SaveFolder = "default";
    //            Game game = GamePersistence.LoadGame("persistent", HighLogic.SaveFolder, true, false);

    //            if (game != null && game.flightState != null && game.compatible)
    //            {
    //                HighLogic.CurrentGame = game;

    //                Int32 FirstVessel;
    //                Boolean blnFoundVessel=false;
    //                for (FirstVessel = 0; FirstVessel < game.flightState.protoVessels.Count; FirstVessel++)
    //                {
    //                    if (game.flightState.protoVessels[FirstVessel].vesselType != VesselType.SpaceObject &&
    //                        game.flightState.protoVessels[FirstVessel].vesselType != VesselType.Unknown)
    //                    {
    //                        blnFoundVessel = true;
    //                        break;
    //                    }
    //                }
    //                if (!blnFoundVessel)
    //                    HighLogic.LoadScene(GameScenes.SPACECENTER);
    //                else 
    //                    FlightDriver.StartAndFocusVessel(game, FirstVessel);
    //            }

    //            //CheatOptions.InfiniteFuel = true;
    //        }
    //    }
    //}
#endif
}