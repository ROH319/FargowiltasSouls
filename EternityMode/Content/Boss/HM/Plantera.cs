﻿using Fargowiltas.Items.Summons.Mutant;
using FargowiltasSouls.Buffs.Masomode;
using FargowiltasSouls.EternityMode.Net;
using FargowiltasSouls.EternityMode.Net.Strategies;
using FargowiltasSouls.EternityMode.NPCMatching;
using FargowiltasSouls.Items.Accessories.Masomode;
using FargowiltasSouls.NPCs.EternityMode;
using FargowiltasSouls.Projectiles.Masomode;
using FargowiltasSouls.Projectiles.MutantBoss;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace FargowiltasSouls.EternityMode.Content.Boss.HM
{
    public abstract class PlanteraPart : EModeNPCBehaviour
    {
        public override void SetDefaults(NPC npc)
        {
            base.SetDefaults(npc);

            npc.buffImmune[BuffID.Poisoned] = true;
        }

        public override void OnHitPlayer(NPC npc, Player target, int damage, bool crit)
        {
            base.OnHitPlayer(npc, target, damage, crit);

            target.AddBuff(BuffID.Poisoned, 300);
            target.AddBuff(ModContent.BuffType<Infested>(), 180);
            target.AddBuff(ModContent.BuffType<IvyVenom>(), 240);
        }

        public override void LoadSprites(NPC npc, bool recolor)
        {
            base.LoadSprites(npc, recolor);

            LoadNPCSprite(recolor, npc.type);
        }
    }

    public class Plantera : PlanteraPart
    {
        public override NPCMatcher CreateMatcher() => new NPCMatcher().MatchType(NPCID.Plantera);

        public int DicerTimer;
        public int RingTossTimer;
        public int TentacleTimer = 480; //line up first tentacles with ring toss lmao, 600

        public float TentacleAttackAngleOffset;

        public bool IsVenomEnraged;
        public bool InPhase2;
        public bool EnteredPhase2;

        public bool DroppedSummon;

        public override Dictionary<Ref<object>, CompoundStrategy> GetNetInfo() =>
            new Dictionary<Ref<object>, CompoundStrategy> {
                { new Ref<object>(DicerTimer), IntStrategies.CompoundStrategy },
                { new Ref<object>(RingTossTimer), IntStrategies.CompoundStrategy },
                { new Ref<object>(TentacleTimer), IntStrategies.CompoundStrategy },

                { new Ref<object>(IsVenomEnraged), BoolStrategies.CompoundStrategy },
                { new Ref<object>(InPhase2), BoolStrategies.CompoundStrategy },
                { new Ref<object>(EnteredPhase2), BoolStrategies.CompoundStrategy },
            };

        public override void SetDefaults(NPC npc)
        {
            base.SetDefaults(npc);

            npc.lifeMax = (int)(npc.lifeMax * 1.75);
        }

        public override void AI(NPC npc)
        {
            IsVenomEnraged = false;

            if (FargoSoulsWorld.SwarmActive)
                return;

            if (!npc.HasValidTarget)
                npc.velocity.Y++;

            const float innerRingDistance = 130f;
            const int delayForRingToss = 360 + 120;

            if (--RingTossTimer < 0)
            {
                RingTossTimer = delayForRingToss;
                if (Main.netMode != NetmodeID.MultiplayerClient && !Main.npc.Any(n => n.active && n.type == ModContent.NPCType<CrystalLeaf>() && n.ai[0] == npc.whoAmI && n.ai[1] == innerRingDistance))
                {
                    const int max = 5;
                    float rotation = 2f * (float)Math.PI / max;
                    for (int i = 0; i < max; i++)
                    {
                        Vector2 spawnPos = npc.Center + new Vector2(innerRingDistance, 0f).RotatedBy(rotation * i);
                        int n = NPC.NewNPC((int)spawnPos.X, (int)spawnPos.Y, ModContent.NPCType<CrystalLeaf>(), 0, npc.whoAmI, innerRingDistance, 0, rotation * i);
                        if (Main.netMode == NetmodeID.Server && n != Main.maxNPCs)
                            NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, n);
                    }
                }
            }
            else if (RingTossTimer == 120)
            {
                npc.netUpdate = true;
                NetSync(npc);

                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    float speed = 8f;
                    int p = Projectile.NewProjectile(npc.Center, speed * npc.DirectionTo(Main.player[npc.target].Center), ModContent.ProjectileType<MutantMark2>(), npc.defDamage / 4, 0f, Main.myPlayer);
                    if (p != Main.maxProjectiles)
                    {
                        Main.projectile[p].timeLeft -= 300;

                        foreach (NPC n in Main.npc.Where(n => n.active && n.type == ModContent.NPCType<CrystalLeaf>() && n.ai[0] == npc.whoAmI && n.ai[1] == innerRingDistance)) //my crystal leaves
                        {
                            Main.PlaySound(SoundID.Grass, n.Center);
                            Projectile.NewProjectile(n.Center, Vector2.Zero, ModContent.ProjectileType<PlanteraCrystalLeafRing>(), npc.defDamage / 4, 0f, Main.myPlayer, Main.projectile[p].identity, n.ai[3]);

                            n.life = 0;
                            n.HitEffect();
                            n.checkDead();
                            n.active = false;
                            if (Main.netMode == NetmodeID.Server)
                                NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, n.whoAmI);
                        }
                    }
                }
            }

            if (npc.life > npc.lifeMax / 2)
            {
                /*if (--Counter0 < 0)
                {
                    Counter0 = 150 * 4 + 25;
                    if (npc.HasValidTarget && Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        Projectile.NewProjectile(Main.player[npc.target].Center, Vector2.Zero, ModContent.ProjectileType<DicerPlantera>(), npc.defDamage / 4, 0f, Main.myPlayer, 0, 0);
                        for (int i = 0; i < 3; i++)
                        {
                            Projectile.NewProjectile(Main.player[npc.target].Center, 30f * npc.DirectionTo(Main.player[npc.target].Center).RotatedBy(2 * (float)Math.PI / 3 * i),
                              ModContent.ProjectileType<DicerPlantera>(), npc.defDamage / 4, 0f, Main.myPlayer, 1, 1);
                        }
                    }
                }*/
            }
            else
            {
                //Aura(npc, 700, ModContent.BuffType<IvyVenom>(), true, 188);
                InPhase2 = true;
                //npc.defense += 21;

                void SpawnOuterLeafRing()
                {
                    const int max = 12;
                    const float distance = 250;
                    float rotation = 2f * (float)Math.PI / max;
                    for (int i = 0; i < max; i++)
                    {
                        Vector2 spawnPos = npc.Center + new Vector2(distance, 0f).RotatedBy(rotation * i);
                        int n = NPC.NewNPC((int)spawnPos.X, (int)spawnPos.Y, ModContent.NPCType<CrystalLeaf>(), 0, npc.whoAmI, distance, 0, rotation * i);
                        if (Main.netMode == NetmodeID.Server && n != Main.maxNPCs)
                            NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, n);
                    }
                }

                if (!EnteredPhase2)
                {
                    EnteredPhase2 = true;

                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        if (!Main.npc.Any(n => n.active && n.type == ModContent.NPCType<CrystalLeaf>() && n.ai[0] == npc.whoAmI && n.ai[1] == innerRingDistance))
                        {
                            const int innerMax = 5;
                            float innerRotation = 2f * (float)Math.PI / innerMax;
                            for (int i = 0; i < innerMax; i++)
                            {
                                Vector2 spawnPos = npc.Center + new Vector2(innerRingDistance, 0f).RotatedBy(innerRotation * i);
                                int n = NPC.NewNPC((int)spawnPos.X, (int)spawnPos.Y, ModContent.NPCType<CrystalLeaf>(), 0, npc.whoAmI, innerRingDistance, 0, innerRotation * i);
                                if (Main.netMode == NetmodeID.Server && n != Main.maxNPCs)
                                    NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, n);
                            }
                        }

                        SpawnOuterLeafRing();

                        for (int i = 0; i < Main.maxProjectiles; i++)
                        {
                            if (Main.projectile[i].active && Main.projectile[i].hostile &&
                                (Main.projectile[i].type == ProjectileID.ThornBall
                                || Main.projectile[i].type == ModContent.ProjectileType<DicerPlantera>()
                                || Main.projectile[i].type == ModContent.ProjectileType<PlanteraCrystalLeafRing>()
                                || Main.projectile[i].type == ModContent.ProjectileType<CrystalLeafShot>()))
                            {
                                Main.projectile[i].Kill();
                            }
                        }
                    }
                }

                //explode time * explode repetitions + spread delay * propagations
                const int delayForDicers = 150 * 4 + 25 * 8;

                if (--DicerTimer < -120)
                {
                    DicerTimer = delayForDicers + delayForRingToss + 240;
                    //Counter3 = delayForDicers + 120; //extra compensation for the toss offset

                    npc.netUpdate = true;
                    NetSync(npc);

                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        Projectile.NewProjectile(npc.Center, Vector2.Zero, ModContent.ProjectileType<DicerPlantera>(), npc.defDamage / 4, 0f, Main.myPlayer);
                        for (int i = 0; i < 3; i++)
                        {
                            Projectile.NewProjectile(npc.Center, 25f * npc.DirectionTo(Main.player[npc.target].Center).RotatedBy(2 * (float)Math.PI / 3 * i),
                              ModContent.ProjectileType<DicerPlantera>(), npc.defDamage / 4, 0f, Main.myPlayer, 1, 8);
                        }
                    }
                }

                if (DicerTimer > delayForDicers || DicerTimer < 0)
                {
                    if (RingTossTimer > 120) //to still respawn the leaf ring if it's missing but disable throwing it
                        RingTossTimer = 120;
                }
                else if (DicerTimer < delayForDicers)
                {
                    RingTossTimer -= 1;

                    if (RingTossTimer % 2 == 0) //make sure plantera can get the timing for its check
                        RingTossTimer--;
                }
                else if (DicerTimer == delayForDicers)
                {
                    RingTossTimer = 121; //activate it immediately as the mines fade
                }

                IsVenomEnraged = npc.HasPlayerTarget && Main.player[npc.target].venom;

                if (--TentacleTimer <= 0)
                {
                    npc.position -= npc.velocity * Math.Min(0.9f, -TentacleTimer / 60f);

                    if (TentacleTimer == 0)
                    {
                        TentacleAttackAngleOffset = Main.rand.NextFloat(MathHelper.TwoPi);

                        Main.PlaySound(SoundID.Roar, npc.Center, 0);

                        npc.netUpdate = true;
                        NetSync(npc);

                        foreach (NPC n in Main.npc.Where(n => n.active && n.type == ModContent.NPCType<CrystalLeaf>() && n.ai[0] == npc.whoAmI && n.ai[1] > innerRingDistance)) //my crystal leaves
                        {
                            Main.PlaySound(SoundID.Grass, n.Center);

                            n.life = 0;
                            n.HitEffect();
                            n.checkDead();
                            n.active = false;
                            if (Main.netMode == NetmodeID.Server)
                                NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, n.whoAmI);
                        }
                    }
                    
                    const int maxTime = 30;
                    const int interval = 3;
                    const float maxDegreeCoverage = 45f; //on either side of the middle, the full coverage of one side is x2 this
                    if (TentacleTimer >= -maxTime && TentacleTimer % interval == 0)
                    {
                        int tentacleSpawnOffset = Math.Abs(TentacleTimer) / interval;
                        for (int i = -tentacleSpawnOffset; i <= tentacleSpawnOffset; i += tentacleSpawnOffset * 2)
                        {
                            float attackAngle = MathHelper.WrapAngle(TentacleAttackAngleOffset + MathHelper.ToRadians(maxDegreeCoverage / (maxTime / interval)) * i);

                            if (Main.netMode != NetmodeID.MultiplayerClient)
                            {
                                Projectile.NewProjectile(npc.Center, Main.rand.NextVector2CircularEdge(24, 24),
                                    ModContent.ProjectileType<PlanteraTentacle>(), npc.damage / 4, 0f, Main.myPlayer, npc.whoAmI, attackAngle);
                                Projectile.NewProjectile(npc.Center, Main.rand.NextVector2CircularEdge(24, 24),
                                    ModContent.ProjectileType<PlanteraTentacle>(), npc.damage / 4, 0f, Main.myPlayer, npc.whoAmI, attackAngle + MathHelper.Pi);
                            }

                            if (i == 0)
                                break;
                        }
                    }

                    if (TentacleTimer < -360)
                    {
                        TentacleTimer = 600 + Main.rand.Next(120);
                        npc.velocity = Vector2.Zero;

                        npc.netUpdate = true;
                        NetSync(npc);

                        SpawnOuterLeafRing();
                    }
                }
                else
                {
                    npc.position -= npc.velocity * (IsVenomEnraged ? 0.1f : 0.2f);
                }
            }

            EModeUtils.DropSummon(npc, ModContent.ItemType<PlanterasFruit>(), NPC.downedPlantBoss, ref DroppedSummon, NPC.downedMechBoss1 && NPC.downedMechBoss2 && NPC.downedMechBoss3);
        }

        public override Color? GetAlpha(NPC npc, Color drawColor)
        {
            return IsVenomEnraged ? base.GetAlpha(npc, drawColor) : new Color(255, drawColor.G / 2, drawColor.B / 2);
        }

        public override void ModifyHitByItem(NPC npc, Player player, Item item, ref int damage, ref float knockback, ref bool crit)
        {
            base.ModifyHitByItem(npc, player, item, ref damage, ref knockback, ref crit);

            if (item.type == ItemID.FetidBaghnakhs)
                damage /= 2;
        }

        public override void NPCLoot(NPC npc)
        {
            base.NPCLoot(npc);

            npc.DropItemInstanced(npc.position, npc.Size, ModContent.ItemType<MagicalBulb>());
            npc.DropItemInstanced(npc.position, npc.Size, ItemID.JungleFishingCrate, 5);
            npc.DropItemInstanced(npc.position, npc.Size, ItemID.LifeFruit, 3);
            npc.DropItemInstanced(npc.position, npc.Size, ItemID.ChlorophyteOre, 200);
        }

        public override void LoadSprites(NPC npc, bool recolor)
        {
            base.LoadSprites(npc, recolor);
            
            LoadBossHeadSprite(recolor, 11);
            LoadBossHeadSprite(recolor, 12);
            LoadGoreRange(recolor, 378, 391);
            Main.chain26Texture = LoadSprite(recolor, "Chain26");
            Main.chain27Texture = LoadSprite(recolor, "Chain27");
        }
    }

    public class PlanterasHook : PlanteraPart
    {
        public override NPCMatcher CreateMatcher() => new NPCMatcher().MatchType(NPCID.PlanterasHook);

        public override void AI(NPC npc)
        {
            if (FargoSoulsWorld.SwarmActive)
                return;

            npc.damage = 0;
            npc.defDamage = 0;

            NPC plantera = FargoSoulsUtil.NPCExists(NPC.plantBoss, NPCID.Plantera);
            if (plantera != null && plantera.life < plantera.lifeMax / 2 && plantera.HasValidTarget)
            {
                if (npc.Distance(Main.player[plantera.target].Center) > 600)
                {
                    Vector2 targetPos = Main.player[plantera.target].Center / 16; //pick a new target pos near player
                    targetPos.X += Main.rand.Next(-25, 26);
                    targetPos.Y += Main.rand.Next(-25, 26);

                    Tile tile = Framing.GetTileSafely((int)targetPos.X, (int)targetPos.Y);
                    npc.localAI[0] = 600; //reset vanilla timer for picking new block
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                        npc.netUpdate = true;

                    npc.ai[0] = targetPos.X;
                    npc.ai[1] = targetPos.Y;
                }

                if (npc.Distance(new Vector2(npc.ai[0] * 16 + 8, npc.ai[1] * 16 + 8)) > 32)
                    npc.position += npc.velocity;
            }
        }
    }

    public class PlanterasTentacle : PlanteraPart
    {
        public override NPCMatcher CreateMatcher() => new NPCMatcher().MatchType(NPCID.PlanterasTentacle);

        public int ChangeDirectionTimer;
        public int RotationDirection;
        public int MaxDistanceFromPlantera;
        public int CanHitTimer;

        public bool DroppedSummon;

        public override Dictionary<Ref<object>, CompoundStrategy> GetNetInfo() =>
            new Dictionary<Ref<object>, CompoundStrategy> {
                { new Ref<object>(ChangeDirectionTimer), IntStrategies.CompoundStrategy },
                { new Ref<object>(RotationDirection), IntStrategies.CompoundStrategy },
                { new Ref<object>(MaxDistanceFromPlantera), IntStrategies.CompoundStrategy },
                { new Ref<object>(CanHitTimer), IntStrategies.CompoundStrategy },
            };

        public override void SetDefaults(NPC npc)
        {
            base.SetDefaults(npc);

            MaxDistanceFromPlantera = 200;
        }

        public override bool CanHitPlayer(NPC npc, Player target, ref int cooldownSlot)
        {
            return base.CanHitPlayer(npc, target, ref cooldownSlot) && CanHitTimer > 60;
        }

        public override void AI(NPC npc)
        {
            if (FargoSoulsWorld.SwarmActive)
                return;

            NPC plantera = FargoSoulsUtil.NPCExists(NPC.plantBoss, NPCID.Plantera);
            if (plantera != null)
            {
                npc.position += plantera.velocity / 3;
                if (npc.Distance(plantera.Center) > MaxDistanceFromPlantera) //snap back in really fast if too far
                {
                    Vector2 vel = plantera.Center - npc.Center;
                    vel += MaxDistanceFromPlantera * plantera.DirectionFrom(npc.Center).RotatedBy(MathHelper.ToRadians(45) * RotationDirection);
                    npc.velocity = Vector2.Lerp(npc.velocity, vel / 15, 0.05f);
                }
            }

            if (++ChangeDirectionTimer > 120)
            {
                ChangeDirectionTimer = Main.rand.Next(30);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    RotationDirection = Main.rand.NextBool() ? -1 : 1;
                    MaxDistanceFromPlantera = 50 + Main.rand.Next(150);
                    npc.netUpdate = true;
                    NetSync(npc);
                }
            }

            ++CanHitTimer;
        }

        public override void LoadSprites(NPC npc, bool recolor)
        {
            base.LoadSprites(npc, recolor);

            LoadNPCSprite(recolor, npc.type);
        }
    }

    public class Spore : PlanteraPart
    {
        public override NPCMatcher CreateMatcher() => new NPCMatcher().MatchType(NPCID.Spore);
    }
}
