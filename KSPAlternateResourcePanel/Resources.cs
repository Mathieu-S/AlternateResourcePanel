using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using KSPPluginFramework;

namespace KSPAlternateResourcePanel
{
    internal class Resources
    {
        //WHERE SHOULD THESE BE???
        internal static string PathApp = KSPUtil.ApplicationRootPath.Replace("\\", "/");

        internal static string PathTriggerTech = string.Format("{0}GameData/TriggerTech", PathApp);

        //internal static String PathPlugin = string.Format("{0}/{1}", PathTriggerTech, KSPAlternateResourcePanel._AssemblyName);
        internal static string PathPlugin =
            Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location).Replace("\\", "/");

        internal static string PathPluginToolbarIcons = string.Format("{0}/ToolbarIcons", PathPlugin);

        internal static string PathPluginTextures = string.Format("{0}/PluginData/Textures", PathPlugin);

        //internal static String PathPluginData = string.Format("{0}/Data", PathPlugin);
        internal static string PathPluginSounds = string.Format("{0}/Sounds", PathPlugin);

        internal static string DBPathTriggerTech = "TriggerTech";
        internal static string DBPathPlugin = string.Format("TriggerTech/{0}", MonoBehaviourExtended._AssemblyName);

        internal static string DBPathToolbarIcons = string.Format("{0}/ToolbarIcons", DBPathPlugin);

        //internal static String DBPathTextures = string.Format("{0}/Textures", DBPathPlugin);
        internal static string DBPathPluginSounds = string.Format("{0}/Sounds", DBPathPlugin);


        internal static Texture2D texPanel = new Texture2D(16, 16, TextureFormat.ARGB32, false);
        internal static Texture2D texBarBlue = new Texture2D(13, 13, TextureFormat.ARGB32, false);
        internal static Texture2D texBarBlue_Back = new Texture2D(13, 13, TextureFormat.ARGB32, false);
        internal static Texture2D texBarGreen = new Texture2D(13, 13, TextureFormat.ARGB32, false);
        internal static Texture2D texBarGreen_Back = new Texture2D(13, 13, TextureFormat.ARGB32, false);

        internal static Texture2D texBarHighlight = new Texture2D(9, 9, TextureFormat.ARGB32, false);
        internal static Texture2D texBarHighlightGreen = new Texture2D(9, 9, TextureFormat.ARGB32, false);
        internal static Texture2D texBarHighlightRed = new Texture2D(9, 9, TextureFormat.ARGB32, false);

        internal static Texture2D btnChevronUp = new Texture2D(17, 16, TextureFormat.ARGB32, false);
        internal static Texture2D btnChevronDown = new Texture2D(17, 16, TextureFormat.ARGB32, false);

        internal static Texture2D btnViewAll = new Texture2D(16, 16, TextureFormat.ARGB32, false);

        internal static Texture2D btnViewTimes = new Texture2D(16, 16, TextureFormat.ARGB32, false);

        //internal static Texture2D btnViewBase = new Texture2D(16, 16, TextureFormat.ARGB32, false);
        internal static Texture2D btnViewBaseActive = new Texture2D(16, 16, TextureFormat.ARGB32, false);

        internal static Texture2D btnSettingsAttention = new Texture2D(17, 16, TextureFormat.ARGB32, false);

        internal static Texture2D texPartWindowHead = new Texture2D(16, 16, TextureFormat.ARGB32, false);

        //internal static Texture2D texTooltipBackground; // = new Texture2D(9, 9);//, TextureFormat.ARGB32, false);

        internal static Texture2D texRateUp = new Texture2D(10, 10, TextureFormat.ARGB32, false);
        internal static Texture2D texRateDown = new Texture2D(10, 10, TextureFormat.ARGB32, false);

