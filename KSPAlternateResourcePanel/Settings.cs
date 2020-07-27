using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using KSPPluginFramework;

namespace KSPAlternateResourcePanel
{
    internal class Settings : ConfigNodeStorage
    {
        [Persistent] internal int AlarmsAlertRepeats = 4;

        [Persistent] internal string AlarmsAlertSound = "_DefaultAlert";

        [Persistent] internal bool AlarmsEnabled = true;
        [Persistent] internal float AlarmsVolume = 0.25f;

        [Persistent] internal bool AlarmsVolumeFromUI = true;
        [Persistent] internal int AlarmsWarningRepeats = 2;
        [Persistent] internal string AlarmsWarningSound = "_DefaultWarning";
        [Persistent] internal bool AppLauncherMutuallyExclusive = true;
        [Persistent] internal int AppLauncherSetTrueTimeOut = 6;
        [Persistent] internal int AutoStagingDelayInTenths = 5;

        [Persistent] internal bool AutoStagingEnabled = false;

        internal bool BlizzyToolbarIsAvailable = false;
        [Persistent] internal bool ButtonPosUpdatedv24 = false;

        [Persistent]
        internal ARPWindowSettings.ButtonStyleEnum ButtonStyleChosen = ARPWindowSettings.ButtonStyleEnum.Launcher;

        //Version Stuff
        [Persistent] internal bool DailyVersionCheck = true;
        [Persistent] internal bool DisableHover = false;
        [Persistent] internal int HideAfter = 2;
        [Persistent] internal bool HideEmptyResources = false;
        [Persistent] internal bool HideFullResources = false;
        [Persistent] internal bool LockLocation = true;

        //Reversing order so player creations are higher up the list
        //[Persistent] internal List<String> lstIconOrder = new List<String>() { "KSPARP", "Mod", "Player" };
        [Persistent] internal List<string> lstIconOrder = new List<string> {"Player", "Mod", "KSPARP"};
        [Persistent] internal RateDisplayEnum RateDisplayType = RateDisplayEnum.Default;

        /// <summary>
        ///     Whether the rates are calculated versus UT periods or Real time periods
        /// </summary>
        [Persistent] internal bool RatesUseUT = true;

        [Persistent] internal int ReplaceStockAppTimeOut = 20;

        internal Dictionary<int, ResourceSettings> Resources = new Dictionary<int, ResourceSettings>();
        [Persistent] private List<ResourceSettings> ResourcesStorage = new List<ResourceSettings>();
        [Persistent] internal DisplaySkin SelectedSkin = DisplaySkin.Default;
        internal bool ShowBase = false;

        [Persistent] internal bool ShowRates = true;
        [Persistent] internal bool ShowRatesForParts = true;

        //[Persistent] 
        internal bool ShowTimeRem = false;

        [Persistent] internal bool ShowWindowOnResourceMonitor = true;

        [Persistent] internal int SpacerPadding = 0;

        [Persistent] internal bool SplitLastStage = true;
        [Persistent] internal bool StageBarOnRight = true;


        [Persistent] internal bool StagingEnabled = false;

        //[Persistent] internal Boolean StagingIgnoreStageLock = true;
        [Persistent] internal bool StagingEnabledInMapView = false;
        [Persistent] internal bool StagingEnabledSpaceInMapView = false;

        [Persistent] internal bool ToggleOn = false;
        [Persistent] internal bool UseBlizzyToolbarIfAvailable = false;

        [Persistent] internal Vector3 vectButtonPos = new Vector3(Screen.width - 405, 0, 0);

        internal bool VersionAttentionFlag;

        //When did we last check??
        internal DateTime VersionCheckDate_Attempt;
        [Persistent] internal string VersionCheckDate_AttemptStored;
        internal DateTime VersionCheckDate_Success;
        [Persistent] internal string VersionCheckDate_SuccessStored;
        internal Rect WindowPosition = new Rect(Screen.width - 381, 0, 299, 20);
        [Persistent] private RectStorage WindowPositionStored = new RectStorage();
        [Persistent] internal bool WindowPosUpdatedv24 = false;

