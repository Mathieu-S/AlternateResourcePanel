using System;
using System.Collections.Generic;
using System.Linq;
using KSPPluginFramework;

namespace KSPAlternateResourcePanel
{
    //internal class ARPPart : Part
    //{
    //    internal ARPPart(Part p)
    //    {
    //        this.ID = this.GetInstanceID();
    //        this.SetDecoupledAt();
    //        //this.Resources = new ARPResourceList();
    //    }

    //    internal Int32 ID { get; private set; }
    //    internal Part PartRef;

    //    internal Int32 DecoupledAt { get; private set; }
    //    internal void SetDecoupledAt()
    //    {
    //        this.DecoupledAt = this.PartRef.DecoupledAt();
    //    }

    //    internal PartResourceList Resources
    //    {
    //        get { return this.PartRef.Resources; }
    //    }

    //    //internal ARPResourceList Resources;


    //    //internal void UpdateAll()
    //    //{
    //    //    //this.SetDecoupledAt();
    //    //    foreach (ARPResource ri in this.Resources.Values)
    //    //    {

    //    //    }
    //    //}
    //}

    internal class ARPPartDef
    {
        internal int DecoupledAt;
        internal Part part;

        internal ARPPartDef(Part p)
        {
            part = p;
            DecoupledAt = p.DecoupledAt();
        }
    }

    internal class ARPPartDefList : List<ARPPartDef>
    {
        internal bool LastStageIsResourceOnlyAndEmpty()
        {
            var LastStage = GetLastStage();

            //check each part in the stage
            foreach (var p in this.Where(pa => pa.DecoupledAt == LastStage))
            {
                //if theres an engine then ignore this case
                if (p.part.Modules.OfType<ModuleEngines>().Any() || p.part.Modules.OfType<ModuleEnginesFX>().Any())
                    return false;
                //if theres any resource then ignore this case
                foreach (PartResource r in p.part.Resources)
                    if (r.amount > 0)
                        return false;
            }

            return true;
            //return !HasEngine && !HasFuel;
        }


        internal int GetLastStage()
        {
            if (Count > 0)
                return this.Max(x => x.DecoupledAt);
            return -1;
        }
    }

    internal class ARPPartList : List<Part>
    {
        //internal Int32 LastStage { get { return this.Max(x => x.DecoupledAt()); } }

        //internal List<Part> PartList
        //{
        //    get
        //    {
        //        return this.Select<ARPPart,Part>(x => x.PartRef).ToList<Part>();
        //    }
        //}
    }

    internal class PartResourceVisible
    {
        internal bool AllVisible;
        internal bool LastStageVisible;
    }

    internal class PartResourceVisibleList : Dictionary<int, PartResourceVisible>
    {
        internal void TogglePartResourceVisible(int ResourceID, bool LastStage = false)
        {
            //Are we editing this resource?
            if (ContainsKey(ResourceID))
            {
                if (!LastStage)
                    this[ResourceID].AllVisible = !this[ResourceID].AllVisible;
                else
                    this[ResourceID].LastStageVisible = !this[ResourceID].LastStageVisible;
            }
            else
            {
                //Or adding a new one
                if (!LastStage)
                    Add(ResourceID, new PartResourceVisible {AllVisible = true});
                else
                    Add(ResourceID, new PartResourceVisible {LastStageVisible = true});
            }

            //If they are both false then remove the resource from the list
            if (!(this[ResourceID].AllVisible || this[ResourceID].LastStageVisible))
            {
                Remove(ResourceID);
                if (ResourceRemoved != null)
                    ResourceRemoved(ResourceID);
            }
        }

        internal event ResourceRemovedHandler ResourceRemoved;

        internal delegate void ResourceRemovedHandler(int ResourceID);
    }


    /// <summary>
    ///     Details about a specific resource.
    ///     All Gets from this class should be straight from memory and all input work via Set functions that are called in the
    ///     Repeating function
    /// </summary>
    public class ARPResource
    {
        public enum AlarmStateEnum
        {
            None,
            Unacknowledged,
            Acknowledged
        }

        public enum MonitorStateEnum
        {
            None,
            Warn,
            Alert
        }

        private AlarmStateEnum _AlarmState;

        private double _Amount;

        private MonitorStateEnum _MonitorState;
        internal DateTime FullAt; // { get; set; }


        internal bool IsEmpty;
        internal bool IsFull;

        internal ARPResource(PartResourceDefinition ResourceDefinition, ResourceSettings ResourceConfig)
        {
            ResourceDef = ResourceDefinition;
            this.ResourceConfig = ResourceConfig;
        }

