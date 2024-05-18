using ECommons.DalamudServices;
using Lumina.Excel;
using Lumina.Excel.GeneratedSheets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UntitledGatheringPlugin.GameData
{
    internal static class GameDataHelper
    {
        public static GatheringPoint GetGatheringPoint(uint id)
        {
            return Svc.Data.GetExcelSheet<GatheringPoint>().GetRow(id);
        }

        public static List<string> GetItemNamesFromGatheringPoint(uint dataId)
        {
            List<string> returnVal = new List<string>();
            var gp = GetGatheringPoint(dataId);
            if (gp == null) return returnVal;

            var items = gp.GatheringPointBase.Value.Item;
            foreach (var item in items)
            {
                if (item <= 0) continue;
                var itemId = Svc.Data.GetExcelSheet<GatheringItem>().GetRow((uint)item).Item;
                if (itemId <= 0) continue;
                var itemName = Svc.Data.GetExcelSheet<Item>().GetRow((uint)itemId).Name;
                returnVal.Add(itemName);
            }
            return returnVal;
        }
    }
}