        internal static Texture2D btnAlarm = new Texture2D(16, 16, TextureFormat.ARGB32, false);
        internal static Texture2D btnAlarmEnabled = new Texture2D(16, 16, TextureFormat.ARGB32, false);
        internal static Texture2D btnAlarmWarn = new Texture2D(16, 16, TextureFormat.ARGB32, false);
        internal static Texture2D btnAlarmAlert = new Texture2D(16, 16, TextureFormat.ARGB32, false);

        //internal static Texture2D btnLock;
        //internal static Texture2D btnUnlock;

        internal static Texture2D btnDropDown = new Texture2D(10, 10, TextureFormat.ARGB32, false);
        internal static Texture2D btnPlay = new Texture2D(10, 10, TextureFormat.ARGB32, false);
        internal static Texture2D btnStop = new Texture2D(10, 10, TextureFormat.ARGB32, false);

        internal static Texture2D btnPlayGreen = new Texture2D(10, 9, TextureFormat.ARGB32, false);
        internal static Texture2D btnBan = new Texture2D(10, 9, TextureFormat.ARGB32, false);

        internal static GUIContent guiFlowEnabled, guiFlowDisabled;

        internal static Texture2D texResourceMove = new Texture2D(378, 9, TextureFormat.ARGB32, false);

        internal static Texture2D texBox = new Texture2D(9, 9, TextureFormat.ARGB32, false);
        internal static Texture2D texBoxUnity = new Texture2D(9, 9, TextureFormat.ARGB32, false);

        internal static Texture2D texSeparatorV = new Texture2D(6, 2, TextureFormat.ARGB32, false);
        internal static Texture2D texSeparatorH = new Texture2D(2, 20, TextureFormat.ARGB32, false);

        internal static Texture2D texAppLaunchIcon = new Texture2D(38, 38, TextureFormat.ARGB32, false);

        //Icon Libraries
        internal static Dictionary<string, Texture2D> texIconsKSPARP;
        internal static Dictionary<string, Texture2D> texIconsPlayer;
        internal static Dictionary<string, Texture2D> texIconsResourceDefs;

        //Alarm Library
        internal static Dictionary<string, AudioClip> clipAlarms;

        internal static Dictionary<string, Texture2D> dictFirst = texIconsKSPARP;
        internal static Dictionary<string, Texture2D> dictSecond = texIconsResourceDefs;
        internal static Dictionary<string, Texture2D> dictThird = texIconsPlayer;

        internal static void LoadSounds()
        {
            MonoBehaviourExtended.LogFormatted("Loading Sounds");

            clipAlarms = new Dictionary<string, AudioClip>();
            clipAlarms.Add("None", null);
            if (Directory.Exists(PathPluginSounds))
            {
                //get all the png and tga's
                var fileClips = new DirectoryInfo(PathPluginSounds).GetFiles("*.wav");

                foreach (var fileClip in fileClips)
                    try
                    {
                        //load the file from the GameDB
                        AudioClip clipLoading = null;
                        if (LoadAudioClipFromGameDB(ref clipLoading, fileClip.Name))
                        {
                            var ClipKey = fileClip.Name;
                            if (ClipKey.ToLower().EndsWith(".wav"))
                                ClipKey = ClipKey.Substring(0, ClipKey.Length - 4);
                            clipAlarms.Add(ClipKey, clipLoading);
                        }
                    }
                    catch (Exception)
                    {
                        //MonoBehaviourExtended.LogFormatted("Unable to load AudioClip from GameDB:{0}/{1}", PathPluginSounds,fileClip.Name);
                    }
            }
        }

