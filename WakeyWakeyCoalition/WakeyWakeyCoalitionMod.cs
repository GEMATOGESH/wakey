using HarmonyLib;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Server;
using Vintagestory.Common;
using Vintagestory.GameContent;


namespace WakeyWakeyCoalition
{
    [HarmonyPatch]
    public class WakeyWakeyCoalitionMod : ModSystem
    {
        static ICoreAPI api;
        static ICoreServerAPI server_api;

        static Harmony harmony;

        public override void StartServerSide(ICoreServerAPI api)
        {
            base.Start(api);

            server_api = api;

            server_api.Event.PlayerJoin += PlayerJoin;

            harmony = new Harmony("Coaliton.Solyanka");
            harmony.PatchAll();

            server_api.Event.RegisterGameTickListener(Canteen, 10000);
        }

        public override void Start(ICoreAPI api_)
        {
            base.Start(api_);

            api = api_;
        }

        public override void Dispose()
        {
            base.Dispose();
            harmony.UnpatchAll();
        }

        public void PlayerJoin(IServerPlayer byPlayer)
        {
            server_api.BroadcastMessageToAllGroups("ОООООООО, " + byPlayer.PlayerName + " ЗАШЕЛ", EnumChatType.Notification);
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(EntityAgent), "TryMount")]
        public static void PostTryMount(EntityAgent __instance, bool __result)
        {
            if (__result && __instance.MountedOn is BlockEntityBed && (__instance as EntityPlayer)?.Player is Vintagestory.Server.ServerPlayer)
            {
                var players = server_api.World.AllOnlinePlayers.ToList();
                int sleeping = players.Where(p => p.Entity?.MountedOn is BlockEntityBed).Count();

                server_api.BroadcastMessageToAllGroups((__instance as EntityPlayer)?.Player.PlayerName + " ложится спать. (" + 
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

                server_api.BroadcastMessageToAllGroups((__instance as EntityPlayer)?.Player.PlayerName + " встал с кровати. (" +
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
                // server_api.Logger.Event("[Coalition] Wakey " + player.PlayerUID);
                server_api.SendMessage(player.Player, GlobalConstants.GeneralChatGroup, "ПРОСНУЛИСЬ))))))УЛЫБНУЛИСЬ)00)))0", EnumChatType.OthersMessage);
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(EntityPlayer), "Die")]
        public static void PreDie(EntityPlayer __instance, ref object[] __state, EnumDespawnReason reason = EnumDespawnReason.Death, DamageSource damageSourceForDeath = null)
        {
            if (damageSourceForDeath != null && damageSourceForDeath.SourceEntity != null)
            {
                if (reason == EnumDespawnReason.Death && damageSourceForDeath.SourceEntity.GetName().ToLower().Contains("bear"))
                {
                    server_api.BroadcastMessageToAllGroups(__instance.Player.PlayerName + ", скажи подвал:", EnumChatType.Notification);
                }

            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(Lang), "GetL")]
        public static void PreGet(ref object[] __state, ref string __result, string key)
        {
            var bears = new List<string>() { "deathmsg-bearmaleblack-1deathmsg-bearmaleblack-1", "deathmsg-bearmaleblack-1", "deathmsg-bearmaleblack-2", "deathmsg-bearmaleblack-3", 
                "deathmsg-bearmalebrown-1", "deathmsg-bearmalebrown-2", "deathmsg-bearmalebrown-3", "deathmsg-bearmalepolar-1", "deathmsg-bearmalepolar-2", "deathmsg-bearmalepolar-3", 
                "deathmsg-bearmaleblack-1", "deathmsg-bearmaleblack-2", "deathmsg-bearmaleblack-3", "deathmsg-bearmalebrown-1", "deathmsg-bearmalebrown-2", "deathmsg-bearmalebrown-3", 
                "deathmsg-bearmalepolar-1", "deathmsg-bearmalepolar-2", "deathmsg-bearmalepolar-3", "deathmsg-bearfemaleblack-1", "deathmsg-bearfemaleblack-2", "deathmsg-bearfemaleblack-3",
                "deathmsg-bearfemalebrown-1", "deathmsg-bearfemalebrown-2", "deathmsg-bearfemalebrown-3", "deathmsg-bearfemalepolar-1", "deathmsg-bearfemalepolar-2", 
                "deathmsg-bearfemalepolar-3", "deathmsg-bearfemaleblack-1", "deathmsg-bearfemaleblack-2", "deathmsg-bearfemaleblack-3", "deathmsg-bearfemalebrown-1",
                "deathmsg-bearfemalebrown-2", "deathmsg-bearfemalebrown-3", "deathmsg-bearfemalepolar-1", "deathmsg-bearfemalepolar-2", "deathmsg-bearfemalepolar-3" };

            if (bears.Contains(key))
            {
                __result += "\nТебя медведь поцеловал.";
            }
        }

        public void Canteen(float dt)
        {
            server_api.BroadcastMessageToAllGroups("А вы не забыли попить водички из своей верной фляжки Убежища 13?", EnumChatType.Notification);
        }
    }
}