        public PartResourceDefinition ResourceDef { get; }
        public ResourceSettings ResourceConfig { get; }

        internal double Amount
        {
            get => _Amount;
            set
            {
                var oldValue = _Amount;
                _Amount = value;
                //if (oldValue != value)
                //{
                //    if (value <= 0)
                //    {
                //        IsEmpty = true;
                //        EmptyAt = DateTime.Now;
                //        //IsFull = false;
                //    }
                //    //else if (value >= MaxAmount) {
                //    //    MonoBehaviourExtended.LogFormatted("Full:{0}-{1}", value, MaxAmount);
                //    //    IsFull = true;
                //    //    FullAt = DateTime.Now;
                //    //    IsEmpty = false;
                //    //}
                //    else {
                //        IsEmpty = false;
                //        //IsFull = false;
                //    }
                //}
            }
        }

        //internal Double Amount {get; set;}
        internal double MaxAmount { get; set; }
        public double AmountValue => Amount;
        public string AmountFormatted => DisplayValue(Amount);
        public double MaxAmountValue => MaxAmount;
        public string MaxAmountFormatted => DisplayValue(MaxAmount);

        internal DateTime EmptyAt { get; set; }

        public MonitorStateEnum MonitorState
        {
            get => _MonitorState;
            private set
            {
                var oldValue = _MonitorState;
                _MonitorState = value;
                if (oldValue != value)
                {
                    if (value > oldValue)
                    {
                        //if severity increased then unacknowledge the state
                        if (ResourceConfig.AlarmEnabled && KSPAlternateResourcePanel.settings.AlarmsEnabled)
                            AlarmState = AlarmStateEnum.Unacknowledged;
                    }
                    else if (value == MonitorStateEnum.None)
                    {
                        //Shortcut the alarmstate if the monitors all turned off
                        _AlarmState = AlarmStateEnum.None;
                    }

                    //MonoBehaviourExtended.LogFormatted_DebugOnly("ResMON-{0}:{1}->{2} ({3})", this.ResourceDef.name, oldValue, value, this.AlarmState);
                    if (OnMonitorStateChanged != null)
                        OnMonitorStateChanged(this, oldValue, value, AlarmState);
                }
            }
        }

        public AlarmStateEnum AlarmState
        {
            get => _AlarmState;
            private set
            {
                var oldValue = _AlarmState;
                _AlarmState = value;
                if (oldValue != value)
                {
                    //MonoBehaviourExtended.LogFormatted_DebugOnly("ResALARM-{0}:{1}->{2} ({3})", this.ResourceDef.name, oldValue, value, this.MonitorState);
                    if (value != AlarmStateEnum.Unacknowledged && IsEmpty)
                        EmptyAt = DateTime.Now;

                    if (value != AlarmStateEnum.Unacknowledged && IsFull)
                        FullAt = DateTime.Now;

                    if (OnAlarmStateChanged != null)
                        OnAlarmStateChanged(this, oldValue, value, MonitorState);
                }
            }
        }

        internal double AmountLast { get; private set; }
        internal string AmountLastFormatted => DisplayValue(Amount);

        internal double Rate { get; private set; }

        internal string RateFormatted => DisplayRateValue(Rate);

        internal string RateFormattedAbs => DisplayRateValue(Math.Abs(Rate));

        internal void CalcEmptyandFull()
        {
            IsEmpty = Amount <= 0;
            IsFull = Amount >= MaxAmount;

            //EmptyAt=DateTime.FromFileTime(0);
            //EmptyAt.ToFileTime()=0;

            if (IsEmpty)
            {
                if (EmptyAt == new DateTime())
                    EmptyAt = DateTime.Now;
            }
            else
            {
                EmptyAt = new DateTime();
            }

            if (IsFull)
            {
                if (FullAt == new DateTime())
                    FullAt = DateTime.Now;
            }
            else
            {
                FullAt = new DateTime();
            }
        }

        internal void ResetAmounts()
        {
            Amount = 0;
            MaxAmount = 0;
        }

        internal void SetAlarmAcknowledged()
        {
            if (AlarmState == AlarmStateEnum.Unacknowledged)
                AlarmState = AlarmStateEnum.Acknowledged;
        }

        internal event MonitorStateChangedHandler OnMonitorStateChanged;
        internal event AlarmStateChangedHandler OnAlarmStateChanged;

