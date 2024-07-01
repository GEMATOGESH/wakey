using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Client;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace WakeyWakeyCoalition
{
    class BlockCustomFirepit : Block, IIgnitable
    {
        public int Stage
        {
            get
            {
                switch (LastCodePart())
                {
                    case "construct1":
                        return 1;
                    case "construct2":
                        return 2;
                    case "construct3":
                        return 3;
                    case "construct4":
                        return 4;
                }
                return 5;
            }
        }

        public string NextStageCodePart
        {
            get
            {
                switch (LastCodePart())
                {
                    case "construct1":
                        return "construct2";
                    case "construct2":
                        return "construct3";
                    case "construct3":
                        return "construct4";
                    case "construct4":
                        return "cold";
                }
                return "cold";
            }
        }


        public bool IsExtinct;

        AdvancedParticleProperties[] ringParticles;
        Vec3f[] basePos;
        WorldInteraction[] interactions;

        public override void OnLoaded(ICoreAPI api)
        {
            base.OnLoaded(api);

            IsExtinct = LastCodePart() != "lit";

            if (!IsExtinct && api.Side == EnumAppSide.Client)
            {
                ringParticles = new AdvancedParticleProperties[this.ParticleProperties.Length * 4];
                basePos = new Vec3f[ringParticles.Length];

                Cuboidf[] spawnBoxes = new Cuboidf[]
                {
                    new Cuboidf(x1: 0.125f, y1: 0, z1: 0.125f, x2: 0.3125f, y2: 0.5f, z2: 0.875f),
                    new Cuboidf(x1: 0.7125f, y1: 0, z1: 0.125f, x2: 0.875f, y2: 0.5f, z2: 0.875f),
                    new Cuboidf(x1: 0.125f, y1: 0, z1: 0.125f, x2: 0.875f, y2: 0.5f, z2: 0.3125f),
                    new Cuboidf(x1: 0.125f, y1: 0, z1: 0.7125f, x2: 0.875f, y2: 0.5f, z2: 0.875f)
                };

                for (int i = 0; i < ParticleProperties.Length; i++)
                {
                    for (int j = 0; j < 4; j++)
                    {
                        AdvancedParticleProperties props = ParticleProperties[i].Clone();

                        Cuboidf box = spawnBoxes[j];
                        basePos[i * 4 + j] = new Vec3f(0, 0, 0);

                        props.PosOffset[0].avg = box.MidX;
                        props.PosOffset[0].var = box.Width / 2;

                        props.PosOffset[1].avg = 0.1f;
                        props.PosOffset[1].var = 0.05f;

                        props.PosOffset[2].avg = box.MidZ;
                        props.PosOffset[2].var = box.Length / 2;

                        props.Quantity.avg /= 4f;
                        props.Quantity.var /= 4f;

                        ringParticles[i * 4 + j] = props;
                    }
                }
            }


            interactions = ObjectCacheUtil.GetOrCreate(api, "firepitInteractions-" + Stage, () =>
            {
                List<ItemStack> canIgniteStacks = BlockBehaviorCanIgnite.CanIgniteStacks(api, true);

                return new WorldInteraction[]
                {
                    new WorldInteraction()
                    {
                        ActionLangCode = "blockhelp-firepit-open",
                        MouseButton = EnumMouseButton.Right,
                        ShouldApply = (WorldInteraction wi, BlockSelection blockSelection, EntitySelection entitySelection) =>
                        {
                            return Stage == 5;
                        }
                    },
                    new WorldInteraction()
                    {
                        ActionLangCode = "blockhelp-firepit-ignite",
                        MouseButton = EnumMouseButton.Right,
                        HotKeyCode = "shift",
                        Itemstacks = canIgniteStacks.ToArray(),
                        GetMatchingStacks = (wi, bs, es) => {
                            EntityCustomFirepit bef = api.World.BlockAccessor.GetBlockEntity(bs.Position) as EntityCustomFirepit;
                            if (bef?.fuelSlot != null && !bef.fuelSlot.Empty && !bef.IsBurning)
                            {
                                return wi.Itemstacks;
                            }
                            return null;
                        }
                    },
                    new WorldInteraction()
                    {
                        ActionLangCode = "blockhelp-firepit-refuel",
                        MouseButton = EnumMouseButton.Right,
                        HotKeyCode = "shift"
                    }
                };
            });
        }


        public override void OnEntityInside(IWorldAccessor world, Entity entity, BlockPos pos)
        {
            if (world.Rand.NextDouble() < 0.05 && GetBlockEntity<EntityCustomFirepit>(pos)?.IsBurning == true)
            {
                entity.ReceiveDamage(new DamageSource() { Source = EnumDamageSource.Block, SourceBlock = this, Type = EnumDamageType.Fire, SourcePos = pos.ToVec3d() }, 0.5f);
            }

            base.OnEntityInside(world, entity, pos);
        }


        EnumIgniteState IIgnitable.OnTryIgniteStack(EntityAgent byEntity, BlockPos pos, ItemSlot slot, float secondsIgniting)
        {
            EntityCustomFirepit bef = api.World.BlockAccessor.GetBlockEntity(pos) as EntityCustomFirepit;
            if (bef.IsBurning) return secondsIgniting > 2 ? EnumIgniteState.IgniteNow : EnumIgniteState.Ignitable;
            return EnumIgniteState.NotIgnitable;
        }
        public EnumIgniteState OnTryIgniteBlock(EntityAgent byEntity, BlockPos pos, float secondsIgniting)
        {
            EntityCustomFirepit bef = api.World.BlockAccessor.GetBlockEntity(pos) as EntityCustomFirepit;
            if (bef == null) return EnumIgniteState.NotIgnitable;
            return bef.GetIgnitableState(secondsIgniting);
        }

        public void OnTryIgniteBlockOver(EntityAgent byEntity, BlockPos pos, float secondsIgniting, ref EnumHandling handling)
        {
            EntityCustomFirepit bef = api.World.BlockAccessor.GetBlockEntity(pos) as EntityCustomFirepit;
            if (bef != null && !bef.canIgniteFuel)
            {
                bef.canIgniteFuel = true;
                bef.extinguishedTotalHours = api.World.Calendar.TotalHours;
            }

            handling = EnumHandling.PreventDefault;
        }


        public override bool ShouldReceiveClientParticleTicks(IWorldAccessor world, IPlayer player, BlockPos pos, out bool isWindAffected)
        {
            bool val = base.ShouldReceiveClientParticleTicks(world, player, pos, out _);
            isWindAffected = true;

            return val;
        }

        public override void OnAsyncClientParticleTick(IAsyncParticleManager manager, BlockPos pos, float windAffectednessAtPos, float secondsTicking)
        {
            if (IsExtinct)
            {
                base.OnAsyncClientParticleTick(manager, pos, windAffectednessAtPos, secondsTicking);
                return;
            }

            EntityCustomFirepit bef = manager.BlockAccess.GetBlockEntity(pos) as EntityCustomFirepit;
            if (bef != null && bef.CurrentModel == EnumFirepitModel.Wide)
            {
                for (int i = 0; i < ringParticles.Length; i++)
                {
                    AdvancedParticleProperties bps = ringParticles[i];
                    bps.WindAffectednesAtPos = windAffectednessAtPos;
                    bps.basePos.X = pos.X + basePos[i].X;
                    bps.basePos.Y = pos.Y + basePos[i].Y;
                    bps.basePos.Z = pos.Z + basePos[i].Z;

                    manager.Spawn(bps);
                }

                return;
            }

            base.OnAsyncClientParticleTick(manager, pos, windAffectednessAtPos, secondsTicking);
        }

        public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            EntityCustomFirepit bef = world.BlockAccessor.GetBlockEntity(blockSel.Position) as EntityCustomFirepit;
            bef.OnPlayerRightClick(byPlayer, blockSel);
            return true;
        }
        //public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        //{
        //    api.Logger.Event("[WakeyCoalition] Pit block interaction.");

        //    if (blockSel != null && !world.Claims.TryAccess(byPlayer, blockSel.Position, EnumBlockAccessFlags.Use))
        //    {
        //        return false;
        //    }


        //    int stage = Stage;
        //    ItemStack stack = byPlayer.InventoryManager.ActiveHotbarSlot?.Itemstack;
        //    api.Logger.Event("[WakeyCoalition] -----------------------");
        //    api.Logger.Event("[WakeyCoalition] ID: " + stack?.Id);
        //    api.Logger.Event("[WakeyCoalition] Stage: " + stage.ToString());
        //    api.Logger.Event("[WakeyCoalition] Temperature: " + stack?.Collectible.CombustibleProps.BurnTemperature.ToString());

        //    if (stage == 5)
        //    {
        //        EntityCustomFirepit bef = world.BlockAccessor.GetBlockEntity(blockSel.Position) as EntityCustomFirepit;

        //        if (bef != null && stack?.Block != null && stack.Block.HasBehavior<BlockBehaviorCanIgnite>() && bef.GetIgnitableState(0) == EnumIgniteState.Ignitable)
        //        {
        //            api.Logger.Event("[WakeyCoalition] Return 1.");
        //            return false;
        //        }


        //        api.Logger.Event("[WakeyCoalition] Shifting: " + byPlayer.Entity.Controls.ShiftKey.ToString()); // FALSE!!!!! 
        //        api.Logger.Event("[WakeyCoalition] Entity: " + byPlayer.Entity.ToString());
        //        if (bef != null && stack != null && byPlayer.Entity.Controls.ShiftKey)
        //        {
        //            if (stack.Collectible.CombustibleProps != null && stack.Collectible.CombustibleProps.MeltingPoint > 0)
        //            {
        //                ItemStackMoveOperation op = new ItemStackMoveOperation(world, EnumMouseButton.Left, 0, EnumMergePriority.DirectMerge, 1);
        //                byPlayer.InventoryManager.ActiveHotbarSlot.TryPutInto(bef.inputSlot, ref op);
        //                if (op.MovedQuantity > 0)
        //                {
        //                    (byPlayer as IClientPlayer)?.TriggerFpAnimation(EnumHandInteract.HeldItemInteract);
        //                    api.Logger.Event("[WakeyCoalition] Added ingridient.");
        //                    return true;
        //                }
        //            }


        //            if (stack.Collectible.CombustibleProps != null && stack.Collectible.CombustibleProps.BurnTemperature > 0)
        //            {
        //                ItemStackMoveOperation op = new ItemStackMoveOperation(world, EnumMouseButton.Left, 0, EnumMergePriority.DirectMerge, 1);
        //                byPlayer.InventoryManager.ActiveHotbarSlot.TryPutInto(bef.fuelSlot, ref op);
        //                if (op.MovedQuantity > 0)
        //                {
        //                    (byPlayer as IClientPlayer)?.TriggerFpAnimation(EnumHandInteract.HeldItemInteract);

        //                    var loc = stack.ItemAttributes?["placeSound"].Exists == true ? AssetLocation.Create(stack.ItemAttributes["placeSound"].AsString(), stack.Collectible.Code.Domain) : null;

        //                    if (loc != null)
        //                    {
        //                        api.World.PlaySoundAt(loc.WithPathPrefixOnce("sounds/"), blockSel.Position.X, blockSel.Position.Y, blockSel.Position.Z, byPlayer, 0.88f + (float)api.World.Rand.NextDouble() * 0.24f, 16);
        //                    }

        //                    api.Logger.Event("[WakeyCoalition] Added fuel.");
        //                    return true;
        //                }
        //            }

        //        }

        //        if (stack?.Collectible.Attributes?.IsTrue("mealContainer") == true)
        //        {
        //            ItemSlot potSlot = null;
        //            if (bef?.inputStack?.Collectible is BlockCookedContainer)
        //            {
        //                potSlot = bef.inputSlot;
        //            }
        //            if (bef?.outputStack?.Collectible is BlockCookedContainer)
        //            {
        //                potSlot = bef.outputSlot;
        //            }

        //            if (potSlot != null)
        //            {
        //                BlockCookedContainer blockPot = potSlot.Itemstack.Collectible as BlockCookedContainer;
        //                ItemSlot targetSlot = byPlayer.InventoryManager.ActiveHotbarSlot;
        //                if (byPlayer.InventoryManager.ActiveHotbarSlot.StackSize > 1)
        //                {
        //                    targetSlot = new DummySlot(targetSlot.TakeOut(1));
        //                    byPlayer.InventoryManager.ActiveHotbarSlot.MarkDirty();
        //                    blockPot.ServeIntoStack(targetSlot, potSlot, world);
        //                    if (!byPlayer.InventoryManager.TryGiveItemstack(targetSlot.Itemstack, true))
        //                    {
        //                        world.SpawnItemEntity(targetSlot.Itemstack, byPlayer.Entity.ServerPos.XYZ);
        //                    }
        //                }
        //                else
        //                {
        //                    blockPot.ServeIntoStack(targetSlot, potSlot, world);
        //                }

        //            }
        //            else
        //            {
        //                if (!bef.inputSlot.Empty || byPlayer.InventoryManager.ActiveHotbarSlot.TryPutInto(api.World, bef.inputSlot, 1) == 0)
        //                {
        //                    bef.OnPlayerRightClick(byPlayer, blockSel);
        //                }
        //            }

        //            api.Logger.Event("[WakeyCoalition] Meal container interaction.");
        //            return true;
        //        }

        //        api.Logger.Event("[WakeyCoalition] Open GUI.");
        //        bef.OnPlayerRightClick(byPlayer, blockSel);
        //        return true;
        //    }

        //    api.Logger.Event("[WakeyCoalition] Nothing happens.");
        //    return false;
        //}

        public override WorldInteraction[] GetPlacedBlockInteractionHelp(IWorldAccessor world, BlockSelection selection, IPlayer forPlayer)
        {
            return interactions.Append(base.GetPlacedBlockInteractionHelp(world, selection, forPlayer));
        }
    }
}
