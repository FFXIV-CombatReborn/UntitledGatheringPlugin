using Dalamud.Game.ClientState.Objects.Types;
using ECommons.DalamudServices;
using ECommons.GameHelpers;
using FFXIVClientStructs.FFXIV.Client.Game;
using Lumina.Excel.GeneratedSheets2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using UntitledGatheringPlugin.GameData;

namespace UntitledGatheringPlugin.Managers
{
    internal class ObjectManager
    {
        public List<GameObject> Gatherables => Svc.Objects.Where(o => o.ObjectKind == Dalamud.Game.ClientState.Objects.Enums.ObjectKind.GatheringPoint && o.IsTargetable).ToList();
        public List<GatherItem> ShoppingList { get; set; } = new List<GatherItem>();
        public bool ShouldGather { get; set; } = false;
        public ObjectManager()
        {
        }

        public void Update()
        {
            if (!ShouldGather) return;

            CheckInventoryCount();
            CheckDurability();

            if (ShoppingList.Count == 0)
            {
                Svc.Log.Warning("Shopping list is empty, stopping gathering.");
                ShouldGather = false;
                return;
            }
            ProcessGatherItem(ShoppingList[0]);

        }

        private unsafe void ProcessGatherItem(GatherItem gatheritem)
        {
            if (!HasGatheringPointIds(gatheritem)) return;

            var territoryCurrent = Svc.ClientState.TerritoryType;
            var territoryId = gatheritem.GatheringPointIds.Select(g => g.TerritoryType.Row);
            var territory = Svc.Data.GetExcelSheet<TerritoryType>().GetRow(territoryId.Skip(1).FirstOrDefault());
            if (territory == null)
            {
                Svc.Log.Warning($"No territory found for item: {gatheritem.Name}");
                return;
            }
            var needTeleport = territoryCurrent != territory.RowId;
            if (needTeleport)
            {
                var aetheryte = territory.Aetheryte.Row;
                Plugin.MovementManager.AertheryteTeleport(aetheryte);
                return;
            }
            else
            {
                var gatherable = FindClosestGatherable(gatheritem);
                if (gatherable == null)
                {
                    Svc.Log.Warning($"No gatherable found for item: {gatheritem.Name}");
                    return;
                }

                NavigateToGatherable(gatherable);

            }
        }

        private bool HasGatheringPointIds(GatherItem gatheritem)
        {
            if (gatheritem.GatheringPointIds.Count == 0)
            {
                Svc.Log.Warning($"No GatheringPointIds for item: {gatheritem.Name}");
                ShoppingList.Remove(gatheritem);
                return false;
            }
            return true;
        }

        private GameObject FindClosestGatherable(GatherItem gatheritem)
        {
            var gatherablesByDistance = Gatherables.OrderBy(g => Vector3.Distance(g.Position, Player.Object.Position)).ToList();
            return gatherablesByDistance.FirstOrDefault(g => gatheritem.GatheringPointIds.Any(gi => gi.RowId == g.DataId));
        }


        private bool ShouldNavigate()
        {
            return Plugin.MovementManager.CurrentDestination == null && Plugin.MovementManager.TeleportDestination == null;
        }

        private void NavigateToGatherable(GameObject gatherable)
        {
            Plugin.MovementManager.NavToObject(gatherable);
        }


        private unsafe void CheckDurability()
        {

            var equipedItems = InventoryManager.Instance()->GetInventoryContainer(InventoryType.EquippedItems);
            uint itemLowestCondition = 60000;
            for (int i = 0; i < 13; i++)
            {
                if (itemLowestCondition > equipedItems->Items[i].Condition)
                    itemLowestCondition = equipedItems->Items[i].Condition;
            }

            float condition = itemLowestCondition / 300f;
            if (condition <= Plugin.Config.MinDurability)
            {
                Svc.Log.Warning("Durability is low, stopping gathering.");
                ShouldGather = false;
            }
        }

        private unsafe void CheckInventoryCount()
        {
            var inventory = InventoryManager.Instance();
            if (inventory == null) return;

            var itemsToRemove = new List<GatherItem>();
            foreach (var gatheritem in ShoppingList)
            {
                var itemCount = inventory->GetInventoryItemCount(gatheritem.ItemId);

                if (itemCount >= gatheritem.Quantity)
                {
                    itemsToRemove.Add(gatheritem);
                }
            }
            ShoppingList.RemoveAll(itemsToRemove.Contains);
        }
    }
}
