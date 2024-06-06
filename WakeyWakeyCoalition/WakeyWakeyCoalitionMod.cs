using HarmonyLib;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Server;
using Vintagestory.GameContent;


namespace WakeyWakeyCoalition
{
    [HarmonyPatch]
    public class WakeyWakeyCoalitionMod : ModSystem
    {
        static ICoreServerAPI server_api;

        static Harmony harmony;

        public override void StartServerSide(ICoreServerAPI api)
        {
            server_api = api;

            server_api.Event.PlayerJoin += PlayerJoin;

            harmony = new Harmony("Coaliton.Wakey");
            harmony.PatchAll();
        }

        public void PlayerJoin(IServerPlayer byPlayer)
        {
            server_api.BroadcastMessageToAllGroups("[Coalition] ОООООООО, " + byPlayer.PlayerName + " ЗАШЕЛ", EnumChatType.Notification);
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(EntityAgent), "TryMount")]
        public static void PostTryMount(EntityAgent __instance, bool __result)
        {
            if (__result && __instance.MountedOn is BlockEntityBed && (__instance as EntityPlayer)?.Player is Vintagestory.Server.ServerPlayer)
            {
                var players = server_api.World.AllOnlinePlayers.ToList();
                int sleeping = players.Where(p => p.Entity?.MountedOn is BlockEntityBed).Count();

                server_api.BroadcastMessageToAllGroups("[Coalition] " + (__instance as EntityPlayer)?.Player.PlayerName + " ложится спать. (" + 
                    sleeping.ToString() + "/" + players.Count() + ")", EnumChatType.Notification);
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(EntityAgent), "TryUnmount")]
        public static void PreTryUnmount(EntityAgent __instance, ref object[] __state)
        {
            __state = new object[]
            {
                __instance.MountedOn
            };
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(EntityAgent), "TryUnmount")]
        public static void PostTryUnmount(EntityAgent __instance, bool __result, object[] __state)
        {
            var mountedOn = __state[0];

            if (__result && mountedOn is BlockEntityBed && (__instance as EntityPlayer)?.Player is Vintagestory.Server.ServerPlayer)
            {
                var players = server_api.World.AllOnlinePlayers.ToList();
                int sleeping = players.Where(p => p.Entity?.MountedOn is BlockEntityBed).Count();

                server_api.BroadcastMessageToAllGroups("[Coalition] " + (__instance as EntityPlayer)?.Player.PlayerName + " встал с кровати. (" +
                    sleeping.ToString() + "/" + players.Count() + ")", EnumChatType.Notification);
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(BlockEntityBed), "RestPlayer")]
        public static void PreRestPlayer(BlockEntityBed __instance, string ___mountedByPlayerUid, ref object[] __state)
        {
            __state = new object[]
            {
                __instance.MountedBy?.GetBehavior("tiredness") is not EntityBehaviorTiredness entityBehaviorTiredness,
                (__instance.MountedBy as EntityPlayer)
            };
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(BlockEntityBed), "RestPlayer")]
        public static void PostRestPlayer(BlockEntityBed __instance, object[] __state)
        {
            bool state = (bool)__state[0];
            EntityPlayer player = (EntityPlayer)__state[1];

            if (state != __instance.MountedBy?.GetBehavior("tiredness") is not EntityBehaviorTiredness entityBehaviorTiredness)
            {
                server_api.Logger.Event("[Coalition] Wakey " + player.PlayerUID);
                server_api.SendMessage(player.Player, GlobalConstants.GeneralChatGroup, "[Coalition] ПРОСНУЛИСЬ))))))УЛЫБНУЛИСЬ)00)))0", EnumChatType.OthersMessage);
            }
        }
    }
}
