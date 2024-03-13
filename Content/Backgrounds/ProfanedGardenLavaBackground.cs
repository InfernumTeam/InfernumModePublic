using CalamityMod;
using CalamityMod.CalPlayer;
using Microsoft.Xna.Framework;
using Terraria;

namespace InfernumMode.Content.Backgrounds
{
    public class ProfanedGardenLavaBackground : BaseHellLavaBackground
    {
        public override bool IsActive => Main.LocalPlayer.Infernum_Biome().ZoneProfaned;

        public override Color LavaColor
        {
            get
            {
                Color dayColor = Color.Lerp(Color.Orange, Color.Wheat, 0.67f) with { A = 0 } * (CalamityPlayer.areThereAnyDamnBosses ? 0.45f : 1f);
                Color nightColor = Color.Lerp(Color.MediumBlue, Color.Yellow, 0.3f) with { A = 0 } * (CalamityPlayer.areThereAnyDamnBosses ? 0.35f : 1f);
                float nightInterpolant = 1f;
                if (Main.dayTime)
                    nightInterpolant = 1f - Utils.GetLerpValue(0f, 1500f, (float)Main.time, true) * Utils.GetLerpValue((float)Main.dayLength, (float)Main.dayLength - 1500f, (float)Main.time, true);

                return Color.Lerp(dayColor, nightColor, nightInterpolant);
            }
        }
    }
}