        internal void SetMonitors()
        {
            var rPercent = Amount / MaxAmount * 100;

            if (ResourceConfig.MonitorDirection == ResourceSettings.MonitorDirections.Low &&
                rPercent <= ResourceConfig.MonitorAlertLevel ||
                ResourceConfig.MonitorDirection == ResourceSettings.MonitorDirections.High &&
                rPercent >= ResourceConfig.MonitorAlertLevel)
                MonitorState = MonitorStateEnum.Alert;
            else if (ResourceConfig.MonitorDirection == ResourceSettings.MonitorDirections.Low &&
                     rPercent <= ResourceConfig.MonitorWarningLevel ||
                     ResourceConfig.MonitorDirection == ResourceSettings.MonitorDirections.High &&
                     rPercent >= ResourceConfig.MonitorWarningLevel)
                MonitorState = MonitorStateEnum.Warn;
            else
                MonitorState = MonitorStateEnum.None;
        }

        internal void SetLastAmount()
        {
            AmountLast = Amount;
        }

        internal void SetRate(double UTPeriod)
        {
            if (UTPeriod > 0)
                Rate = (AmountLast - Amount) / UTPeriod;
            //    //Remmed out code for sampling idea - doesn't work for resourcelist when it is additive
            //    //r.RateSamples.Enqueue(new RateRecord(KSPAlternateResourcePanel.UTUpdate, Resource.amount));
            //    //r.SetRate2();
        }

        internal string DisplayValue(double AmountToDisplay)
        {
            var Amount = AmountToDisplay;
            if (ResourceConfig.DisplayValueAs == ResourceSettings.DisplayUnitsEnum.Tonnes)
                Amount = AmountToDisplay * ResourceDef.density;
            else if (ResourceConfig.DisplayValueAs == ResourceSettings.DisplayUnitsEnum.Kilograms)
                Amount = AmountToDisplay * ResourceDef.density * 1000;

            //Format string - Default
            var strFormat = "{0:0}";
            if (ResourceConfig.DisplayValueAs == ResourceSettings.DisplayUnitsEnum.Tonnes && Math.Abs(Amount) < 1)
                strFormat = "{0:0.000}";
            else if (Math.Abs(Amount) < 100)
                strFormat = "{0:0.00}";
            else if (ResourceConfig.DisplayValueAs == ResourceSettings.DisplayUnitsEnum.Tonnes &&
                     Math.Abs(Amount) < 1000) strFormat = "{0:0.0}";

            //handle the miniature negative value that gets rounded to 0 by string format
            if ((string.Format(strFormat, Amount) == "0.00" || string.Format(strFormat, Amount) == "0.000") &&
                Amount < 0)
                strFormat = "-" + strFormat;

            //Handle large values
            if (Amount < 10000)
                return string.Format(strFormat, Amount);
            if (Amount < 1000000)
                return string.Format("{0:0.0}K", Amount / 1000);
            return string.Format("{0:0.0}M", Amount / 1000000);
        }

        internal string DisplayRateValue(double Amount, bool HighPrecisionBelowOne = false)
        {
            if (Amount == 0) return "-";
            return DisplayValue(Amount);
        }


        internal delegate void MonitorStateChangedHandler(ARPResource sender, MonitorStateEnum oldValue,
            MonitorStateEnum newValue, AlarmStateEnum AlarmState);

        internal delegate void AlarmStateChangedHandler(ARPResource sender, AlarmStateEnum oldValue,
            AlarmStateEnum newValue, MonitorStateEnum MonitorState);


        ///////////////////////////////////////////////////////////////////////////////////////////////
        // This code is for a sampling idea - not implemented yet
        // Should reduce major spikes, but trade off is slower response in values
        ///////////////////////////////////////////////////////////////////////////////////////////////
        //internal Double Rate2 { get; private set; }
        //internal Double SetRate2()
        //{
        //    if (RateSamples.Count > 0)
        //    {
        //        RateRecord newest = RateSamples.OrderBy(x => x.UT).Last();
        //        RateRecord oldest = RateSamples.OrderBy(x => x.UT).First();
        //        Double AmountChanged = (newest.Amount - oldest.Amount) / (newest.UT - oldest.UT);
        //        this.Rate2 = AmountChanged;
        //        return AmountChanged;
        //    }
        //    else
        //    {
        //        this.Rate2 = 0;
        //        return 0;
        //    }
        //}

        //internal String RateFormatted2
        //{
        //    get
        //    {
        //        return DisplayValue(this.Rate2);
        //    }
        //}
        //internal LimitedQueue<RateRecord> RateSamples;

