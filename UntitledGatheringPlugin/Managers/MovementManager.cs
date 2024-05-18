using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.ClientState.Objects.Types;
using ECommons.Automation.LegacyTaskManager;
using ECommons.Configuration;
using ECommons.DalamudServices;
using ECommons.GameHelpers;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using ImGuiNET;
using Lumina.Excel.GeneratedSheets;
using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace UntitledGatheringPlugin.Managers
{
    internal class MovementManager
    {
        public Vector3? CurrentDestination { get; private set; }
        public uint? TeleportDestination { get; private set; }
        public bool CanMove
        {
            get
            {
                if (CurrentDestination != null) return false;
                if (TeleportDestination != null) return false;

                return true;
            }
        }
        public MovementManager()
        {
            CurrentDestination = null;
            TeleportDestination = null;
        }

        public void AertheryteTeleport(uint aetheryte)
        {
            //if (!CanMove) return;
            TeleportDestination = aetheryte;
        }

        public void NavToObject(GameObject obj)
        {
            if (obj == null) return;

            CurrentDestination = obj.Position;
        }

        public void Update()
        {
            if (!Plugin.ObjectManager.ShouldGather) return;
            if (TeleportDestination != null && !_isTeleporting)
            {
                if (Svc.Condition[ConditionFlag.Mounted]) Dismount();
                TeleportViaAtheryte();
                return;
            }
            else if (TeleportDestination != null && AreWeAtTeleportDestination())
            {
                Svc.Log.Information("Arrived at teleport destination.");
                TeleportDestination = null;
                _isTeleporting = false;
            }
            else if (CurrentDestination == null) return;
            else if (!Svc.Condition[ConditionFlag.Mounted] && !AreWeNearDestination()) MountUp();
            else if (Svc.Condition[ConditionFlag.Mounted] && AreWeNearDestination()) Dismount();
            else if (AreWeNearDestination())
            {
                CurrentDestination = null;
                IPCManager.VNavmesh_IPCSubscriber.Path_Stop();
                return;
            }
            else if (IPCManager.VNavmesh_IPCSubscriber.Path_IsRunning()) return;
            else if (!IPCManager.VNavmesh_IPCSubscriber.Nav_IsReady()) return;
            else
            {
                IPCManager.VNavmesh_IPCSubscriber.SimpleMove_PathfindAndMoveTo(CurrentDestination.Value, true);
            }
        }

        private Vector3 _lastKnownPosition;
        private DateTime _stuckCheckTimer = DateTime.MinValue;
        private void StuckCheck()
        {
            if (DateTime.Now - _stuckCheckTimer < TimeSpan.FromSeconds(5)) return;
            var pos = Player.Object.Position;
            if (Vector3.Distance(pos, _lastKnownPosition) < 1f)
            {
                Svc.Log.Warning("Stuck detected. Reloading nav.");
                IPCManager.VNavmesh_IPCSubscriber.Nav_Reload();
            }
            _lastKnownPosition = pos;
            _stuckCheckTimer = DateTime.Now;
        }

        private bool AreWeAtTeleportDestination()
        {
            return Player.Territory == TeleportDestination;
        }

        private bool _isTeleporting = false;
        private unsafe void TeleportViaAtheryte()
        {
            if (TeleportDestination == null) return;
            if (IsAttuned(TeleportDestination.Value))
            {
                _isTeleporting = true;
                var teleport = Telepo.Instance();
                if (teleport == null)
                {
                    Svc.Log.Error("Could not teleport: Telepo is missing.");
                    return;
                }
                teleport->Teleport(TeleportDestination.Value, 0);
            }
        }

        public unsafe bool IsAttuned(uint aetheryte)
        {
            var teleport = Telepo.Instance();
            if (teleport == null)
            {
                Svc.Log.Error("Could not check attunement: Telepo is missing.");
                return false;
            }

            if (Svc.ClientState.LocalPlayer == null)
                return true;
            teleport->UpdateAetheryteList();

            var endPtr = teleport->TeleportList.Last;
            for (var it = teleport->TeleportList.First; it != endPtr; ++it)
            {
                if (it->AetheryteId == aetheryte)
                    return true;
            }

            return false;
        }

        private unsafe void Dismount()
        {
            var am = ActionManager.Instance();
            am->UseAction(ActionType.Mount, 0);
        }

        private bool AreWeNearDestination()
        {
            if (CurrentDestination == null) return true;
            var pos = Player.Object.Position;
            var dest = CurrentDestination.Value;

            var distance = Vector3.Distance(pos, dest) - Player.Object.HitboxRadius;
            return distance < 3f;
        }

        private unsafe uint GetMountId()
        {
            var ps = PlayerState.Instance();
            var mounts = Svc.Data.GetExcelSheet<Mount>();
            if (mounts == null) return 0;
            foreach (var mount in mounts)
            {
                if (ps->IsMountUnlocked(mount.RowId))
                {
                    return mount.RowId;
                }
            }
            return 0;
        }

        private unsafe void MountUp()
        {
            var am = ActionManager.Instance();
            var mount = GetMountId();
            if (am->GetActionStatus(ActionType.Mount, mount) != 0) return;
            am->UseAction(ActionType.Mount, mount);
        }
    }
}