        internal Settings(string FilePath) : base(FilePath)
        {
            Version = Assembly.GetExecutingAssembly().GetName().Version.ToString();
            //on each start set the attention flag to the property - should be on each program start
            VersionAttentionFlag = VersionAvailable;

            OnEncodeToConfigNode();
        }

        internal string AlarmsWarningRepeatsText => RepeatsToString(AlarmsWarningRepeats);
        internal string AlarmsAlertRepeatsText => RepeatsToString(AlarmsAlertRepeats);

        internal ARPWindowSettings.ButtonStyleEnum ButtonStyleToDisplay
        {
            get
            {
                if (BlizzyToolbarIsAvailable || ButtonStyleChosen != ARPWindowSettings.ButtonStyleEnum.Toolbar)
                    return ButtonStyleChosen;
                return ARPWindowSettings.ButtonStyleEnum.Basic;
            }
        }

        public string VersionCheckDate_AttemptString => ConvertVersionCheckDateToString(VersionCheckDate_Attempt);
        public string VersionCheckDate_SuccessString => ConvertVersionCheckDateToString(VersionCheckDate_Success);

        private string RepeatsToString(int value)
        {
            if (value > 5)
                return "For Ever";
            return value + " Time(s)";
        }

        public override void OnEncodeToConfigNode()
        {
            WindowPositionStored = WindowPositionStored.FromRect(WindowPosition);
            ResourcesStorage = Resources.Values.ToList();
            VersionCheckDate_AttemptStored = VersionCheckDate_AttemptString;
            VersionCheckDate_SuccessStored = VersionCheckDate_SuccessString;
        }

        public override void OnDecodeFromConfigNode()
        {
            WindowPosition = WindowPositionStored.ToRect();
            Resources = ResourcesStorage.ToDictionary(x => x.id);
            DateTime.TryParseExact(VersionCheckDate_AttemptStored, "yyyy-MM-dd", null, DateTimeStyles.None,
                out VersionCheckDate_Attempt);
            DateTime.TryParseExact(VersionCheckDate_SuccessStored, "yyyy-MM-dd", null, DateTimeStyles.None,
                out VersionCheckDate_Success);
        }


        internal enum DisplaySkin
        {
            [Description("KSP Style")] Default,
            [Description("Unity Style")] Unity,
            [Description("Unity/KSP Buttons")] UnityWKSPButtons
        }

        internal enum RateDisplayEnum
        {
            [Description("KSP Default")] Default,
            [Description("Left/Right")] LeftRight,
            [Description("Left/Right + Text")] LeftRightPlus,
            [Description("Up/Down")] UpDown,
            [Description("Up/Down + Text")] UpDownPlus
        }


        #region Version Checks

        private readonly string VersionCheckURL =
            "http://triggerau.github.io/KSPAlternateResourcePanel/versioncheck.txt";
        //Could use this one to see usage, but need to be very aware of data connectivity if its ever used "http://bit.ly/TWPVersion";

        private string ConvertVersionCheckDateToString(DateTime Date)
        {
            if (Date < DateTime.Now.AddYears(-10))
                return "No Date Recorded";
            return string.Format("{0:yyyy-MM-dd}", Date);
        }

        public string Version = "";

        [Persistent] public string VersionWeb = "";

        public bool VersionAvailable
        {
            get
            {
                //todo take this out
                if (VersionWeb == "")
                    return false;
                try
                {
                    //if there was a string and its version is greater than the current running one then alert
                    var vTest = new Version(VersionWeb);
                    return Assembly.GetExecutingAssembly().GetName().Version.CompareTo(vTest) < 0;
                }
                catch (Exception ex)
                {
                    MonoBehaviourExtended.LogFormatted("webversion: '{0}'", VersionWeb);
                    MonoBehaviourExtended.LogFormatted("Unable to compare versions: {0}", ex.Message);
                    return false;
                }

                //return ((this.VersionWeb != "") && (this.Version != this.VersionWeb));
            }
        }

        public string VersionCheckResult = "";


