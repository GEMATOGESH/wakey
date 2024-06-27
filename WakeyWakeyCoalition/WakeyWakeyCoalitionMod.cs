using HarmonyLib;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Server;
using Vintagestory.Common;
using Vintagestory.GameContent;


namespace WakeyWakeyCoalition
{
    public class WakeyConfig
    {
        public bool player_join_message_enabled = true;
        public string player_join_message = "ОООООООО, {0} ЗАШЕЛ";

        public bool player_bed_message_enabled = true;
        public string player_enter_bed_message = "{0} ложится спать.";
        public string player_exit_bed_message = "{0} встал с кровати.";

        public bool wakey_message_enabled = true;
        public string wakey_message = "ПРОСНУЛИСЬ))))))УЛЫБНУЛИСЬ)00)))0";

        public bool broadcast_message_enabled = true;
        public string broadcast_message = "А вы не забыли попить водички из своей верной фляжки Убежища 13?";
        public int broadcast_delay_ms = 10000;

        public bool bear_death_message_enabled = true;
    }

    [HarmonyPatch]
    public class WakeyWakeyCoalitionMod : ModSystem
    {
        static ICoreAPI? api;
        static ICoreServerAPI? server_api;

        static Harmony? harmony;
        static WakeyConfig? config;

        public override void StartServerSide(ICoreServerAPI api)
        {
            base.Start(api);

            server_api = api;

            config = server_api.LoadModConfig<WakeyConfig>("WakeyCoalition.json");

            if (config == null)
            {
                config = new WakeyConfig();

                server_api.StoreModConfig<WakeyConfig>(config, "WakeyCoalition.json");

                server_api.Logger.Event("[WakeyCoalition] Created config.");
            }
            else
            {
                server_api.Logger.Event("[WakeyCoalition] Loaded config.");
            }

            if (config.player_join_message_enabled)
            {
                server_api.Event.PlayerJoin += PlayerJoin;
            }

            if (config.broadcast_message_enabled)
            {
                server_api.Event.RegisterGameTickListener(Canteen, config.broadcast_delay_ms);
            }

            harmony = new Harmony("Coaliton.WakeyCoalition");
            harmony.PatchAll();
        }

        public override void Start(ICoreAPI api_)
        {
            base.Start(api_);

            api = api_;

            api.RegisterBlockEntityClass("EntityCustomFirepit", typeof(EntityCustomFirepit));
            api.RegisterBlockClass("BlockCustomFirepit", typeof(BlockCustomFirepit));
        }

        public override void Dispose()
        {
            base.Dispose();
            harmony.UnpatchAll();
        }

        public void PlayerJoin(IServerPlayer byPlayer)
        {
            server_api.BroadcastMessageToAllGroups(config.player_join_message.Replace("{0}", byPlayer.PlayerName), EnumChatType.Notification);
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(EntityAgent), "TryMount")]
        public static void PostTryMount(EntityAgent __instance, bool __result)
        {
            if (config.player_bed_message_enabled)
            {
                if (__result && __instance.MountedOn is BlockEntityBed && (__instance as EntityPlayer)?.Player is Vintagestory.Server.ServerPlayer)
                {
                    var players = server_api.World.AllOnlinePlayers.ToList();
                    int sleeping = players.Where(p => p.Entity?.MountedOn is BlockEntityBed).Count();

                    server_api.BroadcastMessageToAllGroups(config.player_enter_bed_message.Replace("{0}", (__instance as EntityPlayer)?.Player.PlayerName) + " (" +
                        sleeping.ToString() + "/" + players.Count() + ")", EnumChatType.Notification);
                }
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
            if (config.player_bed_message_enabled)
            {
                var mountedOn = __state[0];

                if (__result && mountedOn is BlockEntityBed && (__instance as EntityPlayer)?.Player is Vintagestory.Server.ServerPlayer)
                {
                    var players = server_api.World.AllOnlinePlayers.ToList();
                    int sleeping = players.Where(p => p.Entity?.MountedOn is BlockEntityBed).Count();

                    server_api.BroadcastMessageToAllGroups(config.player_exit_bed_message.Replace("{0}", (__instance as EntityPlayer)?.Player.PlayerName) + " (" +
                        sleeping.ToString() + "/" + players.Count() + ")", EnumChatType.Notification);
                }
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
            if (config.wakey_message_enabled)
            {
                bool state = (bool)__state[0];
                EntityPlayer player = (EntityPlayer)__state[1];

                if (state != __instance.MountedBy?.GetBehavior("tiredness") is not EntityBehaviorTiredness entityBehaviorTiredness)
                {
                    // server_api.Logger.Event("[Solyanka] Wakey " + player.PlayerUID);
                    server_api.SendMessage(player.Player, GlobalConstants.GeneralChatGroup, config.wakey_message, EnumChatType.OthersMessage);
                }
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(EntityPlayer), "Die")]
        public static void PreDie(EntityPlayer __instance, ref object[] __state, EnumDespawnReason reason = EnumDespawnReason.Death, DamageSource damageSourceForDeath = null)
        {
            if (config.bear_death_message_enabled)
            {
                if (damageSourceForDeath != null && damageSourceForDeath.SourceEntity != null)
                {
                    if (reason == EnumDespawnReason.Death && damageSourceForDeath.SourceEntity.GetName().ToLower().Contains("bear"))
                    {
                        server_api.BroadcastMessageToAllGroups(__instance.Player.PlayerName + ", скажи подвал:", EnumChatType.Notification);
                    }

                }
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(Lang), "GetL")]
        public static void PreGet(ref object[] __state, ref string __result, string key)
        {
            if (config.bear_death_message_enabled)
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
        }

        public void Canteen(float dt)
        {
            server_api.BroadcastMessageToAllGroups(config.broadcast_message, EnumChatType.Notification);
        }
    }
}
