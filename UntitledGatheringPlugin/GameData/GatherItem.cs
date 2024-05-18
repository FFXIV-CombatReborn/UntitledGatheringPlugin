using ECommons.DalamudServices;
using Lumina.Excel.GeneratedSheets;

namespace UntitledGatheringPlugin.GameData
{
    public class GatherItem
    {
        public string Name { get; private set; }
        public uint ItemId { get; private set; }
        public List<GatheringPoint> GatheringPointIds { get; private set; }
        public uint Quantity { get; private set; }
        public GatherItem(uint itemId, uint quantity = 1)
        {
            var itemRow = Svc.Data.GetExcelSheet<Item>().GetRow(itemId);
            if (itemRow == null)
            {
                Svc.Log.Error($"Item {itemId} not found.");
                return;
            }
            Name = itemRow.Name;
            ItemId = itemId;
            Quantity = quantity;
            GatheringPointIds = GetGatheringPointsForItem(itemId);
        }

        public GatherItem(string name, uint quantity = 1)
        {
            var itemRow = Svc.Data.GetExcelSheet<Item>().Where(i => i.Singular.RawString.Contains(name)).FirstOrDefault();
            if (itemRow == null)
            {
                Svc.Log.Error($"Item {name} not found.");
                return;
            }
            Name = name;
            ItemId = itemRow.RowId;
            Quantity = quantity;
            GatheringPointIds = GetGatheringPointsForItem(ItemId);
        }

        private List<GatheringPoint> GetGatheringPointsForItem(uint itemId)
        {
            List<GatheringPoint> gatheringPoints = new List<GatheringPoint>();
            var gpItemRow = Svc.Data.GetExcelSheet<GatheringItem>().Where(g => g.Item == itemId).FirstOrDefault();
            if (gpItemRow == null)
            {
                Svc.Log.Warning($"{Name} was not found in the GatheringItem table");
                return gatheringPoints;
            }
            var gpRows = Svc.Data.GetExcelSheet<GatheringPoint>().Where(g => g.GatheringPointBase.Value.Item.Contains((int)gpItemRow.RowId)).ToList();
            if (gpRows == null || gpRows.Count == 0)
            {
                Svc.Log.Warning($"No GatheringPoints found for item {Name}");
                return gatheringPoints;
            }
            foreach (var gpRow in gpRows)
            {
                gatheringPoints.Add(gpRow);
            }
            return gatheringPoints;
        }
    }
}