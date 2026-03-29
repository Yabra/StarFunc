using System;

namespace StarFunc.Data
{
    [Serializable]
    public struct ShopItem
    {
        public string ItemId;
        public string Category;
        public int Price;
        [UnityEngine.TextArea] public string Description;
    }
}
