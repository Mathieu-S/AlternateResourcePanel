﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

using KSP;
using UnityEngine;
using KSPPluginFramework;

namespace KSPAlternateResourcePanel
{
    internal class Settings : ConfigNodeStorage
    {
        internal Settings(String FilePath) : base(FilePath) {
            Version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString();
            //on each start set the attention flag to the property - should be on each program start
            VersionAttentionFlag = VersionAvailable;

            OnEncodeToConfigNode();
        }

        [Persistent] internal Boolean ToggleOn = false;
        [Persistent] internal Boolean LockLocation= true;
        internal Rect WindowPosition = new Rect(Screen.width - 298, 19, 299, 20);
        [Persistent] private RectStorage WindowPositionStored = new RectStorage();

        [Persistent] internal Boolean ShowRates = true;
        [Persistent] internal Boolean ShowRatesForParts = true;

        [Persistent] internal Boolean AlarmsEnabled = true;
        [Persistent] internal String AlarmsWarningSound = "_DefaultWarning";
        [Persistent] internal Int32 AlarmsWarningRepeats = 2;
        internal String AlarmsWarningRepeatsText { get { return RepeatsToString(AlarmsWarningRepeats); } }

        [Persistent] internal String AlarmsAlertSound = "_DefaultAlert";
        [Persistent] internal Int32 AlarmsAlertRepeats = 4;
        internal String AlarmsAlertRepeatsText { get { return RepeatsToString(AlarmsAlertRepeats); } }

        [Persistent] internal Boolean AlarmsVolumeFromUI=true;
        [Persistent] internal Single AlarmsVolume=0.25f;

        private String RepeatsToString(Int32 value)
        {
            if (value > 5)
                return "For Ever";
            else
                return value + " Time(s)";
        }


        [Persistent] internal Boolean StagingEnabled = false;
        [Persistent] internal Boolean StagingEnabledInMapView = false;
        [Persistent] internal Boolean StagingEnabledSpaceInMapView = false;

        [Persistent] internal List<String> lstIconOrder = new List<String>() { "KSPARP", "Mod", "Player" };
        [Persistent] internal DisplaySkin SelectedSkin = DisplaySkin.Default;

        internal Boolean BlizzyToolbarIsAvailable = false;
        [Persistent] internal Boolean UseBlizzyToolbarIfAvailable = false;

        //Version Stuff
        [Persistent] internal Boolean DailyVersionCheck = true;
        internal Boolean VersionAttentionFlag = false;
        //When did we last check??
        internal DateTime VersionCheckDate_Attempt =new DateTime();
        [Persistent] internal String VersionCheckDate_AttemptStored;
        public String VersionCheckDate_AttemptString { get { return ConvertVersionCheckDateToString(this.VersionCheckDate_Attempt); } }
        internal DateTime VersionCheckDate_Success = new DateTime();
        [Persistent] internal String VersionCheckDate_SuccessStored;
        public String VersionCheckDate_SuccessString { get { return ConvertVersionCheckDateToString(this.VersionCheckDate_Success); } }

        internal Dictionary<Int32, ResourceSettings> Resources = new Dictionary<int, ResourceSettings>();
        [Persistent] List<ResourceSettings> ResourcesStorage = new List<ResourceSettings>();
        
        [Persistent] internal Boolean ShowWindowOnResourceMonitor=true;

        public override void OnEncodeToConfigNode()
        {
            WindowPositionStored = WindowPositionStored.FromRect(WindowPosition);
            ResourcesStorage = Resources.Values.ToList<ResourceSettings>();
            VersionCheckDate_AttemptStored = VersionCheckDate_AttemptString;
            VersionCheckDate_SuccessStored = VersionCheckDate_SuccessString;
        }
        public override void OnDecodeFromConfigNode()
        {
 	        WindowPosition = WindowPositionStored.ToRect();
            Resources = ResourcesStorage.ToDictionary(x => x.id);
            DateTime.TryParseExact(VersionCheckDate_AttemptStored, "yyyy-MM-dd", null,System.Globalization.DateTimeStyles.None , out VersionCheckDate_Attempt);
            DateTime.TryParseExact(VersionCheckDate_SuccessStored, "yyyy-MM-dd", null ,System.Globalization.DateTimeStyles.None, out VersionCheckDate_Success);
        }


        internal enum DisplaySkin
        {
            [Description("KSP Style")]          Default,
            [Description("Unity Style")]        Unity,
            [Description("Unity/KSP Buttons")]  UnityWKSPButtons
        }

        #region Version Checks
        private String ConvertVersionCheckDateToString(DateTime Date)
        {
            if (Date < DateTime.Now.AddYears(-10))
                return "No Date Recorded";
            else
                return String.Format("{0:yyyy-MM-dd}", Date);
        }

