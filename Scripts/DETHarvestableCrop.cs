// Project:         Harvestable Crops for Daggerfall Unity
// License:         MIT License (http://www.opensource.org/licenses/mit-license.php)
// Developer:       TheLacus

using System;
using UnityEngine;
using Random = UnityEngine.Random;
using DaggerfallConnect.Arena2;
using DaggerfallConnect.FallExe;
using Stats = DaggerfallConnect.DFCareer.Stats;
using DaggerfallWorkshop;
using DaggerfallWorkshop.Game;
using DaggerfallWorkshop.Game.Items;
using DaggerfallWorkshop.Game.Entity;
using DaggerfallWorkshop.Game.Formulas;
using DaggerfallWorkshop.Game.UserInterfaceWindows;
using DaggerfallWorkshop.Game.Utility;
using DaggerfallWorkshop.Game.Utility.ModSupport;
using DaggerfallWorkshop.Utility;

namespace DETHarvestableCrops
{
    /// <summary>
    /// Makes a crop lootable for ingredients according to character luck.
    /// </summary>
    [ImportedComponent]
    [RequireComponent(typeof(CapsuleCollider))]
    public class DETHarvestableCrop : MonoBehaviour, IPlayerActivable
    {
        #region Fields

        const SoundClips harvestSound = SoundClips.EquipLeather;

        readonly static int[] plantIngredients1 = GetEnumValues(typeof(PlantIngredients1));
        readonly static int[] plantIngredients2 = GetEnumValues(typeof(PlantIngredients2));

        [Tooltip("Texture archive for the billboard.")]
        public int Archive;

        [Tooltip("Texture record for the billboard.")]
        public int Record;

        // Enum to represent each season
        [Flags]
        public enum HarvestSeasons
        {
            None = 0,
            Winter = 1,
            Spring = 2,
            Summer = 4,
            Fall = 8
        }

        // Set which seasons are valid for harvesting in the Inspector
        [Tooltip("Select the seasons when this crop can be harvested.")]
        public HarvestSeasons validSeasons = HarvestSeasons.Spring | HarvestSeasons.Summer | HarvestSeasons.Fall; // Default: spring, summer, fall


        [Tooltip("One of the two ingredient groups.")]
        public ItemGroups IngredientGroup = ItemGroups.PlantIngredients1;

        [Tooltip("Harvestable ingredient if IngredientGroup is PlantIngredients1.")]
        public PlantIngredients1 PlantIngredients1 = PlantIngredients1.Twigs;

        [Tooltip("Harvestable ingredient if IngredientGroup is PlantIngredients2.")]
        public PlantIngredients2 PlantIngredients2 = PlantIngredients2.Twigs;

        [Tooltip("Index of the custom ingredient, if any.")]
        public int CustomIngredient = 0; 

        TextProvider textProvider;
        GameObject billboardGo;
        new CapsuleCollider collider;
        bool isHarvested = false;
        int ingredientIndex;
        string ingredientName;

        #endregion

        #region Unity

        private void Awake()
        {
            DETHarvestableCrops.Instance.OnRestoreSaveData += DETHarvestableCrops_OnRestoreSaveData;
        }

        private void Start()
        {
            // Initialize text provider
            if ((textProvider = DETHarvestableCrops.Instance.TextProvider) == null)
                DETHarvestableCrops.Instance.LogErrorMessage("TextProvider is null.");

            // Get current season
            DaggerfallDateTime dateTime = DaggerfallUnity.Instance.WorldTime.Now;
            DaggerfallDateTime.Seasons currentSeason = dateTime.SeasonValue;

            // Determine the appropriate archive and collider status based on the season
            int archive;
            bool enableCollider = true;

            int record = Record;

            switch (currentSeason)
            {
                case DaggerfallDateTime.Seasons.Winter:
                    // Check if in desert region
                    if (IsInDesert())
                    {
                        archive = Archive; // Use spring texture in deserts during winter
                    }
                    else
                    {
                        if(Archive == 10035) {
                            archive = 10038; // Use winter texture
                        } else {
                            archive = 511;
                            record = 22;                       
                        }
                        enableCollider = false; // Disable collider for winter (non-desert)
                    }
                    break;

                case DaggerfallDateTime.Seasons.Spring:
                    archive = Archive; // Spring texture
                    break;

                case DaggerfallDateTime.Seasons.Summer:
                        if(Archive == 10035) {
                            archive = 10036; // Use winter texture
                        } else {
                            archive = Archive;                    
                        }
                    break;

                case DaggerfallDateTime.Seasons.Fall:
                        if(Archive == 10035) {
                            archive = 10037; // Use winter texture
                        } else {
                            archive = Archive;                    
                        }
                    break;

                default:
                    archive = Archive; // Default to spring texture as a fallback
                    break;
            }

            // Set up the billboard and collider
            SetupBillboardWithTrigger(archive, record);
            collider.enabled = enableCollider;

            // Set harvestable ingredient
            // Set ingredient index based on custom ingredient or regular ingredient group
            if (CustomIngredient != 0)
            {
                ingredientIndex = CustomIngredient;  // Use custom ingredient index if set
            }
            else if (IngredientGroup == ItemGroups.PlantIngredients1)
            if (IngredientGroup == ItemGroups.PlantIngredients1)
                ingredientIndex = Array.IndexOf(plantIngredients1, (int)PlantIngredients1);
            else if (IngredientGroup == ItemGroups.PlantIngredients2)
                ingredientIndex = Array.IndexOf(plantIngredients2, (int)PlantIngredients2);

            // Refresh harvested state if needed
            if (transform.localPosition != Vector3.zero)
                RefreshHarvestedState();
        }

