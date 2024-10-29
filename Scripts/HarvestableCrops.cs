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
using DaggerfallConnect.FallExe;

namespace HarvestableCrops
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

    public enum DETIngredients
    {
        Cherries = 288,
        Pear = 289,
        Plum = 290,
        Peach = 291,
        Olives = 292,
        Red_grapes = 293,
        White_grapes = 294,
        Cabbage_head = 295,
        Bundle_of_wheat = 296,
        Grain = 297
    }

    [FullSerializer.fsObject("v1")]
    public class HarvestableCropsSaveData
    {
        public int Progress;
        public Dictionary<HarvestedCrop, int> HarvestedCrops;
    }

    #endregion

    /// <summary>
    /// Make crops harvestable.
    /// </summary>
    public class HarvestableCrops : MonoBehaviour, IHasModSaveData
    {
        #region Fields

        const int maxProgress = 10000;

        static Mod mod;
        static HarvestableCrops instance;

        /// <summary>
        /// Progress of harvesting skill.
        /// </summary>
        int progress;

        /// <summary>
        /// Crops which are currently harvested. Values is harvest day from zero.
        /// </summary>
        Dictionary<HarvestedCrop, int> harvestedCrops = new Dictionary<HarvestedCrop, int>();

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

        public static HarvestableCrops Instance
        {
            get { return instance ?? (instance = FindObjectOfType<HarvestableCrops>()); }
        }

        public Type SaveDataType
        {
            get { return typeof(HarvestableCropsSaveData); }
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
            GameObject go = new GameObject("HarvestableCrops");
            instance = go.AddComponent<HarvestableCrops>();

            mod = initParams.Mod;
            mod.SaveDataInterface = instance;
            mod.LoadSettingsCallback = (settings, _) => settings.Deserialize("Options", ref instance);
            mod.LoadSettings();

            instance.RegisterCustomItems();

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
            var harvestedCrop = new HarvestedCrop(localPosition);

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
                harvestedCrops.Add(new HarvestedCrop(localPosition), CurrentDay());
            }
            catch (Exception e)
            {
                LogErrorMessage(string.Format("Failed to set harvested crop.\n{0}", e.ToString()));
            }
        }

        public object NewSaveData()
        {
            return new HarvestableCropsSaveData {
                Progress = 0,
                HarvestedCrops = new Dictionary<HarvestedCrop, int>()
            };
        }

        public object GetSaveData()
        {
            if (harvestedCrops.Count == 0)
                return null;

            return new HarvestableCropsSaveData {
                Progress = progress,
                HarvestedCrops = harvestedCrops
            };
        }

        public void RestoreSaveData(object saveData)
        {
            var harvestableCropsSaveData = (HarvestableCropsSaveData)saveData;
            progress = Mathf.Clamp(harvestableCropsSaveData.Progress, 0, maxProgress);
            harvestedCrops = harvestableCropsSaveData.HarvestedCrops;
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

internal void RegisterCustomItems()
{
    ItemHelper itemHelper = DaggerfallUnity.Instance.ItemHelper;

    // Register each custom ingredient from the static array
    foreach (var template in CustomItemTemplates)
    {
        int index = template.index;
        string name = template.name;

        // Register custom item with ItemHelper
        itemHelper.RegisterCustomItem(index, ItemGroups.PlantIngredients1);

        Debug.Log($"Registered custom ingredient: {name} with index {index}");
    }

    int[] customItems = itemHelper.GetCustomItemsForGroup(ItemGroups.PlantIngredients1);

    Debug.Log("Custom items registered in PlantIngredients1:");
    foreach (int itemIndex in customItems)
    {
        ItemTemplate itemTemplate = itemHelper.GetItemTemplate(ItemGroups.PlantIngredients1, itemIndex);

        // Check if the itemTemplate is essentially uninitialized
        if (itemTemplate.index == 0 && string.IsNullOrEmpty(itemTemplate.name))
        {
            Debug.LogWarning($"Failed to retrieve valid ItemTemplate for index: {itemIndex}");
        }
        else
        {
            Debug.Log($"Item index: {itemIndex}, Name: {itemTemplate.name}");
        }
    }
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

        public static readonly ItemTemplateData[] CustomItemTemplates = new ItemTemplateData[]
        {
            new ItemTemplateData
            {
                index = 750,
                name = "Cherries",
                baseWeight = 0.25f,
                hitPoints = 4,
                capacityOrTarget = 0,
                basePrice = 2,
                enchantmentPoints = 0,
                rarity = 3,
                variants = 5,
                drawOrderOrEffect = 25,
                isBluntWeapon = false,
                isLiquid = false,
                isOneHanded = false,
                isIngredient = true,
                worldTextureArchive = 1021,
                worldTextureRecord = 0,
                playerTextureArchive = 1021,
                playerTextureRecord = 0
            },
            new ItemTemplateData
            {
                index = 751,
                name = "Pear",
                baseWeight = 0.25f,
                hitPoints = 5,
                capacityOrTarget = 0,
                basePrice = 2,
                enchantmentPoints = 0,
                rarity = 3,
                variants = 12,
                drawOrderOrEffect = 27,
                isBluntWeapon = false,
                isLiquid = false,
                isOneHanded = false,
                isIngredient = true,
                worldTextureArchive = 1021,
                worldTextureRecord = 1,
                playerTextureArchive = 1021,
                playerTextureRecord = 1
            },
            new ItemTemplateData
            {
                index = 752,
                name = "Plum",
                baseWeight = 0.25f,
                hitPoints = 5,
                capacityOrTarget = 1,
                basePrice = 4,
                enchantmentPoints = 0,
                rarity = 3,
                variants = 16,
                drawOrderOrEffect = 4,
                isBluntWeapon = false,
                isLiquid = false,
                isOneHanded = false,
                isIngredient = true,
                worldTextureArchive = 1021,
                worldTextureRecord = 2,
                playerTextureArchive = 1021,
                playerTextureRecord = 2
            },
            new ItemTemplateData
            {
                index = 753,
                name = "Peach",
                baseWeight = 0.25f,
                hitPoints = 5,
                capacityOrTarget = 0,
                basePrice = 4,
                enchantmentPoints = 0,
                rarity = 3,
                variants = 12,
                drawOrderOrEffect = 23,
                isBluntWeapon = false,
                isLiquid = false,
                isOneHanded = false,
                isIngredient = true,
                worldTextureArchive = 1021,
                worldTextureRecord = 3,
                playerTextureArchive = 1021,
                playerTextureRecord = 3
            },
            new ItemTemplateData
            {
                index = 754,
                name = "Olives",
                baseWeight = 0.25f,
                hitPoints = 12,
                capacityOrTarget = 0,
                basePrice = 2,
                enchantmentPoints = 0,
                rarity = 3,
                variants = 14,
                drawOrderOrEffect = 32,
                isBluntWeapon = false,
                isLiquid = false,
                isOneHanded = false,
                isIngredient = true,
                worldTextureArchive = 1021,
                worldTextureRecord = 4,
                playerTextureArchive = 1021,
                playerTextureRecord = 4
            },
            new ItemTemplateData
            {
                index = 755,
                name = "Red_grapes",
                baseWeight = 0.25f,
                hitPoints = 5,
                capacityOrTarget = 0,
                basePrice = 4,
                enchantmentPoints = 0,
                rarity = 4,
                variants = 14,
                drawOrderOrEffect = 18,
                isBluntWeapon = false,
                isLiquid = false,
                isOneHanded = false,
                isIngredient = true,
                worldTextureArchive = 1021,
                worldTextureRecord = 14,
                playerTextureArchive = 1021,
                playerTextureRecord = 14
            },
            new ItemTemplateData
            {
                index = 756,
                name = "White_grapes",
                baseWeight = 0.25f,
                hitPoints = 5,
                capacityOrTarget = 0,
                basePrice = 4,
                enchantmentPoints = 0,
                rarity = 3,
                variants = 9,
                drawOrderOrEffect = 4,
                isBluntWeapon = false,
                isLiquid = false,
                isOneHanded = false,
                isIngredient = true,
                worldTextureArchive = 1021,
                worldTextureRecord = 15,
                playerTextureArchive = 1021,
                playerTextureRecord = 15
            },
            new ItemTemplateData
            {
                index = 757,
                name = "Cabbage_head",
                baseWeight = 0.5f,
                hitPoints = 5,
                capacityOrTarget = 0,
                basePrice = 4,
                enchantmentPoints = 0,
                rarity = 2,
                variants = 12,
                drawOrderOrEffect = 8,
                isBluntWeapon = false,
                isLiquid = false,
                isOneHanded = false,
                isIngredient = true,
                worldTextureArchive = 1021,
                worldTextureRecord = 16,
                playerTextureArchive = 1021,
                playerTextureRecord = 16
            },
            new ItemTemplateData
            {
                index = 758,
                name = "Bundle_of_wheat",
                baseWeight = 2f,
                hitPoints = 12,
                capacityOrTarget = 0,
                basePrice = 2,
                enchantmentPoints = 0,
                rarity = 1,
                variants = 14,
                drawOrderOrEffect = 32,
                isBluntWeapon = false,
                isLiquid = false,
                isOneHanded = false,
                isIngredient = true,
                worldTextureArchive = 1024,
                worldTextureRecord = 0,
                playerTextureArchive = 1024,
                playerTextureRecord = 0
            },
            new ItemTemplateData
            {
                index = 759,
                name = "Grain",
                baseWeight = 5f,
                hitPoints = 12,
                capacityOrTarget = 0,
                basePrice = 4,
                enchantmentPoints = 0,
                rarity = 1,
                variants = 14,
                drawOrderOrEffect = 18,
                isBluntWeapon = false,
                isLiquid = false,
                isOneHanded = false,
                isIngredient = true,
                worldTextureArchive = 1024,
                worldTextureRecord = 2,
                playerTextureArchive = 1024,
                playerTextureRecord = 2
            }
        };
    }
}
