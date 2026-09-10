

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using EFT.InventoryLogic;
using EFT.UI.DragAndDrop;
using Astar.Vanguard.Client.Extensions;
using Astar.Vanguard.Client.Mgrs;
using Astar.Vanguard.Client.Misc;
using Astar.Vanguard.Client.Utils;
using UnityEngine;

namespace Astar.Vanguard.Client.Datas
{
    public abstract class ItemData : BaseData
    {
        private WeakReference<Item> _itemRef;
        public Item Item => _itemRef.TryGetTarget(out var item) ? item : null;
        public List<ItemData> ItemsInContainer = null;
        public EItemType ItemType = EItemType.None;
        public Transform RootTransform => GetRootTransfrom();
        protected LootDataMgr LootDataMgr => MgrAccessor.Get<LootDataMgr>();

        public ItemData(Item item)
        {
            _itemRef = new(item);
            ItemType = ItemViewFactory.GetItemType(item.GetType());
        }

        public virtual void RefreshInteresting(McsAILeadPlayer mcsAILeadPlayer, bool unlock)
        {
            UpdateContainerInfoData();
            if (unlock)
            {
                LootDataMgr.UnlockLootingTargetRootTransform(RootTransform);
            }

            foreach (var itemData in ItemsInContainer)
            {
                if (itemData == null)
                {
                    continue;
                }

                if (itemData.Item.Id == Item.Id)
                {
                    continue;
                }

                if (this == itemData)
                {
                    continue;
                }

                if (itemData is not LootData lootData)
                {
                    continue;
                }

                if (unlock)
                {
                    LootDataMgr.UnlockLootingTarget(lootData);
                }
                lootData.Refresh(mcsAILeadPlayer);
                lootData.IsItemInContainer = true;
            }
        }

        public IEnumerator UnlockRefreshRootItemInteresting(McsAILeadPlayer mcsAILeadPlayer)
        {
            yield return null;
            try
            {
                RefreshInteresting(mcsAILeadPlayer, true);
            }
            catch (Exception e)
            {
                AstarVanguardPlugin.Logger.LogError(e);
            }
        }

        public void UpdateContainerInfoData()
        {
            ItemsInContainer = Item.GetAllDatas().ToList();
        }

        protected abstract Transform GetRootTransfrom();

        public override void Dispose()
        {
            base.Dispose();
            _itemRef = null;
            if (ItemsInContainer != null)
            {
                ItemsInContainer.Clear();
            }
            ItemsInContainer = null;
        }
    }
}