        //private Int32 _RateSamplesLimit = 2;
        //public Int32 RateSamplesLimit
        //{
        //    get { return _RateSamplesLimit; }
        //    set
        //    {
        //        _RateSamplesLimit = value;
        //        //set the limited queue length when tis changes
        //        RateSamples.Limit = _RateSamplesLimit;
        //    }
        //}
        ///////////////////////////////////////////////////////////////////////////////////////////////
    }


    ///// <summary>
    ///// Simple Class that stores the amount of a Resource at a recorded UT
    ///// </summary>
    //internal class RateRecord
    //{
    //    internal RateRecord(Double UT,Double Amount)
    //    {
    //        this.UT=UT;
    //        this.Amount=Amount;
    //    }

    //    internal Double UT;
    //    internal Double Amount;
    //}

    public class ARPResourceList : Dictionary<int, ARPResource>
    {
        private ResourceUpdate _UpdateType;

        //Should we be storing the UT values in here somewhere instead of referencing back to a static object???
        private double RepeatingWorkerUTPeriod;
        private readonly Dictionary<int, ResourceSettings> ResourceConfigs;

        internal ARPResourceList(ResourceUpdate UpdateType, Dictionary<int, ResourceSettings> ResourceConfigs)
        {
            this.UpdateType = UpdateType;
            this.ResourceConfigs = ResourceConfigs;
        }

        /// <summary>
        ///     This boolean flag controls whether the ResourceList can be updated or not
        ///     This is important for additive lists where we dont want to do the Rate Calcs till all the resources have been added
        /// </summary>
        internal ResourceUpdate UpdateType
        {
            get => _UpdateType;
            set
            {
                UpdatingList = value == ResourceUpdate.SetValues;
                _UpdateType = value;
            }
        }

        internal bool UpdatingList { get; private set; }

        internal void StartUpdatingList(bool CalcRates, double RepeatingWorkerUTPeriod)
        {
            UpdatingList = true;
            this.RepeatingWorkerUTPeriod = RepeatingWorkerUTPeriod;
            foreach (var r in Values)
            {
                if (CalcRates) r.SetLastAmount();
                r.ResetAmounts();
            }
        }

        internal void EndUpdatingList(bool CalcRates)
        {
            UpdatingList = false;

            CalcEmptyandFulls();

            if (CalcRates)
                CalcListRates();
        }

        internal void CleanResources(List<int> ExistingIDs)
        {
            var IDsToRemove = Keys.Except(ExistingIDs).ToList();
            foreach (var rID in IDsToRemove)
            {
                MonoBehaviourExtended.LogFormatted_DebugOnly("Removing Resource-{0}", rID);
                Remove(rID);
            }
        }

        internal ARPResource AddResource(PartResource ResourceToAdd, out bool NewResource)
        {
            if (!UpdatingList) throw new SystemException("List is additive and Updating Flag has not been set");
            int ResourceID = ResourceToAdd.info.id;
            if (!ContainsKey(ResourceID))
            {
                Add(ResourceID, new ARPResource(ResourceToAdd.info, ResourceConfigs[ResourceID]));

                //set the initial alarm states before enabling the events
                NewResource = true;

                this[ResourceID].OnMonitorStateChanged += ARPResourceList_OnMonitorStateChanged;
                this[ResourceID].OnAlarmStateChanged += ARPResourceList_OnAlarmStateChanged;
            }
            else
            {
                NewResource = false;
            }

            return this[ResourceID];
        }

        internal event ARPResource.MonitorStateChangedHandler OnMonitorStateChanged;
        internal event ARPResource.AlarmStateChangedHandler OnAlarmStateChanged;

        private void ARPResourceList_OnMonitorStateChanged(ARPResource sender, ARPResource.MonitorStateEnum oldValue,
            ARPResource.MonitorStateEnum newValue, ARPResource.AlarmStateEnum AlarmState)
        {
            MonoBehaviourExtended.LogFormatted_DebugOnly("LISTMon-{0}:{1}->{2} ({3})", sender.ResourceDef.name,
                oldValue, newValue, sender.AlarmState);
            if (OnMonitorStateChanged != null)
                OnMonitorStateChanged(sender, oldValue, newValue, AlarmState);
        }

        private void ARPResourceList_OnAlarmStateChanged(ARPResource sender, ARPResource.AlarmStateEnum oldValue,
            ARPResource.AlarmStateEnum newValue, ARPResource.MonitorStateEnum MonitorState)
        {
            MonoBehaviourExtended.LogFormatted_DebugOnly("LISTAck-{0}:{1}->{2} ({3})", sender.ResourceDef.name,
                oldValue, newValue, sender.MonitorState);
            if (OnAlarmStateChanged != null)
                OnAlarmStateChanged(sender, oldValue, newValue, MonitorState);
        }

