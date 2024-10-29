using DaggerfallWorkshop.Game.Items;
using DaggerfallWorkshop.Game.Serialization;

namespace HarvestableCrops
{
    public class ItemCherries : DaggerfallUnityItem
    {
        public const int templateIndex = 750;

        public ItemCherries() : base(ItemGroups.UselessItems1, templateIndex) { }

        public override bool IsStackable() => true;

        public override ItemData_v1 GetSaveData()
        {
            ItemData_v1 data = base.GetSaveData();
            data.className = typeof(ItemCherries).ToString();
            return data;
        }
    }

    public class ItemPear : DaggerfallUnityItem
    {
        public const int templateIndex = 751;

        public ItemPear() : base(ItemGroups.UselessItems1, templateIndex) { }

        public override bool IsStackable() => true;

        public override ItemData_v1 GetSaveData()
        {
            ItemData_v1 data = base.GetSaveData();
            data.className = typeof(ItemPear).ToString();
            return data;
        }
    }

    public class ItemPlum : DaggerfallUnityItem
    {
        public const int templateIndex = 752;

        public ItemPlum() : base(ItemGroups.UselessItems1, templateIndex) { }

        public override bool IsStackable() => true;

        public override ItemData_v1 GetSaveData()
        {
            ItemData_v1 data = base.GetSaveData();
            data.className = typeof(ItemPlum).ToString();
            return data;
        }
    }

    public class ItemPeach : DaggerfallUnityItem
    {
        public const int templateIndex = 753;

        public ItemPeach() : base(ItemGroups.UselessItems1, templateIndex) { }

        public override bool IsStackable() => true;

        public override ItemData_v1 GetSaveData()
        {
            ItemData_v1 data = base.GetSaveData();
            data.className = typeof(ItemPeach).ToString();
            return data;
        }
    }

    public class ItemOlives : DaggerfallUnityItem
    {
        public const int templateIndex = 754;

        public ItemOlives() : base(ItemGroups.UselessItems1, templateIndex) { }

        public override bool IsStackable() => true;

        public override ItemData_v1 GetSaveData()
        {
            ItemData_v1 data = base.GetSaveData();
            data.className = typeof(ItemOlives).ToString();
            return data;
        }
    }

    public class ItemRedGrapes : DaggerfallUnityItem
    {
        public const int templateIndex = 755;

        public ItemRedGrapes() : base(ItemGroups.UselessItems1, templateIndex) { }

        public override bool IsStackable() => true;

        public override ItemData_v1 GetSaveData()
        {
            ItemData_v1 data = base.GetSaveData();
            data.className = typeof(ItemRedGrapes).ToString();
            return data;
        }
    }

    public class ItemWhiteGrapes : DaggerfallUnityItem
    {
        public const int templateIndex = 756;

        public ItemWhiteGrapes() : base(ItemGroups.UselessItems1, templateIndex) { }

        public override bool IsStackable() => true;

        public override ItemData_v1 GetSaveData()
        {
            ItemData_v1 data = base.GetSaveData();
            data.className = typeof(ItemWhiteGrapes).ToString();
            return data;
        }
    }

    public class ItemCabbageHead : DaggerfallUnityItem
    {
        public const int templateIndex = 757;

        public ItemCabbageHead() : base(ItemGroups.UselessItems1, templateIndex) { }

        public override bool IsStackable() => true;

        public override ItemData_v1 GetSaveData()
        {
            ItemData_v1 data = base.GetSaveData();
            data.className = typeof(ItemCabbageHead).ToString();
            return data;
        }
    }

    public class ItemBundleOfWheat : DaggerfallUnityItem
    {
        public const int templateIndex = 758;

        public ItemBundleOfWheat() : base(ItemGroups.UselessItems1, templateIndex) { }

        public override bool IsStackable() => true;

        public override ItemData_v1 GetSaveData()
        {
            ItemData_v1 data = base.GetSaveData();
            data.className = typeof(ItemBundleOfWheat).ToString();
            return data;
        }
    }

    public class ItemGrain : DaggerfallUnityItem
    {
        public const int templateIndex = 759;

        public ItemGrain() : base(ItemGroups.UselessItems1, templateIndex) { }

        public override bool IsStackable() => true;

        public override ItemData_v1 GetSaveData()
        {
            ItemData_v1 data = base.GetSaveData();
            data.className = typeof(ItemGrain).ToString();
            return data;
        }
    }
}