        internal static void LoadTextures()
        {
            MonoBehaviourExtended.LogFormatted("Loading Textures");

            texIconsKSPARP = LoadIconDictionary("Icons");
            MonoBehaviourExtended.LogFormatted("KSPARP Icons Loaded: {0}", texIconsKSPARP.Count.ToString());
            texIconsPlayer = LoadIconDictionary("Icons-Player");
            MonoBehaviourExtended.LogFormatted("Player Icons Loaded: {0}", texIconsPlayer.Count.ToString());
            texIconsResourceDefs = LoadIconDictionary_Defs();
            MonoBehaviourExtended.LogFormatted("Mod Definition Icons Loaded: {0}",
                texIconsResourceDefs.Count.ToString());

            //Set the ordering arrays
            dictFirst = texIconsKSPARP;
            dictSecond = texIconsResourceDefs;
            dictThird = texIconsPlayer;

            LoadImageFromFile(ref texPanel, "img_PanelBack.png");

            LoadImageFromFile(ref texBarBlue, "img_BarBlue.png");
            LoadImageFromFile(ref texBarBlue_Back, "img_BarBlue_Back.png");
            LoadImageFromFile(ref texBarGreen, "img_BarGreen.png");
            LoadImageFromFile(ref texBarGreen_Back, "img_BarGreen_Back.png");

            LoadImageFromFile(ref texBarHighlight, "img_BarHighlight.png");
            LoadImageFromFile(ref texBarHighlightGreen, "img_BarHighlightGreen.png");
            LoadImageFromFile(ref texBarHighlightRed, "img_BarHighlightRed.png");

            LoadImageFromFile(ref btnChevronUp, "img_buttonChevronUp.png");
            LoadImageFromFile(ref btnChevronDown, "img_buttonChevronDown.png");

            LoadImageFromFile(ref btnViewAll, "img_buttonEye.png");
            LoadImageFromFile(ref btnViewTimes, "img_buttonClock.png");
            //LoadImageFromFile(ref btnViewBase, "img_buttonBase.png");
            LoadImageFromFile(ref btnViewBaseActive, "img_buttonBaseActive.png");

            LoadImageFromFile(ref btnSettingsAttention, "img_buttonSettingsAttention.png");

            LoadImageFromFile(ref texPartWindowHead, "img_PartWindowHead.png");

            //LoadImageFromFile(ref texTooltipBackground, "tex_TooltipBackground.png");

            LoadImageFromFile(ref texRateUp, "img_RateUp.png");
            LoadImageFromFile(ref texRateDown, "img_RateDown.png");

            LoadImageFromFile(ref btnAlarm, "img_Alarm.png");
            LoadImageFromFile(ref btnAlarmEnabled, "img_AlarmEnabled.png");
            LoadImageFromFile(ref btnAlarmWarn, "img_AlarmWarn.png");
            LoadImageFromFile(ref btnAlarmAlert, "img_AlarmAlert.png");

            //LoadImageFromFile(ref btnLock, "img_Lock.png");
            //LoadImageFromFile(ref btnUnlock, "img_Unlock.png");

            LoadImageFromFile(ref btnDropDown, "img_DropDown.png");
            LoadImageFromFile(ref btnPlay, "img_Play.png");
            LoadImageFromFile(ref btnStop, "img_Stop.png");

            LoadImageFromFile(ref btnPlayGreen, "img_PlayGreen.png");
            LoadImageFromFile(ref btnBan, "img_BanRed.png");

            guiFlowEnabled = new GUIContent(btnPlayGreen, "Click to disable");
            guiFlowDisabled = new GUIContent(btnBan, "Click to enable");
            //LoadImageFromFile(ref btnDropDownSep, "img_DropDownSep.png");

            //LoadImageFromFile(ref texDropDownListBox, "tex_DropDownListBox.png");
            //LoadImageFromFile(ref texDropDownListBoxUnity, "tex_DropDownListBoxUnity.png");

            LoadImageFromFile(ref texResourceMove, "img_ResourceMove.png");

            LoadImageFromFile(ref texBox, "tex_Box.png");
            LoadImageFromFile(ref texBoxUnity, "tex_BoxUnity.png");

            LoadImageFromFile(ref texSeparatorH, "img_SeparatorHorizontal.png");
            LoadImageFromFile(ref texSeparatorV, "img_SeparatorVertical.png");

            LoadImageFromFile(ref texAppLaunchIcon, "KSPARPaBig.png", PathPluginToolbarIcons);
        }


