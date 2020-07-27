using KSPPluginFramework;

namespace KSPAlternateResourcePanel
{
    internal class AudioController : MonoBehaviourExtended
    {
        private AudioSource audiosourceAlarm;

        //Parent Objects
        internal KSPAlternateResourcePanel mbARP;

        private bool Playing;
        private int RepeatCounter;
        private int RepeatLimit;
        private Settings settings;

        internal float Volume
        {
            get
            {
                if (KSPAlternateResourcePanel.settings.AlarmsVolumeFromUI)
                    return GameSettings.UI_VOLUME;
                return KSPAlternateResourcePanel.settings.AlarmsVolume;
            }
        }

        internal int VolumePct => (int) (Volume * 100);

        internal void Init()
        {
            settings = KSPAlternateResourcePanel.settings;

            if (Resources.clipAlarms.ContainsKey(settings.AlarmsAlertSound))
                mbARP.clipAlarmsAlert = Resources.clipAlarms[settings.AlarmsAlertSound];
            if (Resources.clipAlarms.ContainsKey(settings.AlarmsWarningSound))
                mbARP.clipAlarmsWarning = Resources.clipAlarms[settings.AlarmsWarningSound];

            audiosourceAlarm = mbARP.gameObject.AddComponent<AudioSource>();
            audiosourceAlarm.spatialBlend = 0;
            audiosourceAlarm.playOnAwake = false;
            audiosourceAlarm.loop = false;
            audiosourceAlarm.Stop();
        }

        internal void Play(AudioClip clipToPlay)
        {
            Play(clipToPlay, 1);
        }

        internal void Play(AudioClip clipToPlay, int Repeats)
        {
            audiosourceAlarm.clip = clipToPlay;
            audiosourceAlarm.loop = false;
            audiosourceAlarm.volume = Volume;

            RepeatCounter = 0;
            RepeatLimit = Repeats;
            Playing = true;

            audiosourceAlarm.Play();

            if (onPlayStarted != null)
                onPlayStarted(this, clipToPlay);
        }

        internal void Stop()
        {
            audiosourceAlarm.Stop();
            Playing = false;
            if (onPlayFinished != null)
                onPlayFinished(this, audiosourceAlarm.clip);
        }


        //internal Boolean isClipPlaying() { return audiosourceAlarm.isPlaying; }
        internal bool isClipPlaying(AudioClip clip)
        {
            return Playing && clip == audiosourceAlarm.clip;
        }

        internal bool isPlaying()
        {
            return Playing;
        }

        //check status of playing and do whats next;
        internal override void Update()
        {
            //if the audioclip is done
            if (!audiosourceAlarm.isPlaying && Playing)
            {
                //increase the repeat counter
                RepeatCounter++;
                if (RepeatCounter < RepeatLimit || RepeatLimit > 5)
                {
                    //play it again
                    audiosourceAlarm.Play();
                }
                else
                {
                    //halt playing
                    Playing = false;
                    if (onPlayFinished != null)
                        onPlayFinished(this, audiosourceAlarm.clip);
                }
            }
        }

        internal event AudioEventArgs onPlayFinished;
        internal event AudioEventArgs onPlayStarted;

        internal delegate void AudioEventArgs(AudioController sender, AudioClip clip);
    }
}