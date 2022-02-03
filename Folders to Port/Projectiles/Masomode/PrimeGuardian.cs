using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;

namespace FargowiltasSouls.Projectiles.Masomode
{
    public class PrimeGuardian : MutantBoss.MutantGuardian
    {
        public override string Texture => "Terraria/Images/NPC_127";

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Dungeon Guardian Prime");
            Main.projFrames[projectile.type] = 3;
        }

        public override void SetDefaults()
        {
            base.SetDefaults();
            projectile.timeLeft = 600;
            CooldownSlot = -1;
        }

        public override bool CanHitPlayer(Player target)
        {
            return true;
        }

        public override void AI()
        {
            if (projectile.localAI[0] == 0)
            {
                projectile.localAI[0] = 1;
                projectile.rotation = Main.rand.NextFloat(0, 2 * (float)Math.PI);
                projectile.hide = false;

                for (int i = 0; i < 30; i++)
                {
                    int dust = Dust.NewDust(projectile.position, projectile.width, projectile.height, DustID.Torch, 0, 0, 100, default(Color), 2f);
                    Main.dust[dust].noGravity = true;
                }
            }

            projectile.frame = 2;
            projectile.direction = projectile.velocity.X < 0 ? -1 : 1;
            projectile.rotation += projectile.direction * .3f;
        }

        public override void OnHitPlayer(Player target, int damage, bool crit)
        {
            target.AddBuff(ModContent.BuffType<NanoInjection>(), 480);
            target.AddBuff(ModContent.BuffType<Defenseless>(), 480);
            target.AddBuff(ModContent.BuffType<Lethargic>(), 480);
        }

        public override void Kill(int timeLeft)
        {
            for (int i = 0; i < 30; i++)
            {
                int dust = Dust.NewDust(projectile.position, projectile.width, projectile.height, DustID.Torch, 0, 0, 100, default(Color), 2f);
                Main.dust[dust].noGravity = true;
            }

            Gore.NewGore(projectile.Center, projectile.velocity / 3, mod.GetGoreSlot(Main.rand.NextBool() ? "Gores/Skeletron/Gore_149" : "Gores/Skeletron/Gore_150"), projectile.scale);
        }
    }
}