        public String Version = "";
        [Persistent] public String VersionWeb = "";
        public Boolean VersionAvailable
        {
            get
            {
                //todo take this out
                if (this.VersionWeb == "")
                    return false;
                else
                    try
                    {
                        //if there was a string and its version is greater than the current running one then alert
                        System.Version vTest = new System.Version(this.VersionWeb);
                        return (System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.CompareTo(vTest) < 0);
                    }
                    catch (Exception ex)
                    {
                        MonoBehaviourExtended.LogFormatted("webversion: '{0}'", this.VersionWeb);
                        MonoBehaviourExtended.LogFormatted("Unable to compare versions: {0}", ex.Message);
                        return false;
                    }

                //return ((this.VersionWeb != "") && (this.Version != this.VersionWeb));
            }
        }

        public String VersionCheckResult = "";

        public Boolean getLatestVersion()
        {
            Boolean blnReturn = false;
            try
            {
                //Get the file from Codeplex
                this.VersionCheckResult = "Unknown - check again later";
                this.VersionCheckDate_Attempt = DateTime.Now;

                MonoBehaviourExtended.LogFormatted("Reading version from Web");
                //Page content FormatException is |LATESTVERSION|1.2.0.0|LATESTVERSION|
                WWW www = new WWW("http://kspalternateresourcepanel.codeplex.com/wikipage?title=LatestVersion");
                while (!www.isDone) { }

                //Parse it for the version String
                String strFile = www.text;
                MonoBehaviourExtended.LogFormatted("Response Length:" + strFile.Length);

                Match matchVersion;
                matchVersion = Regex.Match(strFile, "(?<=\\|LATESTVERSION\\|).+(?=\\|LATESTVERSION\\|)", System.Text.RegularExpressions.RegexOptions.Singleline);
                MonoBehaviourExtended.LogFormatted("Got Version '" + matchVersion.ToString() + "'");

                String strVersionWeb = matchVersion.ToString();
                if (strVersionWeb != "")
                {
                    this.VersionCheckResult = "Success";
                    this.VersionCheckDate_Success = DateTime.Now;
                    this.VersionWeb = strVersionWeb;
                    blnReturn = true;
                }
                else
                {
                    this.VersionCheckResult = "Unable to parse web service";
                }
            }
            catch (Exception ex)
            {
                MonoBehaviourExtended.LogFormatted("Failed to read Version info from web");
                MonoBehaviourExtended.LogFormatted(ex.Message);

            }
            MonoBehaviourExtended.LogFormatted("Version Check result:" + VersionCheckResult);
            return blnReturn;
        }

        /// <summary>
        /// Does some logic to see if a check is needed, and returns true if there is a different version
        /// </summary>
        /// <param name="ForceCheck">Ignore all logic and simply do a check</param>
        /// <returns></returns>
        public Boolean VersionCheck(Boolean ForceCheck)
        {
            Boolean blnReturn = false;
            Boolean blnDoCheck = false;

            try
            {
                if (ForceCheck)
                {
                    blnDoCheck = true;
                    MonoBehaviourExtended.LogFormatted("Starting Version Check-Forced");
                }
                else if (this.VersionWeb == "")
                {
                    blnDoCheck = true;
                    MonoBehaviourExtended.LogFormatted("Starting Version Check-No current web version stored");
                }
                else if (this.VersionCheckDate_Success < DateTime.Now.AddYears(-9))
                {
                    blnDoCheck = true;
                    MonoBehaviourExtended.LogFormatted("Starting Version Check-No current date stored");
                }
                else if (this.VersionCheckDate_Success.Date != DateTime.Now.Date)
                {
                    blnDoCheck = true;
                    MonoBehaviourExtended.LogFormatted("Starting Version Check-stored date is not today");
                }
                else
                    MonoBehaviourExtended.LogFormatted("Skipping version check");


                if (blnDoCheck)
                {
                    getLatestVersion();
                    this.Save();
                    //if (getLatestVersion())
                    //{
                    //    //save all the details to the file
                    //    this.Save();
                    //}
                    //if theres a new version then set the flag
                    VersionAttentionFlag = VersionAvailable;
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
        #endregion
    }

    internal class RectStorage:ConfigNodeStorage
    {
        [Persistent] internal Single x,y,width,height;

        internal Rect ToRect() { return new Rect(x, y, width, height); }
        internal RectStorage FromRect(Rect rectToStore)
        {
            this.x = rectToStore.x;
            this.y = rectToStore.y;
            this.width = rectToStore.width;
            this.height = rectToStore.height;
            return this;
        }
    }


    internal class ResourceSettings:ConfigNodeStorage
    {
        [Persistent] internal Int32 id;
        [Persistent] internal String name="";
        [Persistent] internal Boolean IsSeparator=false;        
        [Persistent] internal VisibilityTypes Visibility = VisibilityTypes.AlwaysOn;
        [Persistent] internal Boolean AlarmEnabled = false;
        [Persistent] internal MonitorDirections MonitorDirection = MonitorDirections.Low;
        [Persistent] internal Int32 MonitorWarningLevel = 20;
        [Persistent] internal Int32 MonitorAlertLevel = 10;

        internal enum VisibilityTypes
        {
            AlwaysOn,
            Threshold,
            Hidden
        }
        internal enum MonitorDirections
        {
            [Description("Monitor for High Values")]    High,
            [Description("Monitor for Low Values")]     Low
        }

    }

    static class ExtensionsARP
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
