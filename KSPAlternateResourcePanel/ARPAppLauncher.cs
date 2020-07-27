using System;
using System.Collections;

namespace KSPAlternateResourcePanel
{
    public partial class KSPAlternateResourcePanel
    {
        internal bool AppLauncherToBeSetTrue;
        internal DateTime AppLauncherToBeSetTrueAttemptDate;
        internal ApplicationLauncherButton btnAppLauncher = null;

        private bool MouseOverAppLauncherBtn;
        internal bool SceneChangeRequiredToRestoreResourcesApp = false;

        internal bool StockAppToBeHidden;
        internal DateTime StockAppToBeHiddenAttemptDate;

        /// <summary>
        ///     Sets up the App Button - no longer called by the event as that only happens on StartMenu->SpaceCenter now
        /// </summary>
        private void OnGUIAppLauncherReady()
        {
            LogFormatted_DebugOnly("AppLauncherReady");
            if (settings.ButtonStyleChosen == ARPWindowSettings.ButtonStyleEnum.Launcher ||
                settings.ButtonStyleChosen == ARPWindowSettings.ButtonStyleEnum.StockReplace)
            {
                btnAppLauncher = InitAppLauncherButton();

                if (settings.ButtonStyleChosen == ARPWindowSettings.ButtonStyleEnum.StockReplace)
                {
                    //StartCoroutine(ReplaceStockAppButton());
                }
            }
        }

        private void OnGUIAppLauncherUnreadifying(GameScenes SceneToLoad)
        {
            LogFormatted_DebugOnly("Unreadifying the Launcher");
            DestroyAppLauncherButton();
        }

        internal ApplicationLauncherButton InitAppLauncherButton()
        {
            ApplicationLauncherButton retButton = null;

            ApplicationLauncherButton[] lstButtons =
                KSPAlternateResourcePanel.FindObjectsOfType<ApplicationLauncherButton>();
            LogFormatted("AppLauncher: Creating Button-BEFORE", lstButtons.Length);
            try
            {
                retButton = ApplicationLauncher.Instance.AddModApplication(
                    onAppLaunchToggleOn, onAppLaunchToggleOff,
                    onAppLaunchHoverOn, onAppLaunchHoverOff,
                    null, null,
                    ApplicationLauncher.AppScenes.FLIGHT | ApplicationLauncher.AppScenes.MAPVIEW,
                    (Texture) Resources.texAppLaunchIcon);

                AppLauncherButtonMutuallyExclusive(settings.AppLauncherMutuallyExclusive);

                //appButton = ApplicationLauncher.Instance.AddApplication(
                //    onAppLaunchToggleOn, onAppLaunchToggleOff,
                //    onAppLaunchHoverOn, onAppLaunchHoverOff,
                //    null, null,
                //    (Texture)Resources.texAppLaunchIcon);
                //appButton.VisibleInScenes = ApplicationLauncher.AppScenes.FLIGHT;
            }
            catch (Exception ex)
            {
                LogFormatted("AppLauncher: Failed to set up App Launcher Button\r\n{0}", ex.Message);
                retButton = null;
            }

            lstButtons = KSPAlternateResourcePanel.FindObjectsOfType<ApplicationLauncherButton>();
            LogFormatted("AppLauncher: Creating Button-AFTER", lstButtons.Length);

            return retButton;
        }

        internal void AppLauncherButtonMutuallyExclusive(bool Enable)
        {
            if (btnAppLauncher == null) return;
            if (Enable)
            {
                LogFormatted("AppLauncher: Setting Mutually Exclusive");
                ApplicationLauncher.Instance.EnableMutuallyExclusive(btnAppLauncher);
            }
            else
            {
                LogFormatted("AppLauncher: Clearing Mutually Exclusive");
                ApplicationLauncher.Instance.DisableMutuallyExclusive(btnAppLauncher);
            }
        }

        //internal ApplicationLauncherButton btnAppLauncher2 = null;

        //internal ApplicationLauncherButton InitAppLauncherButton2()
        //{
        //    ApplicationLauncherButton retButton = null;

        //    try
        //    {
        //        retButton = ApplicationLauncher.Instance.AddApplication(
        //            onAppLaunchToggleOn, onAppLaunchToggleOff,
        //            onAppLaunchHoverOn, onAppLaunchHoverOff,
        //            null, null,
        //            (Texture)Resources.texAppLaunchIcon);
        //        retButton.VisibleInScenes = ApplicationLauncher.AppScenes.FLIGHT | ApplicationLauncher.AppScenes.MAPVIEW;


        //        //ApplicationLauncher.Instance.EnableMutuallyExclusive(retButton);

        //        //appButton = ApplicationLauncher.Instance.AddApplication(
        //        //    onAppLaunchToggleOn, onAppLaunchToggleOff,
        //        //    onAppLaunchHoverOn, onAppLaunchHoverOff,
        //        //    null, null,
        //        //    (Texture)Resources.texAppLaunchIcon);
        //        //appButton.VisibleInScenes = ApplicationLauncher.AppScenes.FLIGHT;


        //        if (KSPAlternateResourcePanel.settings.ToggleOn)
        //            retButton.toggleButton.SetTrue();
        //    }
        //    catch (Exception ex)
        //    {
        //        MonoBehaviourExtended.LogFormatted("Failed to set up App Launcher Button\r\n{0}", ex.Message);
        //        retButton = null;
        //    }
        //    return retButton;
        //}