        private static Dictionary<string, Texture2D> LoadIconDictionary(string IconFolderName)
        {
            Dictionary<string, Texture2D> dictReturn = new Dictionary<string, Texture2D>();
            Texture2D texLoading;

            //Where are the Icons
            var strIconPath = string.Format("{0}/{1}", PathPlugin, IconFolderName);
            //String strIconDBPath = string.Format("{0}/{1}", DBPathPlugin, IconFolderName);

            if (Directory.Exists(strIconPath))
            {
                //get all the png and tga's
                var fileIconsPNG = new DirectoryInfo(strIconPath).GetFiles("*.png");
                var fileIconsTGA = new DirectoryInfo(strIconPath).GetFiles("*.tga");
                var fileIcons = fileIconsPNG.Concat(fileIconsTGA).ToArray();

                foreach (var fileIcon in fileIcons)
                    try
                    {
                        //load the file from the GameDB
                        texLoading = new Texture2D(32, 16, TextureFormat.ARGB32, false);
                        if (LoadImageFromFile(ref texLoading, fileIcon.Name, strIconPath))
                            dictReturn.Add(fileIcon.Name.ToLower().Replace(".png", "").Replace(".tga", ""), texLoading);
                    }
                    catch (Exception)
                    {
                        MonoBehaviourExtended.LogFormatted("Unable to load Texture from GameDB:{0}", strIconPath);
                    }
            }

            return dictReturn;
        }

        /// <summary>
        ///     This one gets all the icons named in the resource definitions
        /// </summary>
        /// <returns></returns>
        private static Dictionary<string, Texture2D> LoadIconDictionary_Defs()
        {
            Dictionary<string, Texture2D> dictReturn = new Dictionary<string, Texture2D>();
            Texture2D texLoading;

            //Find All the RESOURCE_DEFINITION Nodes
            ConfigNode[] cns = GameDatabase.Instance.GetConfigNodes("RESOURCE_DEFINITION");
            foreach (ConfigNode cn in cns)
                if (cn.HasValue("name"))
                    if (cn.HasValue("ksparpicon"))
                        //If it has a name and a ksparpicon
                        try
                        {
                            //lead the Texture from the GameDB
                            texLoading = GameDatabase.Instance.GetTexture(cn.GetValue("ksparpicon"), false);
                            if (texLoading.width > 32 || texLoading.height > 16)
                                MonoBehaviourExtended.LogFormatted("Texture Too Big (32x16 is limit) - w:{0} h:{1}",
                                    texLoading.width, texLoading.height);
                            else
                                dictReturn.Add(cn.GetValue("name").ToLower(), texLoading);
                        }
                        catch (Exception)
                        {
                            MonoBehaviourExtended.LogFormatted("Unable to load texture {0}-{1}", cn.GetValue("name"),
                                cn.GetValue("ksparpicon"));
                        }

            return dictReturn;
        }

        internal static void SetIconOrder(Settings settings)
        {
            dictFirst = GetIconDict(settings.lstIconOrder[0]);
            dictSecond = GetIconDict(settings.lstIconOrder[1]);
            dictThird = GetIconDict(settings.lstIconOrder[2]);
        }

        internal static Dictionary<string, Texture2D> GetIconDict(string Name)
        {
            switch (Name.ToLower())
            {
                case "ksparp": return texIconsKSPARP;
                case "mod": return texIconsResourceDefs;
                case "player": return texIconsPlayer;
                default:
                    return null;
            }
        }

        internal static GUIContent IconOrderContent(string Name)
        {
            switch (Name.ToLower())
            {
                case "ksparp": return new GUIContent("ARP", "Alternate Resource Panel");
                case "mod": return new GUIContent("Mod", "Mod Resource Definition");
                case "player": return new GUIContent("Player", "Players Icons");
                default:
                    return new GUIContent("ERROR", "");
            }
        }