        /// <summary>
        ///     Does some logic to see if a check is needed, and returns true if there is a different version
        /// </summary>
        /// <param name="ForceCheck">Ignore all logic and simply do a check</param>
        /// <returns></returns>
        public bool VersionCheck(MonoBehaviour parent, bool ForceCheck)
        {
            var blnReturn = false;
            var blnDoCheck = false;

            try
            {
                if (!VersionCheckRunning)
                {
                    if (ForceCheck)
                    {
                        blnDoCheck = true;
                        MonoBehaviourExtended.LogFormatted("Starting Version Check-Forced");
                    }
                    //else if (this.VersionWeb == "")
                    //{
                    //    blnDoCheck = true;
                    //    MonoBehaviourExtended.LogFormatted("Starting Version Check-No current web version stored");
                    //}
                    else if (VersionCheckDate_Attempt < DateTime.Now.AddYears(-9))
                    {
                        blnDoCheck = true;
                        MonoBehaviourExtended.LogFormatted("Starting Version Check-No current date stored");
                    }
                    else if (VersionCheckDate_Attempt.Date != DateTime.Now.Date)
                    {
                        blnDoCheck = true;
                        MonoBehaviourExtended.LogFormatted("Starting Version Check-stored date is not today");
                    }
                    else
                    {
                        MonoBehaviourExtended.LogFormatted("Skipping version check");
                    }

                    if (blnDoCheck) parent.StartCoroutine(CRVersionCheck());
                }
                else
                {
                    MonoBehaviourExtended.LogFormatted("Starting Version Check-version check already running");
                }

                blnReturn = true;
            }
            catch (Exception ex)
            {
                MonoBehaviourExtended.LogFormatted("Failed to run the update test");
                MonoBehaviourExtended.LogFormatted(ex.Message);
            }

            return blnReturn;
        }

        internal bool VersionCheckRunning;

        private IEnumerator CRVersionCheck()
        {
            WWW wwwVersionCheck;

            //set initial stuff and save it
            VersionCheckRunning = true;
            VersionCheckResult = "Unknown - check again later";
            VersionCheckDate_Attempt = DateTime.Now;
            Save();

            //now do the download
            MonoBehaviourExtended.LogFormatted("Reading version from Web");
            wwwVersionCheck = new WWW(VersionCheckURL);
            yield return wwwVersionCheck;
            if (wwwVersionCheck.error == null)
                CRVersionCheck_Completed(wwwVersionCheck);
            else
                MonoBehaviourExtended.LogFormatted("Version Download failed:{0}", wwwVersionCheck.error);
            VersionCheckRunning = false;
        }

        private void CRVersionCheck_Completed(WWW wwwVersionCheck)
        {
            try
            {
                //get the response from the variable and work with it
                //Parse it for the version String
                string strFile = wwwVersionCheck.text;
                MonoBehaviourExtended.LogFormatted("Response Length:" + strFile.Length);

                Match matchVersion;
                matchVersion = Regex.Match(strFile, "(?<=\\|LATESTVERSION\\|).+(?=\\|LATESTVERSION\\|)",
                    RegexOptions.Singleline);
                MonoBehaviourExtended.LogFormatted("Got Version '" + matchVersion + "'");

                var strVersionWeb = matchVersion.ToString();
                if (strVersionWeb != "")
                {
                    VersionCheckResult = "Success";
                    VersionCheckDate_Success = DateTime.Now;
                    VersionWeb = strVersionWeb;
                }
                else
                {
                    VersionCheckResult = "Unable to parse web service";
                }
            }
            catch (Exception ex)
            {
                MonoBehaviourExtended.LogFormatted("Failed to read Version info from web");
                MonoBehaviourExtended.LogFormatted(ex.Message);
            }

            MonoBehaviourExtended.LogFormatted("Version Check result:" + VersionCheckResult);

            Save();
            VersionAttentionFlag = VersionAvailable;
        }

        //public Boolean getLatestVersion()
        //{
        //    Boolean blnReturn = false;
        //    try
        //    {
        //        //Get the file from Codeplex
        //        this.VersionCheckResult = "Unknown - check again later";
        //        this.VersionCheckDate_Attempt = DateTime.Now;

