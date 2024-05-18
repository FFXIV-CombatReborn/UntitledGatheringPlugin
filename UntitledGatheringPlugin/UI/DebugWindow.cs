using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Interface.Windowing;
using ECommons.GameHelpers;
using ECommons.Reflection;
using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using UntitledGatheringPlugin.GameData;
using UntitledGatheringPlugin.Managers;

namespace UntitledGatheringPlugin.UI
{
    internal class DebugWindow : Window
    {
        private const ImGuiWindowFlags BaseFlags = ImGuiWindowFlags.None;
        public DebugWindow() : base("UGP Debug", BaseFlags)
        {
            Size = new System.Numerics.Vector2(400, 400);
            SizeCondition = ImGuiCond.FirstUseEver;
        }
        private int _insertItemId = 5380;
        private string _itemName = "";
        private int _quantity = 1;
        public override void Draw()
        {
            ImGui.Text($"VNavMesh Enabled: {IPCManager.VNavmesh_IPCSubscriber.IsEnabled}");
            var shouldGather = Plugin.ObjectManager.ShouldGather;
            if (ImGui.Checkbox("Should Auto-Gather", ref shouldGather))
            {
                Plugin.ObjectManager.ShouldGather = shouldGather;
            }
            var minDurability = Plugin.Config.MinDurability;
            if (ImGui.DragFloat("Min Durability", ref minDurability, 1, 0, 100))
            {
                Plugin.Config.MinDurability = minDurability;
                Plugin.Config.Save();
            }
            ImGui.InputInt("Item ID", ref _insertItemId);
            ImGui.InputText("Item Name (Singular as shown in chat window)", ref _itemName, 100);
            ImGui.InputInt("Quantity", ref _quantity);
            if (ImGui.Button("Add Item By ID"))
            {
                if (_insertItemId <= 0) return;
                if (_quantity <= 0) return;
                var item = new GatherItem((uint)_insertItemId, (uint)_quantity);
                Plugin.ObjectManager.ShoppingList.Add(item);
            }
            ImGui.SameLine();
            if (ImGui.Button("Add Item By Name"))
            {
                if (string.IsNullOrEmpty(_itemName)) return;
                if (_quantity <= 0) return;
                var item = new GatherItem(_itemName, (uint)_quantity);
                Plugin.ObjectManager.ShoppingList.Add(item);
            }
            ImGui.Text("Shopping List:");
            var shoppingList = Plugin.ObjectManager.ShoppingList;
            if (shoppingList.Count == 0)
            {
                ImGui.Text("None");
            }
            else
            {
                foreach (var item in shoppingList)
                {
                    ImGui.Text($"{item.Name} x{item.Quantity}");
                }
            }
            ImGui.Separator();
            ImGui.Text("Gatherables Detected:");
            var gatherables = Plugin.ObjectManager.Gatherables;
            if (gatherables.Count == 0)
            {
                ImGui.Text("None");
            }
            else
            {
                foreach (var gatherable in gatherables)
                {
                    ImGui.PushID(gatherable.ObjectId.ToString()); // Push unique ID for the outer gatherable

                    if (ImGui.TreeNodeEx($"Name: {gatherable.Name}"))
                    {
                        ImGui.Text($"ID: {gatherable.ObjectId}");
                        ImGui.Text($"Position: {gatherable.Position}");
                        ImGui.Text($"Targetable: {gatherable.IsTargetable}");
                        ImGui.Text($"DataID: {gatherable.DataId}");
                        ImGui.Text($"Distance = {Vector3.Distance(Player.Object.Position, gatherable.Position)}");

                        if (ImGui.Button($"Navigate##{gatherable.ObjectId}")) // Ensure the button has a unique ID
                        {
                            Plugin.MovementManager.NavToObject(gatherable);
                        }

                        ImGui.PushID("GatherableItems"); // Push another unique ID scope for the nested tree

                        if (ImGui.TreeNodeEx("Gatherable Items"))
                        {
                            var gatherableData = GameDataHelper.GetItemNamesFromGatheringPoint(gatherable.DataId);
                            foreach (var item in gatherableData)
                            {
                                ImGui.Text(item);
                            }
                            ImGui.TreePop(); // Close the nested tree node
                        }

                        ImGui.PopID(); // Pop the nested ID scope
                        ImGui.TreePop(); // Close the outer tree node
                    }

                    ImGui.PopID(); // Pop the outer ID scope
                }
            }

        }
    }
}
