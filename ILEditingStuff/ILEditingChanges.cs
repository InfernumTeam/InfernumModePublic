using CalamityMod;
using CalamityMod.CalPlayer;
using CalamityMod.Items.SummonItems;
using CalamityMod.NPCs;
using CalamityMod.NPCs.DesertScourge;
using CalamityMod.NPCs.DevourerofGods;
using CalamityMod.NPCs.ExoMechs.Ares;
using CalamityMod.NPCs.SupremeCalamitas;
using CalamityMod.UI;
using CalamityMod.World;
using InfernumMode.BehaviorOverrides.BossAIs;
using InfernumMode.BehaviorOverrides.BossAIs.Golem;
using InfernumMode.GlobalInstances;
using InfernumMode.OverridingSystem;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour.HookGen;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent.Events;
using Terraria.Graphics;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.ILEditingStuff
{
    public class ILEditingChanges
    {
        public static List<int> DrawCacheProjsOverSignusBlackening = new List<int>(Main.maxProjectiles);
        public static List<int> DrawCacheAdditiveLighting = new List<int>(Main.maxProjectiles);
        public static event ILContext.Manipulator ModifyPreAINPC
        {
            add => HookEndpointManager.Modify(typeof(NPCLoader).GetMethod("PreAI", Utilities.UniversalBindingFlags), value);
            remove => HookEndpointManager.Unmodify(typeof(NPCLoader).GetMethod("PreAI", Utilities.UniversalBindingFlags), value);
        }

        public static event ILContext.Manipulator ModifySetDefaultsNPC
        {
            add => HookEndpointManager.Modify(typeof(NPCLoader).GetMethod("SetDefaults", Utilities.UniversalBindingFlags), value);
            remove => HookEndpointManager.Unmodify(typeof(NPCLoader).GetMethod("SetDefaults", Utilities.UniversalBindingFlags), value);
        }

        public static event ILContext.Manipulator ModifyCheckDead
        {
            add => HookEndpointManager.Modify(typeof(NPCLoader).GetMethod("CheckDead", Utilities.UniversalBindingFlags), value);
            remove => HookEndpointManager.Unmodify(typeof(NPCLoader).GetMethod("CheckDead", Utilities.UniversalBindingFlags), value);
        }

        public static event ILContext.Manipulator ModifyPreDrawNPC
        {
            add => HookEndpointManager.Modify(typeof(NPCLoader).GetMethod("PreDraw", Utilities.UniversalBindingFlags), value);
            remove => HookEndpointManager.Unmodify(typeof(NPCLoader).GetMethod("PreDraw", Utilities.UniversalBindingFlags), value);
        }

        public static event ILContext.Manipulator ModifyPreDrawProjectile
        {
            add => HookEndpointManager.Modify(typeof(ProjectileLoader).GetMethod("PreDraw", Utilities.UniversalBindingFlags), value);
            remove => HookEndpointManager.Unmodify(typeof(ProjectileLoader).GetMethod("PreDraw", Utilities.UniversalBindingFlags), value);
        }

        public static event ILContext.Manipulator ModifyFindFrameNPC
        {
            add => HookEndpointManager.Modify(typeof(NPCLoader).GetMethod("FindFrame", Utilities.UniversalBindingFlags), value);
            remove => HookEndpointManager.Unmodify(typeof(NPCLoader).GetMethod("FindFrame", Utilities.UniversalBindingFlags), value);
        }

        public static event ILContext.Manipulator ModifyPreAIProjectile
        {
            add => HookEndpointManager.Modify(typeof(ProjectileLoader).GetMethod("PreAI", Utilities.UniversalBindingFlags), value);
            remove => HookEndpointManager.Unmodify(typeof(ProjectileLoader).GetMethod("PreAI", Utilities.UniversalBindingFlags), value);
        }

        public static event ILContext.Manipulator ModifyTextUtility
        {
            add => HookEndpointManager.Modify(typeof(CalamityUtils).GetMethod("DisplayLocalizedText", Utilities.UniversalBindingFlags), value);
            remove => HookEndpointManager.Unmodify(typeof(CalamityUtils).GetMethod("DisplayLocalizedText", Utilities.UniversalBindingFlags), value);
        }

        public static event ILContext.Manipulator ModeIndicatorUIDraw
        {
            add => HookEndpointManager.Modify(typeof(ModeIndicatorUI).GetMethod("Draw", Utilities.UniversalBindingFlags), value);
            remove => HookEndpointManager.Unmodify(typeof(ModeIndicatorUI).GetMethod("Draw", Utilities.UniversalBindingFlags), value);
        }

        public static event ILContext.Manipulator CalamityWorldPostUpdate
        {
            add => HookEndpointManager.Modify(typeof(CalamityWorld).GetMethod("PostUpdate", Utilities.UniversalBindingFlags), value);
            remove => HookEndpointManager.Unmodify(typeof(CalamityWorld).GetMethod("PostUpdate", Utilities.UniversalBindingFlags), value);
        }

        public static event ILContext.Manipulator CalamityPlayerOtherBuffEffects
        {
            add => HookEndpointManager.Modify(typeof(CalamityPlayerMiscEffects).GetMethod("OtherBuffEffects", Utilities.UniversalBindingFlags), value);
            remove => HookEndpointManager.Unmodify(typeof(CalamityPlayerMiscEffects).GetMethod("OtherBuffEffects", Utilities.UniversalBindingFlags), value);
        }

        public static event ILContext.Manipulator CalamityNPCLifeRegen
        {
            add => HookEndpointManager.Modify(typeof(CalamityGlobalNPC).GetMethod("UpdateLifeRegen", Utilities.UniversalBindingFlags), value);
            remove => HookEndpointManager.Unmodify(typeof(CalamityGlobalNPC).GetMethod("UpdateLifeRegen", Utilities.UniversalBindingFlags), value);
        }

        public static event ILContext.Manipulator CalamityGenNewTemple
        {
            add => HookEndpointManager.Modify(typeof(CustomTemple).GetMethod("GenNewTemple", Utilities.UniversalBindingFlags), value);
            remove => HookEndpointManager.Unmodify(typeof(CustomTemple).GetMethod("GenNewTemple", Utilities.UniversalBindingFlags), value);
        }

        public static event ILContext.Manipulator SepulcherHeadModifyProjectile
        {
            add => HookEndpointManager.Modify(typeof(SCalWormHead).GetMethod("ModifyHitByProjectile", Utilities.UniversalBindingFlags), value);
            remove => HookEndpointManager.Unmodify(typeof(SCalWormHead).GetMethod("ModifyHitByProjectile", Utilities.UniversalBindingFlags), value);
        }

        public static event ILContext.Manipulator SepulcherBodyModifyProjectile
        {
            add => HookEndpointManager.Modify(typeof(SCalWormBody).GetMethod("ModifyHitByProjectile", Utilities.UniversalBindingFlags), value);
            remove => HookEndpointManager.Unmodify(typeof(SCalWormBody).GetMethod("ModifyHitByProjectile", Utilities.UniversalBindingFlags), value);
        }

        public static event ILContext.Manipulator SepulcherTailModifyProjectile
        {
            add => HookEndpointManager.Modify(typeof(SCalWormTail).GetMethod("ModifyHitByProjectile", Utilities.UniversalBindingFlags), value);
            remove => HookEndpointManager.Unmodify(typeof(SCalWormTail).GetMethod("ModifyHitByProjectile", Utilities.UniversalBindingFlags), value);
        }

        public static event ILContext.Manipulator DesertScourgeItemUseItem
        {
            add => HookEndpointManager.Modify(typeof(DriedSeafood).GetMethod("UseItem", Utilities.UniversalBindingFlags), value);
            remove => HookEndpointManager.Unmodify(typeof(DriedSeafood).GetMethod("UseItem", Utilities.UniversalBindingFlags), value);
        }

        public static event ILContext.Manipulator AresBodyCanHitPlayer
        {
            add => HookEndpointManager.Modify(typeof(AresBody).GetMethod("CanHitPlayer", Utilities.UniversalBindingFlags), value);
            remove => HookEndpointManager.Unmodify(typeof(AresBody).GetMethod("CanHitPlayer", Utilities.UniversalBindingFlags), value);
        }

        public static void ILEditingLoad()
        {
            On.Terraria.Gore.NewGore += RemoveCultistGore;
            IL.Terraria.Player.ItemCheck += ItemCheckChange;
            IL.Terraria.Main.DrawTiles += WoFLavaColorChange;
            IL.Terraria.GameContent.Liquid.LiquidRenderer.InternalDraw += WoFLavaColorChange2;
            IL.Terraria.Main.DoDraw += DrawSignusBlack;
            On.Terraria.Player.KillMe += RemoveTheDamnCancerousDoGInstakill;
            ModifyPreAINPC += NPCPreAIChange;
            ModifySetDefaultsNPC += NPCSetDefaultsChange;
            ModifyFindFrameNPC += NPCFindFrameChange;
            ModifyPreDrawNPC += NPCPreDrawChange;
            ModifyCheckDead += NPCCheckDeadChange;
            ModifyPreAIProjectile += ProjectilePreAIChange;
            ModifyPreDrawProjectile += ProjectilePreDrawChange;
            ModeIndicatorUIDraw += DrawInfernumIcon;
            CalamityWorldPostUpdate += PermitODRain;
            CalamityPlayerOtherBuffEffects += DisablePlagueDarkness;
            CalamityNPCLifeRegen += NerfShellfishStaff;
            CalamityGenNewTemple += MakeGolemRoomInvariable;
            SepulcherHeadModifyProjectile += FuckYou;
            SepulcherBodyModifyProjectile += FuckYou;
            SepulcherTailModifyProjectile += FuckYou;
            DesertScourgeItemUseItem += GetRidOfDesertNuisances;
            AresBodyCanHitPlayer += LetAresHitPlayer;
        }

        public static void ILEditingUnload()
        {
            On.Terraria.Gore.NewGore -= RemoveCultistGore;
            IL.Terraria.Player.ItemCheck -= ItemCheckChange;
            IL.Terraria.Main.DrawTiles -= WoFLavaColorChange;
            IL.Terraria.GameContent.Liquid.LiquidRenderer.InternalDraw -= WoFLavaColorChange2;
            IL.Terraria.Main.DoDraw -= DrawSignusBlack;
            On.Terraria.Player.KillMe -= RemoveTheDamnCancerousDoGInstakill;
            ModifyPreAINPC -= NPCPreAIChange;
            ModifySetDefaultsNPC -= NPCSetDefaultsChange;
            ModifyFindFrameNPC -= NPCFindFrameChange;
            ModifyPreDrawNPC -= NPCPreDrawChange;
            ModifyCheckDead -= NPCCheckDeadChange;
            ModifyPreAIProjectile -= ProjectilePreAIChange;
            ModifyPreDrawProjectile -= ProjectilePreDrawChange;
            ModeIndicatorUIDraw -= DrawInfernumIcon;
            CalamityWorldPostUpdate -= PermitODRain;
            CalamityPlayerOtherBuffEffects -= DisablePlagueDarkness;
            CalamityNPCLifeRegen -= NerfShellfishStaff;
            CalamityGenNewTemple -= MakeGolemRoomInvariable;
            SepulcherHeadModifyProjectile -= FuckYou;
            SepulcherBodyModifyProjectile -= FuckYou;
            SepulcherTailModifyProjectile -= FuckYou;
            DesertScourgeItemUseItem -= GetRidOfDesertNuisances;
            AresBodyCanHitPlayer -= LetAresHitPlayer;
        }

        // Why.
        internal static void RemoveTheDamnCancerousDoGInstakill(On.Terraria.Player.orig_KillMe orig, Player self, PlayerDeathReason damageSource, double dmg, int hitDirection, bool pvp)
        {
            if (PoDWorld.InfernumMode && damageSource.SourceCustomReason == self.name + "'s essence was consumed by the devourer.")
                return;

            orig(self, damageSource, dmg, hitDirection, pvp);
        }

        internal static int RemoveCultistGore(On.Terraria.Gore.orig_NewGore orig, Vector2 Position, Vector2 Velocity, int Type, float Scale)
        {
            if (InfernumMode.CanUseCustomAIs && Type >= GoreID.Cultist1 && Type <= GoreID.CultistBoss2)
                return Main.maxDust;

            return orig(Position, Velocity, Type, Scale);
        }

        internal static Color BlendLavaColors(Color baseColor)
        {
            if (InfernumMode.CanUseCustomAIs && Main.wof >= 0 && Main.npc[Main.wof].active)
            {
                float lifeRatio = Main.npc[Main.wof].life / (float)Main.npc[Main.wof].lifeMax;
                float fade = (float)Math.Pow(1f - lifeRatio, 0.4f);
                Color endColor = Color.Lerp(Color.DarkRed, Color.Black, 0.56f);

                return Color.Lerp(baseColor, endColor, fade);
            }
            return baseColor;
        }

        private static void WoFLavaColorChange(ILContext il)
        {
            ILCursor cursor = new ILCursor(il);

            if (!cursor.TryGotoNext(i => i.MatchLdsfld<Main>("liquidTexture")))
                return;

            if (!cursor.TryGotoPrev(MoveType.Before, i => i.MatchLdsfld<Main>("spriteBatch")))
                return;

            cursor.Emit(OpCodes.Ldloc, 151);
            cursor.Emit(OpCodes.Ldloc, 155);
            cursor.EmitDelegate<Func<int, Color, Color>>((liquidType, baseColor) =>
            {
                if (liquidType == 1 && InfernumMode.CanUseCustomAIs && Main.wof >= 0 && Main.npc[Main.wof].active)
                    baseColor = BlendLavaColors(baseColor);

                return baseColor;
            });
            cursor.Emit(OpCodes.Stloc, 155);
        }

        private static void WoFLavaColorChange2(ILContext il)
        {
            ILCursor cursor = new ILCursor(il);

            if (!cursor.TryGotoNext(MoveType.Before, i => i.MatchCallvirt<TileBatch>("Draw")))
                return;

            if (!cursor.TryGotoPrev(MoveType.Before, i => i.MatchLdsfld<Main>("tileBatch")))
                return;

            cursor.Emit(OpCodes.Ldloc, 8);
            cursor.Emit(OpCodes.Ldloc, 9);
            cursor.EmitDelegate<Func<int, VertexColors, VertexColors>>((liquidType, vertexColor) =>
            {
                if (liquidType == 1 && InfernumMode.CanUseCustomAIs && Main.wof >= 0 && Main.npc[Main.wof].active)
                {
                    vertexColor.BottomLeftColor = BlendLavaColors(vertexColor.BottomLeftColor);
                    vertexColor.BottomRightColor = BlendLavaColors(vertexColor.BottomRightColor);
                    vertexColor.TopLeftColor = BlendLavaColors(vertexColor.TopLeftColor);
                    vertexColor.TopRightColor = BlendLavaColors(vertexColor.TopRightColor);
                }
                return vertexColor;
            });
            cursor.Emit(OpCodes.Stloc, 9);
        }

        private static void ItemCheckChange(ILContext instructionContext)
        {
            var c = new ILCursor(instructionContext);
            // Attempt to match a section of code which involves the value 3032. Other values can be used, including not just numbers.
            if (!c.TryGotoNext(i => i.MatchLdcI4(3032)))
                return;
            c.Index++;
            c.EmitDelegate<Action>(() =>
            {
                if (NPC.AnyNPCs(NPCID.MoonLordCore) && PoDWorld.InfernumMode)
                {
                    Main.LocalPlayer.noBuilding = true;
                }
            });
        }

        private static readonly object hookPreAI = typeof(NPCLoader).GetField("HookPreAI", Utilities.UniversalBindingFlags).GetValue(null);
        private static FieldInfo hookListArrayField = typeof(NPCLoader).GetNestedType("HookList", Utilities.UniversalBindingFlags).GetField("arr", Utilities.UniversalBindingFlags);

        private static void NPCPreAIChange(ILContext context)
        {
            ILCursor cursor = new ILCursor(context);
            cursor.Emit(OpCodes.Ldarg_0);
            cursor.EmitDelegate(new Func<NPC, bool>(npc =>
            {
                GlobalNPC[] arr = hookListArrayField.GetValue(hookPreAI) as GlobalNPC[];
                for (int i = 0; i < arr.Length; i++)
                {
                    GlobalNPC globalNPC = arr[i];
                    if (globalNPC != null &&
                        globalNPC is CalamityGlobalNPC &&
                        OverridingListManager.InfernumNPCPreAIOverrideList.ContainsKey(npc.type) && InfernumMode.CanUseCustomAIs)
                    {
                        continue;
                    }
                    if (!globalNPC.Instance(npc).PreAI(npc))
                    {
                        return false;
                    }
                }
                return npc.modNPC == null || npc.modNPC.PreAI();
            }));
            cursor.Emit(OpCodes.Ret);
        }

        private static void NPCSetDefaultsChange(ILContext context)
        {
            ILCursor cursor = new ILCursor(context);
            cursor.Emit(OpCodes.Ldarg_0);
            cursor.Emit(OpCodes.Ldarg_1);
            cursor.EmitDelegate(new Action<NPC, bool>((npc, createModNPC) =>
            {
                GlobalNPC[] instancedGlobals = typeof(NPCLoader).GetField("InstancedGlobals", Utilities.UniversalBindingFlags).GetValue(null) as GlobalNPC[];
                if (npc.type >= 580)
                {
                    if (createModNPC)
                    {
                        typeof(NPC).GetProperty("modNPC", Utilities.UniversalBindingFlags).SetValue(npc, NPCLoader.GetNPC(npc.type).NewInstance(npc));
                    }
                    else
                    {
                        Array.Resize(ref npc.buffImmune, BuffLoader.BuffCount);
                    }
                }
                typeof(NPC).GetField("globalNPCs", Utilities.UniversalBindingFlags).SetValue(npc, (from g in instancedGlobals.ToList() select g.NewInstance(npc)).ToArray());
                npc.modNPC?.SetDefaults();
                object instance = typeof(NPCLoader).GetField("HookSetDefaults", Utilities.UniversalBindingFlags).GetValue(null);
                GlobalNPC[] arr = hookListArrayField.GetValue(instance) as GlobalNPC[];
                for (int i = 0; i < arr.Length; i++)
                {
                    GlobalNPC globalNPC = arr[i];
                    globalNPC.Instance(npc).SetDefaults(npc);
                }
                int oldLifeMax = npc.lifeMax;
                if (OverridingListManager.InfernumSetDefaultsOverrideList.ContainsKey(npc.type))
                {
                    npc.GetGlobalNPC<GlobalNPCDrawEffects>().SetDefaults(npc);
                }
            }));
            cursor.Emit(OpCodes.Ret);
        }

        private static void NPCPreDrawChange(ILContext context)
        {
            ILCursor cursor = new ILCursor(context);
            cursor.Emit(OpCodes.Ldarg_0);
            cursor.Emit(OpCodes.Ldarg_1);
            cursor.Emit(OpCodes.Ldarg_2);
            cursor.EmitDelegate(new Func<NPC, SpriteBatch, Color, bool>((npc, spriteBatch, drawColor) =>
            {
                object instance = typeof(NPCLoader).GetField("HookPreDraw", Utilities.UniversalBindingFlags).GetValue(null);
                GlobalNPC[] arr = hookListArrayField.GetValue(instance) as GlobalNPC[];

                if (OverridingListManager.InfernumPreDrawOverrideList.ContainsKey(npc.type) && InfernumMode.CanUseCustomAIs)
                    return npc.GetGlobalNPC<GlobalNPCDrawEffects>().PreDraw(npc, spriteBatch, drawColor);

                for (int i = 0; i < arr.Length; i++)
                {
                    GlobalNPC globalNPC = arr[i];
                    if (!globalNPC.Instance(npc).PreDraw(npc, spriteBatch, drawColor))
                        return false;
                }
                return npc.modNPC == null || npc.modNPC.PreDraw(spriteBatch, drawColor);
            }));
            cursor.Emit(OpCodes.Ret);
        }

        private static void NPCFindFrameChange(ILContext context)
        {
            ILCursor cursor = new ILCursor(context);

            cursor.Emit(OpCodes.Ldarg_0);
            cursor.Emit(OpCodes.Ldarg_1);
            cursor.EmitDelegate(new Action<NPC, int>((npc, frameHeight) =>
            {
                int type = npc.type;
                if (npc.modNPC != null && npc.modNPC.animationType > 0)
                {
                    npc.type = npc.modNPC.animationType;
                }
                if (OverridingListManager.InfernumFrameOverrideList.ContainsKey(type) && InfernumMode.CanUseCustomAIs)
                {
                    npc.GetGlobalNPC<GlobalNPCDrawEffects>().FindFrame(npc, frameHeight);
                    return;
                }
                npc.VanillaFindFrame(frameHeight);
                npc.type = type;
                npc.modNPC?.FindFrame(frameHeight);
                object instance = typeof(NPCLoader).GetField("HookFindFrame", Utilities.UniversalBindingFlags).GetValue(null);
                GlobalNPC[] arr = hookListArrayField.GetValue(instance) as GlobalNPC[];
                for (int i = 0; i < arr.Length; i++)
                {
                    GlobalNPC globalNPC = arr[i];
                    globalNPC.Instance(npc).FindFrame(npc, frameHeight);
                }
            }));
            cursor.Emit(OpCodes.Ret);
        }

        private static void NPCCheckDeadChange(ILContext context)
        {
            ILCursor cursor = new ILCursor(context);

            cursor.Emit(OpCodes.Ldarg_0);
            cursor.EmitDelegate(new Func<NPC, bool>(npc =>
            {
                bool result = true;
                if (npc.modNPC != null)
                {
                    result = npc.modNPC.CheckDead();
                }
                object instance = typeof(NPCLoader).GetField("HookCheckDead", Utilities.UniversalBindingFlags).GetValue(null);
                GlobalNPC[] arr = hookListArrayField.GetValue(instance) as GlobalNPC[];
                foreach (GlobalNPC g in arr)
                {
                    if (g is GlobalNPCOverrides)
                        return g.Instance(npc).CheckDead(npc);
                    result &= g.Instance(npc).CheckDead(npc);
                }
                return result;
            }));
            cursor.Emit(OpCodes.Ret);
        }

        private static void ProjectilePreAIChange(ILContext context)
        {
            ILCursor cursor = new ILCursor(context);
            cursor.Emit(OpCodes.Ldarg_0);
            cursor.EmitDelegate(new Func<Projectile, bool>(projectile =>
            {
                object instance = typeof(ProjectileLoader).GetField("HookPreAI", Utilities.UniversalBindingFlags).GetValue(null);
                GlobalProjectile[] arr = typeof(ProjectileLoader).GetNestedType("HookList", Utilities.UniversalBindingFlags).GetField("arr", Utilities.UniversalBindingFlags).GetValue(instance) as GlobalProjectile[];
                for (int i = 0; i < arr.Length; i++)
                {
                    GlobalProjectile globalNPC = arr[i];
                    if (globalNPC != null &&
                        globalNPC is CalamityMod.Projectiles.CalamityGlobalProjectile &&
                        OverridingListManager.InfernumProjectilePreAIOverrideList.ContainsKey(projectile.type))
                    {
                        continue;
                    }
                    if (!globalNPC.Instance(projectile).PreAI(projectile))
                    {
                        return false;
                    }
                }
                return projectile.modProjectile == null || projectile.modProjectile.PreAI();
            }));
            cursor.Emit(OpCodes.Ret);
        }

        private static void ProjectilePreDrawChange(ILContext context)
        {
            ILCursor cursor = new ILCursor(context);
            cursor.Emit(OpCodes.Ldarg_0);
            cursor.Emit(OpCodes.Ldarg_1);
            cursor.Emit(OpCodes.Ldarg_2);
            cursor.EmitDelegate(new Func<Projectile, SpriteBatch, Color, bool>((projectile, spriteBatch, lightColor) =>
            {
                object instance = typeof(ProjectileLoader).GetField("HookPreDraw", Utilities.UniversalBindingFlags).GetValue(null);
                GlobalProjectile[] arr = typeof(ProjectileLoader).GetNestedType("HookList", Utilities.UniversalBindingFlags).GetField("arr", Utilities.UniversalBindingFlags).GetValue(instance) as GlobalProjectile[];
                if (OverridingListManager.InfernumProjectilePreDrawOverrideList.ContainsKey(projectile.type))
                    return (bool)OverridingListManager.InfernumProjectilePreDrawOverrideList[projectile.type].DynamicInvoke(projectile, spriteBatch, lightColor);
                for (int i = 0; i < arr.Length; i++)
                {
                    GlobalProjectile globalNPC = arr[i];
                    if (globalNPC != null &&
                        globalNPC is CalamityMod.Projectiles.CalamityGlobalProjectile)
                    {
                        continue;
                    }
                    if (!globalNPC.Instance(projectile).PreDraw(projectile, spriteBatch, lightColor))
                    {
                        return false;
                    }
                }
                return projectile.modProjectile == null || projectile.modProjectile.PreDraw(spriteBatch, lightColor);
            }));
            cursor.Emit(OpCodes.Ret);
        }

        public static float frameNumber = 0f;
        private static void DrawInfernumModeUI()
        {
            // The mode indicator should only be displayed when the inventory is open, to prevent obstruction.
            if (!Main.playerInventory)
                return;

            bool renderingText = false;
            Rectangle mouseRectangle = Utils.CenteredRectangle(Main.MouseScreen, Vector2.One * 2f);

            // 3 times the number of total frames, so that each frame gets displayed 3 times.
            if (frameNumber >= 186f)
                frameNumber = 0f;

            float row = (float)Math.Floor(frameNumber / 3);
            if (row >= 27)
                row -= (((float)Math.Floor(row / 27)) * 27);

            Texture2D outerAreaTexture = ModContent.GetTexture("InfernumMode/ExtraTextures/InfernumBG");
            Texture2D armaTexture = ModContent.GetTexture("InfernumMode/ExtraTextures/InfernumArma");
            Texture2D maliceTexture = ModContent.GetTexture("InfernumMode/ExtraTextures/InfernumMalice");

            Rectangle areaFrame = outerAreaTexture.Frame(3, 27, (int)Math.Floor(frameNumber / 81), (int)row);
            Vector2 drawCenter = new Vector2(Main.screenWidth - 400f, 72f) + areaFrame.Size() * 0.5f;

            if (CalamityPlayer.areThereAnyDamnBosses)
            {
                Color drawColor = Color.Red * 0.4f;
                drawColor.A = 0;
                for (int i = 0; i < 12; i++)
                {
                    Vector2 drawOffset = (MathHelper.TwoPi * i / 12f + Main.GlobalTime * 4f).ToRotationVector2() * 5f;
                    Main.spriteBatch.Draw(outerAreaTexture, drawCenter + drawOffset, areaFrame, drawColor, 0f, areaFrame.Size() * 0.5f, 1f, SpriteEffects.None, 0f);
                }
            }

            Main.spriteBatch.Draw(outerAreaTexture, drawCenter, areaFrame, Color.White, 0f, areaFrame.Size() * 0.5f, 1f, SpriteEffects.None, 0f);
            if (CalamityWorld.armageddon)
                Main.spriteBatch.Draw(armaTexture, drawCenter, areaFrame, Color.White, 0f, areaFrame.Size() * 0.5f, 1f, SpriteEffects.None, 0f);
            if (CalamityWorld.malice)
                Main.spriteBatch.Draw(maliceTexture, drawCenter, areaFrame, Color.White, 0f, areaFrame.Size() * 0.5f, 1f, SpriteEffects.None, 0f);

            // Infernum active text.
            if (mouseRectangle.Intersects(Utils.CenteredRectangle(drawCenter - Vector2.UnitY * 14f, new Vector2(24f))))
            {
                Main.instance.MouseText(CalamityUtils.ColorMessage("Infernum Mode is active.", Color.Red));
                renderingText = true;
            }

            // Armageddon active text.
            if (!renderingText && mouseRectangle.Intersects(Utils.CenteredRectangle(drawCenter + new Vector2(12f, 7f), new Vector2(24f))))
            {
                Main.instance.MouseText(CalamityUtils.ColorMessage($"Armageddon is {(CalamityWorld.armageddon ? "active" : "not active")}.", Color.DarkViolet));
                renderingText = true;
            }

            // Armageddon active text.
            if (!renderingText && mouseRectangle.Intersects(Utils.CenteredRectangle(drawCenter + new Vector2(-12f, 7f), new Vector2(24f))))
            {
                Main.instance.MouseText(CalamityUtils.ColorMessage($"Malice is {(CalamityWorld.malice ? "active" : "not active")}.", Color.LightCoral));
                renderingText = true;
            }

            frameNumber++;
            // Displays each frame for 2 ticks instead of 3 if there are any bosses by skipping the 3rd frame.
            if (CalamityPlayer.areThereAnyDamnBosses && (frameNumber + 4) % 3 == 0)
                frameNumber++;

            // Flush text data to the screen.
            if (renderingText)
                Main.instance.MouseTextHackZoom(string.Empty);
        }

        private static void DrawInfernumIcon(ILContext il)
        {
            ILCursor cursor = new ILCursor(il);

            // Go to the last Ret and leave a marker to return to so that manual
            // drawing can be done.
            while (cursor.TryGotoNext(i => i.MatchRet())) { }

            ILLabel endOfMethod = cursor.DefineLabel();
            cursor.MarkLabel(endOfMethod);

            cursor.Index = 0;
            cursor.EmitDelegate<Action>(() =>
            {
                if (PoDWorld.InfernumMode)
                    DrawInfernumModeUI();
            });

            cursor.Emit(OpCodes.Ldsfld, typeof(PoDWorld).GetField("InfernumMode"));
            cursor.Emit(OpCodes.Brtrue, endOfMethod);
        }

        private static void DrawSignusBlack(ILContext il)
        {
            ILCursor cursor = new ILCursor(il);

            if (!cursor.TryGotoNext(MoveType.After, i => i.MatchCallOrCallvirt<MoonlordDeathDrama>("DrawWhite")))
                return;

            cursor.EmitDelegate<Action>(() =>
            {
                float fadeToBlack = 0f;
                if (CalamityGlobalNPC.signus != -1)
                    fadeToBlack = Main.npc[CalamityGlobalNPC.signus].Infernum().ExtraAI[9];
                if (InfernumMode.BlackFade > 0f)
                    fadeToBlack = InfernumMode.BlackFade;

                if (fadeToBlack > 0f)
                {
                    Color color = Color.Black * fadeToBlack;
                    Main.spriteBatch.Draw(Main.magicPixel, new Rectangle(-2, -2, Main.screenWidth + 4, Main.screenHeight + 4), new Rectangle(0, 0, 1, 1), color);
                }

                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive, Main.DefaultSamplerState, DepthStencilState.None, Main.instance.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
                for (int i = 0; i < DrawCacheProjsOverSignusBlackening.Count; i++)
                {
                    try
                    {
                        Main.instance.DrawProj(DrawCacheProjsOverSignusBlackening[i]);
                    }
                    catch (Exception e)
                    {
                        TimeLogger.DrawException(e);
                        Main.projectile[DrawCacheProjsOverSignusBlackening[i]].active = false;
                    }
                }
                DrawCacheProjsOverSignusBlackening.Clear();

                Main.spriteBatch.SetBlendState(BlendState.Additive);
                for (int i = 0; i < DrawCacheAdditiveLighting.Count; i++)
                {
                    try
                    {
                        Main.instance.DrawProj(DrawCacheAdditiveLighting[i]);
                    }
                    catch (Exception e)
                    {
                        TimeLogger.DrawException(e);
                        Main.projectile[DrawCacheAdditiveLighting[i]].active = false;
                    }
                }
                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive, Main.DefaultSamplerState, DepthStencilState.None, Main.instance.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
                DrawCacheAdditiveLighting.Clear();
            });
        }

        private static void PermitODRain(ILContext il)
        {
            ILCursor cursor = new ILCursor(il);

            if (!cursor.TryGotoNext(MoveType.Before, i => i.MatchStsfld<Main>("raining")))
                return;

            int start = cursor.Index - 1;

            if (!cursor.TryGotoNext(MoveType.After, i => i.MatchCallOrCallvirt<CalamityNetcode>("SyncWorld")))
                return;

            int end = cursor.Index;
            cursor.Goto(start);
            cursor.RemoveRange(end - start);
            cursor.Emit(OpCodes.Nop);
        }

        private static void DisablePlagueDarkness(ILContext il)
        {
            ILCursor cursor = new ILCursor(il);

            if (!cursor.TryGotoNext(MoveType.After, i => i.MatchLdfld<CalamityPlayer>("alchFlask")))
                return;

            cursor.Emit(OpCodes.Ldarg_0);
            cursor.EmitDelegate<Action<Player>>(player =>
            {
                player.blind = false;
            });
        }

        private static void NerfShellfishStaff(ILContext il)
        {
            ILCursor cursor = new ILCursor(il);

            for (int j = 0; j < 2; j++)
            {
                if (!cursor.TryGotoNext(MoveType.Before, i => i.MatchLdcI4(250)))
                    return;
            }

            cursor.Remove();
            cursor.Emit(OpCodes.Ldc_I4, 150);

            if (!cursor.TryGotoNext(MoveType.Before, i => i.MatchLdcI4(50)))
                return;

            cursor.Remove();
            cursor.Emit(OpCodes.Ldc_I4, 30);
        }

        private static void MakeGolemRoomInvariable(ILContext il)
        {
            ILCursor cursor = new ILCursor(il);

            if (!cursor.TryGotoNext(MoveType.Before, i => i.MatchLdcI4(106)))
                return;

            if (!cursor.TryGotoNext(MoveType.Before, i => i.MatchStloc(29)))
                return;

            cursor.Emit(OpCodes.Ldsfld, typeof(GolemBodyBehaviorOverride).GetField("ArenaWidth", Utilities.UniversalBindingFlags));
            cursor.Emit(OpCodes.Stloc, 31);
            cursor.Emit(OpCodes.Ldsfld, typeof(GolemBodyBehaviorOverride).GetField("ArenaHeight", Utilities.UniversalBindingFlags));
            cursor.Emit(OpCodes.Stloc, 32);
        }        

        private static void FuckYou(ILContext il)
        {
            ILCursor cursor = new ILCursor(il);
            cursor.Emit(OpCodes.Ret);
        }

        private static void GetRidOfDesertNuisances(ILContext il)
        {
            ILCursor cursor = new ILCursor(il);
            cursor.Emit(OpCodes.Ldarg_1);
            cursor.EmitDelegate<Action<Player>>(player =>
            {
                Main.PlaySound(SoundID.Roar, player.position, 0);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                    NPC.SpawnOnPlayer(player.whoAmI, ModContent.NPCType<DesertScourgeHead>());
                else
                    NetMessage.SendData(MessageID.SpawnBoss, -1, -1, null, player.whoAmI, ModContent.NPCType<DesertScourgeHead>());
            });
            cursor.Emit(OpCodes.Ldc_I4_1);
            cursor.Emit(OpCodes.Ret);
        }

        private static void LetAresHitPlayer(ILContext il)
        {
            ILCursor cursor = new ILCursor(il);
            cursor.Emit(OpCodes.Ldc_I4_1);
            cursor.Emit(OpCodes.Ret);
        }
    }
}