        //        MonoBehaviourExtended.LogFormatted("Reading version from Web");
        //        //Page content FormatException is |LATESTVERSION|1.2.0.0|LATESTVERSION|
        //        //                WWW www = new WWW("http://kspalternateresourcepanel.codeplex.com/wikipage?title=LatestVersion");
        //        WWW www = new WWW("https://sites.google.com/site/kspalternateresourcepanel/latestversion");
        //        while (!www.isDone) { }

        //        //Parse it for the version String
        //        String strFile = www.text;
        //        MonoBehaviourExtended.LogFormatted("Response Length:" + strFile.Length);

        //        Match matchVersion;
        //        matchVersion = Regex.Match(strFile, "(?<=\\|LATESTVERSION\\|).+(?=\\|LATESTVERSION\\|)", System.Text.RegularExpressions.RegexOptions.Singleline);
        //        MonoBehaviourExtended.LogFormatted("Got Version '" + matchVersion.ToString() + "'");

        //        String strVersionWeb = matchVersion.ToString();
        //        if (strVersionWeb != "")
        //        {
        //            this.VersionCheckResult = "Success";
        //            this.VersionCheckDate_Success = DateTime.Now;
        //            this.VersionWeb = strVersionWeb;
        //            blnReturn = true;
        //        }
        //        else
        //        {
        //            this.VersionCheckResult = "Unable to parse web service";
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        MonoBehaviourExtended.LogFormatted("Failed to read Version info from web");
        //        MonoBehaviourExtended.LogFormatted(ex.Message);

        //    }
        //    MonoBehaviourExtended.LogFormatted("Version Check result:" + VersionCheckResult);
        //    return blnReturn;
        //}

        #endregion
    }

    internal class RectStorage : ConfigNodeStorage
    {
        [Persistent] internal float x, y, width, height;

        internal Rect ToRect()
        {
            return new Rect(x, y, width, height);
        }

        internal RectStorage FromRect(Rect rectToStore)
        {
            x = rectToStore.x;
            y = rectToStore.y;
            width = rectToStore.width;
            height = rectToStore.height;
            return this;
        }
    }


    public class ResourceSettings : ConfigNodeStorage
    {
        [Persistent] internal bool AlarmEnabled = false;

        [Persistent] internal DisplayUnitsEnum DisplayValueAs = DisplayUnitsEnum.Units;
        [Persistent] internal bool HideWhenEmpty = false;
        [Persistent] internal bool HideWhenFull = false;

        [Persistent] internal int id;
        [Persistent] internal bool IsSeparator = false;
        [Persistent] internal int MonitorAlertLevel = 10;
        [Persistent] internal MonitorDirections MonitorDirection = MonitorDirections.Low;
        [Persistent] internal int MonitorWarningLevel = 20;
        [Persistent] internal string name = "";
        [Persistent] internal bool ShowReserveLevels = false;

        [Persistent] internal bool SplitLastStage = true;
        [Persistent] internal VisibilityTypes Visibility = VisibilityTypes.AlwaysOn;

        public ResourceSettings(int id, string name) : this()
        {
            this.id = id;
            this.name = name;
        }

        public ResourceSettings()
        {
        }

        internal enum VisibilityTypes
        {
            AlwaysOn,
            Threshold,
            Hidden
        }

        internal enum MonitorDirections
        {
            [Description("Monitor for High Values")]
            High,

            [Description("Monitor for Low Values")]
            Low
        }

        internal enum DisplayUnitsEnum
        {
            [Description("Default KSP Units")] Units,
            [Description("Display as Tonnes")] Tonnes,
            [Description("Display as Kilograms")] Kilograms,
            [Description("Display as Liters")] Liters
        }
    }

    internal static class ExtensionsARP
    {
        public static ResourceSettings.VisibilityTypes Next(this ResourceSettings.VisibilityTypes vt)
        {
            switch (vt)
            {
                case ResourceSettings.VisibilityTypes.AlwaysOn:
                    return ResourceSettings.VisibilityTypes.Threshold;
                case ResourceSettings.VisibilityTypes.Threshold:
                    return ResourceSettings.VisibilityTypes.Hidden;
                case ResourceSettings.VisibilityTypes.Hidden:
                    return ResourceSettings.VisibilityTypes.AlwaysOn;
                default:
                    return ResourceSettings.VisibilityTypes.AlwaysOn;
            }
        }
    }
}