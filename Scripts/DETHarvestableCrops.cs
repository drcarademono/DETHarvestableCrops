// Project:         Harvestable Crops for Daggerfall Unity
// License:         MIT License (http://www.opensource.org/licenses/mit-license.php)
// Developer:       TheLacus

using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using DaggerfallWorkshop;
using DaggerfallWorkshop.Game;
using DaggerfallWorkshop.Game.Items;
using DaggerfallWorkshop.Game.Serialization;
using DaggerfallWorkshop.Game.UserInterfaceWindows;
using DaggerfallWorkshop.Game.Utility.ModSupport;
using DaggerfallWorkshop.Utility;
using DaggerfallConnect.Arena2;

namespace DETHarvestableCrops
{
    #region Types

    internal class TextProvider
    {
        internal string SuccessOnce { get; private set; }
        internal string SuccessAmount { get; private set; }
        internal string Failure { get; private set; }
        internal string LowReputation { get; private set; }
        internal string TheftDetected { get; private set; }

        internal TextProvider(Mod mod)
        {
            SuccessOnce = mod.Localize("SuccessOnce");
            SuccessAmount = mod.Localize("SuccessAmount");
            Failure = mod.Localize("Failure");
            LowReputation = mod.Localize("LowReputation");
            TheftDetected = mod.Localize("TheftDetected");
        }
    }

    [Serializable]
    public class ItemTemplateData
    {
        public int index;
        public string name;
        public float baseWeight;
        public int hitPoints;
        public int capacityOrTarget;
        public int basePrice;
        public int enchantmentPoints;
        public int rarity;
        public int variants;
        public int drawOrderOrEffect;
        public bool isBluntWeapon;
        public bool isLiquid;
        public bool isOneHanded;
        public bool isIngredient;
        public int worldTextureArchive;
        public int worldTextureRecord;
        public int playerTextureArchive;
        public int playerTextureRecord;
    }

    [FullSerializer.fsObject("v1")]
    public class DETHarvestableCropsSaveData
    {
        public int Progress;
        public Dictionary<DETHarvestedCrop, int> DETHarvestedCrops;
    }

    #endregion

    /// <summary>
    /// Make crops harvestable.
    /// </summary>
    public class DETHarvestableCrops : MonoBehaviour, IHasModSaveData
    {
        #region Fields

        const int maxProgress = 10000;

        static Mod mod;
        static DETHarvestableCrops instance;

        /// <summary>
        /// Progress of harvesting skill.
        /// </summary>
        int progress;

        /// <summary>
        /// Crops which are currently harvested. Values is harvest day from zero.
        /// </summary>
        Dictionary<DETHarvestedCrop, int> harvestedCrops = new Dictionary<DETHarvestedCrop, int>();

        /// <summary>
        /// Harvested crops are harvestable again after this number of days.
        /// </summary>
        public int GrowDays = 10;

        /// <summary>
        /// Base harvesting is always succesful and not affected by skill.
        /// </summary>
        public bool AlwaysSuccesful;

        /// <summary>
        /// Positive reputation is needed to have permission to harvest farm owned crops.
        /// </summary>
        public bool NeedReputation;

        #endregion

        #region Properties

        public static DETHarvestableCrops Instance
        {
            get { return instance ?? (instance = FindObjectOfType<DETHarvestableCrops>()); }
        }

        public Type SaveDataType
        {
            get { return typeof(DETHarvestableCropsSaveData); }
        }

        /// <summary>
        /// Progress of harvesting skill.
        /// </summary>
        /// <value>A value between 0 and 1.</value>
        internal float SkillProgress
        {
            get { return (float)progress / (float)maxProgress; }
        }

        internal TextProvider TextProvider { get; private set; }

        #endregion

        #region Unity

        [Invoke(StateManager.StateTypes.Start, 0)]
        public static void Init(InitParams initParams)
        {
            GameObject go = new GameObject("DETHarvestableCrops");
            instance = go.AddComponent<DETHarvestableCrops>();

            mod = initParams.Mod;
            mod.SaveDataInterface = instance;
            mod.LoadSettingsCallback = (settings, _) => settings.Deserialize("Options", ref instance);
            mod.LoadSettings();

            mod.IsReady = true;
        }

        private void Awake()
        {
            if (instance != null && instance != this)
                Destroy(gameObject);
        }

