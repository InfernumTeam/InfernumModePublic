using CalamityMod;
using CalamityMod.CalPlayer;
using InfernumMode.Content.BehaviorOverrides.BossAIs.SupremeCalamitas.CragsCutscene;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace InfernumMode.Core.GlobalInstances.Systems
{
    public class CalamitasCragsSpawnSystem : ModSystem
    {
        public static Point CragMiddle
        {
            get
            {
                int biomeStart = Main.dungeonX < Main.maxTilesX / 2 ? 25 : (Main.maxTilesX - (Main.maxTilesX / 5)) - 25;
                int biomeEdge = biomeStart + (Main.maxTilesX / 5);
                int biomeMiddle = (biomeStart + biomeEdge) / 2;
                return new Point(biomeMiddle, Main.maxTilesY - 120);
            }
        }

        public override void PreUpdateWorld()
        {
            Vector2 calSpawnPosition = CragMiddle.ToWorldCoordinates() + Vector2.UnitX * 1040f;
            calSpawnPosition.Y -= 30f;
            while (calSpawnPosition.X >= 1600f && Utilities.GetGroundPositionFrom(calSpawnPosition).Distance(calSpawnPosition) >= 50f)
                calSpawnPosition.X -= 16f;

            if (WorldSaveSystem.MetCalamitasAtCrags || !InfernumMode.CanUseCustomAIs || !DownedBossSystem.downedBrimstoneElemental)
                return;

            for (int i = 0; i < Main.maxPlayers; i++)
            {
                Player p = Main.player[i];
                if (!p.dead && p.active && !CalamityPlayer.areThereAnyDamnBosses && p.Calamity().ZoneCalamity && p.WithinRange(calSpawnPosition, 1800f) && !p.WithinRange(calSpawnPosition, 1000f))
                {
                    Utilities.NewProjectileBetter(calSpawnPosition, Vector2.Zero, ModContent.ProjectileType<CalamitasCutsceneProj>(), 0, 0f);
                    WorldSaveSystem.MetCalamitasAtCrags = true;
                    break;
                }
            }
        }
    }
}