        private void OnDestroy()
        {
            DETHarvestableCrops.Instance.OnRestoreSaveData -= DETHarvestableCrops_OnRestoreSaveData;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Triggers when player click on crop and activates behaviour corresponding to current interaction mode.
        /// </summary>
        /// <param name="hit">The raycast that activate collider.</param>
        public void Activate(RaycastHit hit)
        {
            if (hit.distance > 128 * MeshReader.GlobalScale)
            {
                DaggerfallUI.SetMidScreenText(TextManager.Instance.GetLocalizedText("youAreTooFarAway"));
                return;
            }

            string message = null;
            switch (GameManager.Instance.PlayerActivate.CurrentMode)
            {
                case PlayerActivateModes.Info:
                    message = GetIngredientName();
                    break;

                case PlayerActivateModes.Grab:
                    message = HasEnoughReputation() ? Harvest(false) : textProvider.LowReputation;
                    break;

                case PlayerActivateModes.Steal:
                    message = Harvest(!HasEnoughReputation());
                    break;
            }

            if (message != null)
                DaggerfallUI.Instance.PopupMessage(message);

        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Hides crop mesh and increase skill progress.
        /// If harvest is succesful, adds a number of corresponding ingredients to inventory.
        /// </summary>
        /// <param name="theft">Checks if theft is detected and sets theft as current crime.</param>
        /// <returns>A message to be shown on hud or null.</returns>
        private string Harvest(bool theft)
        {
            string message = null;
            PlaySoundClip(harvestSound);
            int amount = AttemptHarvest();
            if (amount > 0)
            {
                DaggerfallUnityItem ingredient = new DaggerfallUnityItem(IngredientGroup, ingredientIndex);
                if (!theft || !TheftDetected(ingredient.weightInKg, amount))
                {
                    GameManager.Instance.PlayerEntity.Items.AddItem(ingredient);
                    for (int i = 1; i < amount; i++)
                        GameManager.Instance.PlayerEntity.Items.AddItem(new DaggerfallUnityItem(ingredient));

                    if (amount == 1)
                        message = string.Format(textProvider.SuccessOnce, ingredient.ItemName);
                    else
                        message = string.Format(textProvider.SuccessAmount, ingredient.ItemName, amount);
                }
            }
            else
            {
                if (!theft || !TheftDetected(GetItemTemplate().baseWeight, 1))
                    message = textProvider.Failure;
            }

            ToggleBillboard(true);
            DETHarvestableCrops.Instance.SetHarvested(transform.localPosition);
            DETHarvestableCrops.Instance.IncreaseSkillProgress();
            return message;
        }

        /// <summary>
        /// Play a clip at the crop position.
        /// </summary>
        private void PlaySoundClip(SoundClips sound)
        {
            AudioClip audioClip = DaggerfallUnity.Instance.SoundReader.GetAudioClip((int)sound);
            AudioSource.PlayClipAtPoint(audioClip, transform.position);
        }

        /// <summary>
        /// Makes a billboard component attached to this gameobject with the given archive and record texture.
        /// Also sets size and position of trigger collider.
        /// </summary>
        private void SetupBillboardWithTrigger(int archive, int record)
        {
            billboardGo = GameObjectHelper.CreateDaggerfallBillboardGameObject(archive, record, transform);
            billboardGo.transform.localPosition = Vector3.zero;
            var billboard = billboardGo.GetComponent<DaggerfallBillboard>();
            billboard.AlignToBase();

            collider = GetComponent<CapsuleCollider>();
            collider.center = billboardGo.transform.localPosition;
            collider.height = billboard.Summary.Size.y;
            collider.radius = billboard.Summary.Size.x * 0.5f;
            collider.isTrigger = true;
        }

        /// <summary>
        /// Toggle visibility of crop billboard.
        /// </summary>
        private void ToggleBillboard(bool isHarvested)
        {
            if (billboardGo && collider)
            {
                billboardGo.SetActive(!(this.isHarvested = isHarvested));
                collider.enabled = !isHarvested;
            }
            else
            {
                DETHarvestableCrops.Instance.LogErrorMessage("Failed to toggle visibility because object instance is null.", this);
            }
        }

        /// <summary>
        /// Sync the state of this crop with the central collection.
        /// </summary>
        private void RefreshHarvestedState()
        {
            if (DETHarvestableCrops.Instance.IsHarvested(transform.localPosition) != isHarvested)
                ToggleBillboard(!isHarvested);
        }
        
        /// <summary>
        /// Gets the name of the ingredient that can be harvested from this crop.
        /// </summary>
        /// <returns>The name of the ingredient.</returns>
        private string GetIngredientName()
        {
            return ingredientName ?? (ingredientName = GetItemTemplate().name);
        }

        /// <summary>
        /// Checks if player has a positive reputation and can harvest crops legally.
        /// </summary>
        private bool HasEnoughReputation()
        {
            if (!DETHarvestableCrops.Instance.NeedReputation)
                return true;

            int index = GameManager.Instance.PlayerGPS.CurrentRegionIndex;
            return GameManager.Instance.PlayerEntity.RegionData[index].LegalRep >= 0;
        }
        
        /// <summary>
        /// Checks if steal attempt has been detected.
        /// </summary>
        /// <param name="weight">Weight of ingredient.</param>
        /// <param name="amount">Amount of harvested ingredients.</param>
        /// <returns>True if theft detected.</returns>
        private bool TheftDetected(float weight, int amount)
        {
            var playerEntity = GameManager.Instance.PlayerEntity;
            if (Dice100.FailedRoll(FormulaHelper.CalculateShopliftingChance(playerEntity, 10, (int)weight + amount)))
            {
                DaggerfallUI.SetMidScreenText(textProvider.TheftDetected);
                playerEntity.CrimeCommitted = PlayerEntity.Crimes.Theft;
                playerEntity.SpawnCityGuards(false);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Gets the item template for the ingredient that can be harvested from this crop.
        /// </summary>
        /// <returns>The item template associated to the ingredient for this crop.</returns>
        private ItemTemplate GetItemTemplate()
        {
            return GameManager.Instance.ItemHelper.GetItemTemplate(IngredientGroup, ingredientIndex);
        }

        #endregion

        #region Static Methods

        private static int[] GetEnumValues(Type type)
        {
            if (!type.IsEnum)
                throw new Exception(string.Format("Type {0} is not enum", type.FullName));

            Array array = Enum.GetValues(type);
            int[] intArray = new int[array.Length];
            array.CopyTo(intArray, 0);
            return intArray;
        }

        /// <summary>
        /// Checks if harvest is successful and returns the number of harvested ingredients.
        /// </summary>
        /// <returns>A value that is equal or greater than zero.</returns>
        private int AttemptHarvest()
        {
            const int max = 3;

            // Check if the current season is valid for harvesting
            DaggerfallDateTime.Seasons currentSeason = DaggerfallUnity.Instance.WorldTime.Now.SeasonValue;
            HarvestSeasons currentHarvestSeason = ConvertToHarvestSeason(currentSeason);

            // If the current season is not valid, return 0
            if ((validSeasons & currentHarvestSeason) == 0)
                return 0;

            // Calculate harvest chance based on luck and skill progress
            int luck = GameManager.Instance.PlayerEntity.Stats.GetLiveStatValue(Stats.Luck);
            float chance = Mathf.Lerp(0.1f, 0.95f, DETHarvestableCrops.Instance.SkillProgress + (((float)luck / 1000) - 0.05f));

            for (int i = 0; i < max; i++)
            {
                if (Random.value >= chance)
                    return Mathf.Max(DETHarvestableCrops.Instance.AlwaysSuccesful ? 1 : 0, i);
            }

            return max;
        }

        /// <summary>
        /// Converts the DaggerfallDateTime.Seasons to HarvestSeasons enum.
        /// </summary>
        /// <param name="season">The current season in Daggerfall.</param>
        /// <returns>The equivalent HarvestSeasons value.</returns>
        private static HarvestSeasons ConvertToHarvestSeason(DaggerfallDateTime.Seasons season)
        {
            switch (season)
            {
                case DaggerfallDateTime.Seasons.Winter:
                    return HarvestSeasons.Winter;
                case DaggerfallDateTime.Seasons.Spring:
                    return HarvestSeasons.Spring;
                case DaggerfallDateTime.Seasons.Summer:
                    return HarvestSeasons.Summer;
                case DaggerfallDateTime.Seasons.Fall:
                    return HarvestSeasons.Fall;
                default:
                    return HarvestSeasons.None;
            }
        }

        /// <summary>
        /// Checks if current season is winter and player is not in a desert region.
        /// </summary>
        /// <returns>True if is winter.</returns>
        private static bool IsWinter()
        {
            return GameManager.Instance.PlayerGPS.CurrentClimateIndex != (int)MapsFile.Climates.Desert &&
                GameManager.Instance.PlayerGPS.CurrentClimateIndex != (int)MapsFile.Climates.Desert2 &&
                DaggerfallUnity.Instance.WorldTime.Now.SeasonValue == DaggerfallDateTime.Seasons.Winter;
        }

        // Helper method to check if current climate is a desert
        private static bool IsInDesert()
        {
            int climateIndex = GameManager.Instance.PlayerGPS.CurrentClimateIndex;
            return climateIndex == (int)MapsFile.Climates.Desert || climateIndex == (int)MapsFile.Climates.Desert2;
        }

        #endregion

        #region Events

        private void DETHarvestableCrops_OnRestoreSaveData()
        {
            RefreshHarvestedState();
        }

        #endregion
    }
}