        private void Start()
        {
            TextProvider = new TextProvider(mod);

            // Register custom items
            DaggerfallUnity.Instance.ItemHelper.RegisterCustomItem(ItemCherries.templateIndex, ItemGroups.UselessItems1, typeof(ItemCherries));
            DaggerfallUnity.Instance.ItemHelper.RegisterCustomItem(ItemPear.templateIndex, ItemGroups.UselessItems1, typeof(ItemPear));
            DaggerfallUnity.Instance.ItemHelper.RegisterCustomItem(ItemPlum.templateIndex, ItemGroups.UselessItems1, typeof(ItemPlum));
            DaggerfallUnity.Instance.ItemHelper.RegisterCustomItem(ItemPeach.templateIndex, ItemGroups.UselessItems1, typeof(ItemPeach));
            DaggerfallUnity.Instance.ItemHelper.RegisterCustomItem(ItemOlives.templateIndex, ItemGroups.UselessItems1, typeof(ItemOlives));
            DaggerfallUnity.Instance.ItemHelper.RegisterCustomItem(ItemRedGrapes.templateIndex, ItemGroups.UselessItems1, typeof(ItemRedGrapes));
            DaggerfallUnity.Instance.ItemHelper.RegisterCustomItem(ItemWhiteGrapes.templateIndex, ItemGroups.UselessItems1, typeof(ItemWhiteGrapes));
            DaggerfallUnity.Instance.ItemHelper.RegisterCustomItem(ItemCabbageHead.templateIndex, ItemGroups.UselessItems1, typeof(ItemCabbageHead));
            DaggerfallUnity.Instance.ItemHelper.RegisterCustomItem(ItemBundleOfWheat.templateIndex, ItemGroups.UselessItems1, typeof(ItemBundleOfWheat));
            DaggerfallUnity.Instance.ItemHelper.RegisterCustomItem(ItemGrain.templateIndex, ItemGroups.UselessItems1, typeof(ItemGrain));
        }

        #endregion

        #region Public Methods

        public override string ToString()
        {
            if (mod == null)
                return base.ToString();

            return string.Format("{0} v.{1}", mod.Title, mod.ModInfo.ModVersion);
        }

        /// <summary>
        /// Is the crop at this position harvested?
        /// </summary>
        public bool IsHarvested(Vector3 localPosition)
        {
            var harvestedCrop = new DETHarvestedCrop(localPosition);

            int harvestDate;
            bool isHarvested = harvestedCrops.TryGetValue(harvestedCrop, out harvestDate) && !IsHarvestableAgain(harvestDate);

            if (!isHarvested)
                harvestedCrops.Remove(harvestedCrop);

            return isHarvested;
        }

        /// <summary>
        /// Adds a position to the collection of harvested crops positions.
        /// </summary>
        public void SetHarvested(Vector3 localPosition)
        {
            try
            {
                harvestedCrops.Add(new DETHarvestedCrop(localPosition), CurrentDay());
            }
            catch (Exception e)
            {
                LogErrorMessage(string.Format("Failed to set harvested crop.\n{0}", e.ToString()));
            }
        }

        public object NewSaveData()
        {
            return new DETHarvestableCropsSaveData {
                Progress = 0,
                DETHarvestedCrops = new Dictionary<DETHarvestedCrop, int>()
            };
        }

        public object GetSaveData()
        {
            if (harvestedCrops.Count == 0)
                return null;

            return new DETHarvestableCropsSaveData {
                Progress = progress,
                DETHarvestedCrops = harvestedCrops
            };
        }

        public void RestoreSaveData(object saveData)
        {
            var harvestableCropsSaveData = (DETHarvestableCropsSaveData)saveData;
            progress = Mathf.Clamp(harvestableCropsSaveData.Progress, 0, maxProgress);
            harvestedCrops = harvestableCropsSaveData.DETHarvestedCrops;
            RaiseOnRestoreSaveDataEvent();
        }

        #endregion

        #region Internal Methods

        internal void IncreaseSkillProgress()
        {
            if (progress < maxProgress && (++progress % 100) == 0)
            {
                var uiManager = DaggerfallUI.Instance.UserInterfaceManager;
                var messageBox = new DaggerfallMessageBox(uiManager, DaggerfallUI.UIManager.TopWindow);
                messageBox.SetText(string.Format("Harvesting Skill raised to {0}%", SkillProgress * 100));
                messageBox.AllowCancel = true;
                messageBox.ClickAnywhereToClose = true;
                uiManager.PushWindow(messageBox);
            }
        }

        internal void LogErrorMessage(string message, UnityEngine.Object context = null)
        {
            Debug.LogErrorFormat(context ?? this, "{0}: {1}", this.ToString(), message);
        }

        private void LogErrorMessage(string message)
        {
            Debug.LogError(message);
        }

        #endregion

        #region Private Methods

        private bool IsHarvestableAgain(int harvestDate)
        {
            return CurrentDay() - harvestDate > GrowDays;
        }

        private static int CurrentDay()
        {
            var date = DaggerfallUnity.Instance.WorldTime.Now;
            return (date.Year * DaggerfallDateTime.DaysPerYear) + date.DayOfYear;
        }

        #endregion

        #region Event Handlers

        public delegate void OnRestoreSaveDataEventHandler();
        public event OnRestoreSaveDataEventHandler OnRestoreSaveData;
        protected virtual void RaiseOnRestoreSaveDataEvent()
        {
            if (OnRestoreSaveData != null)
                OnRestoreSaveData();
        }

        #endregion
    }
}
