using CalamityMod;
using CalamityMod.CalPlayer;
using CalamityMod.NPCs.Signus;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace InfernumMode.Core.GlobalInstances.Systems
{
    public class SignusGardenSpawnSystem : ModSystem
    {
        public override void PreUpdateWorld()
        {
            if (WorldSaveSystem.MetSignusAtProfanedGarden || !InfernumMode.CanUseCustomAIs || !NPC.downedMoonlord)
                return;

            Vector2 signusSpawnPosition = WorldSaveSystem.ProvidenceArena.TopLeft() * 16f + new Vector2(4380f, 1600f);
            for (int i = 0; i < Main.maxPlayers; i++)
            {
                Player p = Main.player[i];
                if (!p.dead && p.active && CalamityPlayer.areThereAnyDamnBosses && p.Infernum_Biome().ZoneProfaned && p.WithinRange(signusSpawnPosition, 2000f))
                {
                    int signus = NPC.NewNPC(new EntitySource_WorldEvent(), (int)signusSpawnPosition.X, (int)signusSpawnPosition.Y, ModContent.NPCType<Signus>(), 0, 0f, 0f, 0f, 1f);
                    NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, signus);
                    WorldSaveSystem.MetSignusAtProfanedGarden = true;
                    break;
                }
            }
        }
    }
}