        internal void DestroyAppLauncherButton()
        {
            //LogFormatted_DebugOnly("Destroying AppLauncher Button. Count:{0}", lstButtons.Length);
            LogFormatted("AppLauncher: Destroying Button-BEFORE NULL CHECK");
            if (btnAppLauncher != null)
            {
                ApplicationLauncherButton[] lstButtons =
                    KSPAlternateResourcePanel.FindObjectsOfType<ApplicationLauncherButton>();
                LogFormatted("AppLauncher: Destroying Button-Button Count:{0}", lstButtons.Length);
                ApplicationLauncher.Instance.RemoveModApplication(btnAppLauncher);
                btnAppLauncher = null;
            }

            LogFormatted("AppLauncher: Destroying Button-AFTER NULL CHECK");
        }

        internal IEnumerator ReplaceStockAppButton()
        {
            while (ResourceDisplay.Instance == null || ResourceDisplay.Instance.appLauncherButton == null ||
                   !ApplicationLauncher.Ready)
                yield return null;

            if (ResourceDisplay.Instance.appLauncherButton == null)
            {
                if (!StockAppToBeHidden)
                    StockAppToBeHiddenAttemptDate = DateTime.Now;
                StockAppToBeHidden = true;

                if (StockAppToBeHiddenAttemptDate.AddSeconds(settings.ReplaceStockAppTimeOut) < DateTime.Now)
                {
                    StockAppToBeHidden = false;
                    LogFormatted(
                        "AppLauncher: Unable to Swap the ARP App for the Stock Resource App - tried for {0} secs",
                        settings.ReplaceStockAppTimeOut);
                }
            }
            else
            {
                LogFormatted("AppLauncher: Swapping the ARP App for the Stock Resource App - after {0} secs",
                    (DateTime.Now - StockAppToBeHiddenAttemptDate).TotalSeconds);
                StockAppToBeHidden = false;
                ResourceDisplay.Instance.appLauncherButton.onDisable();

                ResourceDisplay.Instance.appLauncherButton.onHover = btnAppLauncher.onHover;
                ResourceDisplay.Instance.appLauncherButton.onHoverOut = btnAppLauncher.onHoverOut;
                ResourceDisplay.Instance.appLauncherButton.onTrue = btnAppLauncher.onTrue;
                ResourceDisplay.Instance.appLauncherButton.onFalse = btnAppLauncher.onFalse;
                ResourceDisplay.Instance.appLauncherButton.onEnable = btnAppLauncher.onEnable;
                ResourceDisplay.Instance.appLauncherButton.onDisable = btnAppLauncher.onDisable;
                ResourceDisplay.Instance.appLauncherButton.SetTexture(Resources.texAppLaunchIcon);

                try
                {
                    ApplicationLauncher.Instance.RemoveModApplication(btnAppLauncher);
                    btnAppLauncher = null;
                }
                catch (Exception ex)
                {
                    LogFormatted(
                        "AppLauncher: Error killing ARP button after replacing the Stock Resource App - {0}\r\n{1}",
                        ex.Message, ex.StackTrace);
                }

                windowMain.DragEnabled = false;
                windowMain.WindowRect = new Rect(windowMainResetPos);
            }
        }

        internal void SetAppButtonToTrue()
        {
            if (!ApplicationLauncher.Ready)
            {
                LogFormatted_DebugOnly("not ready yet");
                AppLauncherToBeSetTrueAttemptDate = DateTime.Now;
                return;
            }

            ApplicationLauncherButton ButtonToToggle = btnAppLauncher;
            if (settings.ButtonStyleToDisplay == ARPWindowSettings.ButtonStyleEnum.StockReplace)
                ButtonToToggle = ResourceDisplay.Instance.appLauncherButton;

            if (ButtonToToggle == null)
            {
                LogFormatted_DebugOnly("Button Is Null");
                AppLauncherToBeSetTrueAttemptDate = DateTime.Now;
                return;
            }


            if (ButtonToToggle.toggleButton.CurrentState != KSP.UI.UIRadioButton.State.True)
            {
                if (AppLauncherToBeSetTrueAttemptDate.AddSeconds(settings.AppLauncherSetTrueTimeOut) < DateTime.Now)
                {
                    AppLauncherToBeSetTrue = false;
                    LogFormatted("AppLauncher: Unable to set the AppButton to true - tried for {0} secs",
                        settings.AppLauncherSetTrueTimeOut);
                }
                else
                {
                    LogFormatted("Setting App Button True");
                    ButtonToToggle.SetTrue(true);
                }
            }
            else
            {
                AppLauncherToBeSetTrue = false;
            }
        }

        private void onAppLaunchToggleOn()
        {
            LogFormatted_DebugOnly("TOn");
            settings.ToggleOn = true;
            settings.Save();
        }

        private void onAppLaunchToggleOff()
        {
            LogFormatted_DebugOnly("TOff");
            settings.ToggleOn = false;
            settings.Save();
        }

        private void onAppLaunchHoverOn()
        {
            LogFormatted_DebugOnly("HovOn");
            MouseOverAppLauncherBtn = true;
        }

        private void onAppLaunchHoverOff()
        {
            LogFormatted_DebugOnly("HovOff");
            MouseOverAppLauncherBtn = false;
        }
    }
}