        internal bool UnacknowledgedAlarms()
        {
            var blnReturn = false;
            foreach (var r in Values)
                if (r.AlarmState == ARPResource.AlarmStateEnum.Unacknowledged &&
                    ResourceConfigs[r.ResourceDef.id].AlarmEnabled)
                {
                    blnReturn = true;
                    break;
                }

            return blnReturn;
        }

        internal void SetUTPeriod(double UTPeriod)
        {
            RepeatingWorkerUTPeriod = UTPeriod;
        }

        internal ARPResource UpdateResource(PartResource Resource, bool CalcRates = false)
        {
            if (!UpdatingList) throw new SystemException("List is additive and Updating Flag has not been set");
            //Get the Resource (or create it if needed)
            var NewResource = false;
            var r = AddResource(Resource, out NewResource);

            //Are we adding or setting the amounts
            switch (UpdateType)
            {
                case ResourceUpdate.AddValues:
                    r.Amount += Resource.amount;
                    r.MaxAmount += Resource.maxAmount;
                    break;
                case ResourceUpdate.SetValues:
                    if (CalcRates)
                        r.SetLastAmount();
                    r.Amount = Resource.amount;
                    r.MaxAmount = Resource.maxAmount;
                    if (CalcRates)
                        r.SetRate(RepeatingWorkerUTPeriod);
                    break;
                default:
                    throw new SystemException("Invalid ResourceUpdate Type");
            }

            if (NewResource)
            {
                r.SetMonitors();
                r.SetAlarmAcknowledged();
            }

            return r;
        }

        internal void CalcEmptyandFulls()
        {
            foreach (var r in Values) r.CalcEmptyandFull();
        }


        internal void CalcListRates()
        {
            foreach (var r in Values) r.SetRate(RepeatingWorkerUTPeriod);
        }


        /// <summary>
        ///     How to set the amounts in a resourceupdate
        /// </summary>
        internal enum ResourceUpdate
        {
            /// <summary>
            ///     Add new values to existing amounts
            /// </summary>
            AddValues,

            /// <summary>
            ///     Set the values to the provided ones
            /// </summary>
            SetValues
        }
    }

    internal class ARPTransfer
    {
        internal bool Active;

        internal Part part;
        internal float RatePerSec;
        internal PartResourceDefinition resource;
        internal TransferStateEnum transferState;

        internal ARPTransfer()
        {
        }

        internal ARPTransfer(Part p, PartResourceDefinition RD, TransferStateEnum State)
        {
            part = p;
            resource = RD;
            transferState = State;
            Active = false;
        }

        internal int partID => part.GetInstanceID();
        internal int ResourceID => resource.id;
    }

    internal enum TransferStateEnum
    {
        None,
        In,
        Out
    }

    internal class ARPTransferList : List<ARPTransfer>
    {
        internal float RatePerSecMax
        {
            get { return this.Max(t => t.RatePerSec); }
        }

        internal void AddItem(Part p, PartResourceDefinition RD, TransferStateEnum State)
        {
            Add(new ARPTransfer(p, RD, State));
        }

        internal bool ItemExists(int PartID, int ResourceID)
        {
            return this.Any(x => x.partID == PartID && x.ResourceID == ResourceID);
        }

        internal ARPTransfer GetItem(int PartID, int ResourceID)
        {
            return this.FirstOrDefault(x => x.partID == PartID && x.ResourceID == ResourceID);
        }

        internal void SetStateNone(int ResourceID)
        {
            foreach (var t in this.Where(x => x.ResourceID == ResourceID)) t.transferState = TransferStateEnum.None;
        }

        internal void SetStateNone(int ResourceID, TransferStateEnum State)
        {
            foreach (var t in this.Where(x => x.ResourceID == ResourceID && x.transferState == State))
                t.transferState = TransferStateEnum.None;
        }

        internal void RemoveItem(int PartID, int ResourceID)
        {
            var toRemove = GetItem(PartID, ResourceID);
            if (toRemove != null)
                Remove(toRemove);
        }

        internal void RemovePartItems(int PartID)
        {
            var toRemove = this.Where(x => x.partID == PartID).ToList();
            foreach (var item in toRemove) Remove(item);
        }

        internal void RemoveResourceItems(int ResourceID)
        {
            var toRemove = this.Where(x => x.ResourceID == ResourceID).ToList();
            foreach (var item in toRemove) Remove(item);
        }
    }
}