        #region Util Stuff

        //internal static Boolean LoadImageFromGameDB(ref Texture2D tex, String FileName, String FolderPath = "")
        //{
        //    Boolean blnReturn = false;
        //    try
        //    {
        //        //trim off the tga and png extensions
        //        if (FileName.ToLower().EndsWith(".png")) FileName = FileName.Substring(0, FileName.Length - 4);
        //        if (FileName.ToLower().EndsWith(".tga")) FileName = FileName.Substring(0, FileName.Length - 4); 
        //        //default folder
        //        if (FolderPath == "") FolderPath = DBPathTextures;

        //        //Look for case mismatches
        //        if (!GameDatabase.Instance.ExistsTexture(String.Format("{0}/{1}", FolderPath, FileName)))
        //            throw new Exception();

        //        //now load it
        //        tex = GameDatabase.Instance.GetTexture(String.Format("{0}/{1}", FolderPath, FileName), false);
        //        blnReturn = true;
        //    }
        //    catch (Exception)
        //    {
        //        MonoBehaviourExtended.LogFormatted("Failed to load (are you missing a file - and check case):{0}/{1}", FolderPath, FileName);
        //    }
        //    return blnReturn;
        //}

        internal static bool LoadAudioClipFromGameDB(ref AudioClip clip, string FileName, string FolderPath = "")
        {
            var blnReturn = false;
            try
            {
                //trim off the tga and png extensions
                if (FileName.ToLower().EndsWith(".wav")) FileName = FileName.Substring(0, FileName.Length - 4);
                //default folder
                if (FolderPath == "") FolderPath = DBPathPluginSounds;

                //Look for case mismatches
                if (!GameDatabase.Instance.ExistsAudioClip(string.Format("{0}/{1}", FolderPath, FileName)))
                    throw new Exception();

                //now load it
                clip = GameDatabase.Instance.GetAudioClip(string.Format("{0}/{1}", FolderPath, FileName));
                blnReturn = true;
            }
            catch (Exception)
            {
                MonoBehaviourExtended.LogFormatted("Failed to load (are you missing a file - and check case):{0}/{1}",
                    FolderPath, FileName);
            }

            return blnReturn;
        }

        /// <summary>
        ///     Loads a texture from the file system directly
        /// </summary>
        /// <param name="tex">Unity Texture to Load</param>
        /// <param name="FileName">Image file name</param>
        /// <param name="FolderPath">Optional folder path of image</param>
        /// <returns></returns>
        public static bool LoadImageFromFile(ref Texture2D tex, string FileName, string FolderPath = "")
        {
            //DebugLogFormatted("{0},{1}",FileName, FolderPath);
            var blnReturn = false;
            try
            {
                if (FolderPath == "") FolderPath = PathPluginTextures;

                //File Exists check
                if (File.Exists(string.Format("{0}/{1}", FolderPath, FileName)))
                    try
                    {
                        //MonoBehaviourExtended.LogFormatted_DebugOnly("Loading: {0}", String.Format("{0}/{1}", FolderPath, FileName));
                        tex.LoadImage(File.ReadAllBytes(string.Format("{0}/{1}", FolderPath, FileName)));
                        blnReturn = true;
                    }
                    catch (Exception ex)
                    {
                        MonoBehaviourExtended.LogFormatted("Failed to load the texture:{0} ({1})",
                            string.Format("{0}/{1}", FolderPath, FileName), ex.Message);
                    }
                else
                    MonoBehaviourExtended.LogFormatted("Cannot find texture to load:{0}",
                        string.Format("{0}/{1}", FolderPath, FileName));
            }
            catch (Exception ex)
            {
                MonoBehaviourExtended.LogFormatted("Failed to load (are you missing a file):{0} ({1})",
                    string.Format("{0}/{1}", FolderPath, FileName), ex.Message);
            }

            return blnReturn;
        }

        #endregion
    }
}