﻿using System;
using System.Linq;
using System.Collections.Generic;
using System.Globalization;
using EloBuddy;
using EloBuddy.SDK;
using EloBuddy.SDK.Enumerations;
using EloBuddy.SDK.Events;
using EloBuddy.SDK.Menu;
using EloBuddy.SDK.Menu.Values;
using EloBuddy.SDK.Rendering;
using SharpDX;
using Color = System.Drawing.Color;

namespace T7_Fiora
{
    class Program
    {
        static void Main(string[] args) { Loading.OnLoadingComplete += OnLoad; }
        public static AIHeroClient myhero { get { return ObjectManager.Player; } }
        private static Menu menu, combo, harass, laneclear, misc, draw, pred, fleee;
        private static Spell.Targeted ignt = new Spell.Targeted(myhero.GetSpellSlotFromName("summonerdot"), 550);
        public static Item tiamat { get; private set; }
        public static Item rhydra { get; private set; }
        public static Item thydra { get; private set; }
        public static Item cutl { get; private set; }
        public static Item blade { get; private set; }
        public static Item yomus { get; private set; }

        private static void OnLoad(EventArgs args)
        {
            if (Player.Instance.ChampionName != "Fiora") { return; }
            Chat.Print("<font color='#0040FF'>T7</font><font color='#FF0505'> Fiora</font> : Loaded!(v1.0)");
            Chat.Print("<font color='#04B404'>By </font><font color='#FF0000'>T</font><font color='#FA5858'>o</font><font color='#FF0000'>y</font><font color='#FA5858'>o</font><font color='#FF0000'>t</font><font color='#FA5858'>a</font><font color='#0040FF'>7</font><font color='#FF0000'> <3 </font>");
            Drawing.OnDraw += OnDraw;
            Obj_AI_Base.OnLevelUp += OnLvlUp;
            // Game.OnUpdate += OnUpdate;
            // Gapcloser.OnGapcloser += OnGapcloser
            DatMenu();
            Game.OnTick += OnTick;
            tiamat = new Item((int)ItemId.Tiamat_Melee_Only, 400);
            rhydra = new Item((int)ItemId.Ravenous_Hydra_Melee_Only, 400);
            thydra = new Item((int)ItemId.Titanic_Hydra);
            cutl = new Item((int)ItemId.Bilgewater_Cutlass, 550);
            blade = new Item((int)ItemId.Blade_of_the_Ruined_King, 550);
            yomus = new Item((int)ItemId.Youmuus_Ghostblade);
            Player.LevelSpell(SpellSlot.Q);
        }

        private static void OnTick(EventArgs args)
        {
            if (myhero.IsDead) return;

            var flags = Orbwalker.ActiveModesFlags;

            if (flags.HasFlag(Orbwalker.ActiveModes.Combo)) { Combo(); }

            if (flags.HasFlag(Orbwalker.ActiveModes.Harass) && myhero.ManaPercent > harass["HMIN"].Cast<Slider>().CurrentValue) Harass();

            if (flags.HasFlag(Orbwalker.ActiveModes.LaneClear) && myhero.ManaPercent > laneclear["LMIN"].Cast<Slider>().CurrentValue) Laneclear();

            if (flags.HasFlag(Orbwalker.ActiveModes.Flee)) Flee();

            Misc();
        }

        private static bool check(Menu submenu, string sig)
        {
            return submenu[sig].Cast<CheckBox>().CurrentValue;
        }

        private static int slider(Menu submenu, string sig)
        {
            return submenu[sig].Cast<Slider>().CurrentValue;
        }

        private static int comb(Menu submenu, string sig)
        {
            return submenu[sig].Cast<ComboBox>().CurrentValue;
        }

        private static void OnLvlUp(Obj_AI_Base sender, Obj_AI_BaseLevelUpEventArgs args)
        {
            if (!sender.IsMe) return;

            /*Q>E>W*/
            SpellSlot[] sequence1 = { SpellSlot.Unknown, SpellSlot.W, SpellSlot.E, SpellSlot.Q,
                                        SpellSlot.Q, SpellSlot.R, SpellSlot.Q, SpellSlot.E, 
                                        SpellSlot.Q, SpellSlot.E, SpellSlot.R, SpellSlot.E, 
                                        SpellSlot.E, SpellSlot.W, SpellSlot.W, SpellSlot.R, 
                                        SpellSlot.W , SpellSlot.W };

            if (check(misc, "autolevel")) Player.LevelSpell(sequence1[myhero.Level]);
        }

        private static float ComboDamage(AIHeroClient target)
        {
            if (target != null)
            {
                float TotalDamage = 0;

                if (DemSpells.Q.IsLearned && DemSpells.Q.IsReady()) { TotalDamage += QDamage(target); }

                if (DemSpells.W.IsLearned && DemSpells.W.IsReady()) { TotalDamage += WDamage(target); }

                if (DemSpells.E.IsLearned && DemSpells.E.IsReady())
                {
                    TotalDamage += (float)(myhero.GetAutoAttackDamage(target) + (new float[] {0, 1.4f, 1.55f, 1.7f, 1.85f, 2 }[DemSpells.E.Level] * myhero.TotalAttackDamage));
                }

                if (DemSpells.R.IsLearned && DemSpells.R.IsReady())
                {  TotalDamage += (float)PassiveManager.GetPassiveDamage(target, 4); }
                else { TotalDamage += (float)PassiveManager.GetPassiveDamage(target, PassiveManager.GetPassiveCount(target)); }

                if(tiamat.IsOwned() && tiamat.IsReady() && tiamat.IsInRange(target.Position))
                { TotalDamage += myhero.GetItemDamage(target, tiamat.Id); }

                if (rhydra.IsOwned() && rhydra.IsReady() && rhydra.IsInRange(target.Position))
                { TotalDamage += myhero.GetItemDamage(target, rhydra.Id); }

              /*  if (thydra.IsOwned() && thydra.IsReady())
                { TotalDamage += myhero.GetItemDamage(target, thydra.Id); }*/

                if (cutl.IsOwned() && cutl.IsReady() && cutl.IsInRange(target.Position))
                { TotalDamage += myhero.GetItemDamage(target, cutl.Id); }

                if (blade.IsOwned() && blade.IsReady() && blade.IsInRange(target.Position))
                { TotalDamage += myhero.GetItemDamage(target, blade.Id); }

                return TotalDamage;
            }
            return 0;
        }

        private static float QDamage(AIHeroClient target)
        {
            int index = DemSpells.Q.Level - 1;

            var QDamage = new float[] {65, 75, 85, 95, 105}[index] +
                          (new float[] {0.55f, 0.70f, 0.85f, 1, 1.15f}[index] * myhero.TotalAttackDamage);
            return myhero.CalculateDamageOnUnit(target, DamageType.Physical, QDamage);
        }

        private static float WDamage(AIHeroClient target)
        {
            int index = DemSpells.W.Level - 1;

            var WDamage = new float[] {90, 130, 170, 210, 250}[index] + myhero.FlatMagicDamageMod;

            return myhero.CalculateDamageOnUnit(target, DamageType.Magical, WDamage);
        }

        private static void ItemManager(AIHeroClient target)
        {
            if (target != null && target.IsValidTarget() && check(combo, "ITEMS"))
            {
                if(tiamat.IsOwned() && tiamat.IsReady() && tiamat.IsInRange(target.Position))
                {
                    tiamat.Cast();
                }

                if (rhydra.IsOwned() && rhydra.IsReady() && rhydra.IsInRange(target.Position))
                {
                    rhydra.Cast();
                }

                if (thydra.IsOwned() && thydra.IsReady() && target.Distance(myhero.Position) < myhero.AttackRange && !Orbwalker.CanAutoAttack)
                {
                    thydra.Cast();
                }

                if (cutl.IsOwned() && cutl.IsReady() && cutl.IsInRange(target.Position))
                {
                    cutl.Cast(target);
                }

                if (blade.IsOwned() && blade.IsReady() && blade.IsInRange(target.Position))
                {
                    blade.Cast(target);
                }

                if (yomus.IsOwned() && yomus.IsReady() && target.Distance(myhero.Position) < 1000)
                {
                    yomus.Cast();
                }
            }

        }

        private static void Combo()
        {
            var target = TargetSelector.GetTarget(1200, DamageType.Physical, Player.Instance.Position);
            
            if (target != null && target.IsValidTarget() && !target.IsInvulnerable)
            {
                var WPred = DemSpells.W.GetPrediction(target);

                PassiveManager.castAutoAttack(target);

                ItemManager(target);

                if (check(combo, "CQ") && DemSpells.Q.IsReady() && DemSpells.Q.IsInRange(target.Position))
                {
                    switch(comb(pred, "QPREDMODE"))
                    {
                        case 0:
                            PassiveManager.castQhelper(target);
                            break;
                        case 1:
                            DemSpells.Q.Cast(target.Position);
                            break;
                    }
                }

                if(check(combo, "CW") &&  DemSpells.W.IsReady() && DemSpells.W.IsInRange(target.Position))
                {
                    switch (comb(pred, "WPREDMODE"))
                    {
                        case 0:
                            if (slider(pred, "WPred") <= WPred.HitChancePercent)
                            { DemSpells.W.Cast(WPred.CastPosition); }
                            break;
                        case 1:
                            DemSpells.W.Cast(target.Position);
                            break;
                    }
                }

                if (check(combo, "CE") && DemSpells.E.IsReady() && target.Distance(myhero.Position) < DemSpells.E.Range)
                {
                    switch(check(combo, "CERESET"))
                    {
                        case true:
                            if(!Orbwalker.CanAutoAttack)
                            { DemSpells.E.Cast(); }
                            break;
                        case false:
                            DemSpells.E.Cast();
                            break;
                    }
                }

                if (check(combo, "CR") && DemSpells.R.IsReady() && target.Distance(myhero.Position) < DemSpells.R.Range &&
                    myhero.HealthPercent >= slider(combo, "CRMIN") && ComboDamage(target) > target.Health)
               {
                   if((ComboDamage(target) - PassiveManager.GetPassiveDamage(target,4) > target.Health) ||
                      (ignt.IsReady() && myhero.GetSummonerSpellDamage(target, DamageLibrary.SummonerSpells.Ignite) > target.Health)) return;

                    if (check(combo, "CRTURRET"))
                    {
                        var ClosestTurret = EntityManager.Turrets.Enemies.Where(x => x.Distance(target.Position) < 3000)
                                                                          .OrderBy(x => x.Distance(target.Position))
                                                                          .FirstOrDefault();
                        if (ClosestTurret.Distance(target.Position) > (ClosestTurret.AttackRange + 350))
                        {
                            DemSpells.R.Cast(target);
                        }                                     
                    }
                    else { DemSpells.R.Cast(target); }
                    
                }

                if (check(combo, "Cignt") && ignt.IsReady() && ignt.IsInRange(target.Position))
                {
                    if (target.Health > ComboDamage(target) && myhero.GetSummonerSpellDamage(target, DamageLibrary.SummonerSpells.Ignite) > target.Health &&
                        !check(misc, "autoign"))
                    {
                        ignt.Cast(target);
                    }
                    else if (target.Health > ComboDamage(target))
                    {
                        if((ComboDamage(target) + (myhero.GetSummonerSpellDamage(target, DamageLibrary.SummonerSpells.Ignite) - 5)) > target.Health)
                        { ignt.Cast(target); }
                    }
                }
            }
        }

        private static void Harass()
        {
            var target = TargetSelector.GetTarget(1200, DamageType.Physical, Player.Instance.Position);

            if (target != null && target.IsValidTarget())
            {
                var qpred = DemSpells.Q.GetPrediction(target);
                var wpred = DemSpells.W.GetPrediction(target);

                if (check(harass, "HQ") && DemSpells.Q.IsReady() && DemSpells.Q.IsInRange(target.Position))
                {
                    if (Extensions.CountEnemiesInRange(target, 650) <= slider(harass, "HQMAX"))
                    {
                        switch (comb(pred, "QPREDMODE"))
                        {
                            case 0:
                                PassiveManager.castQhelper(target);
                                break;
                            case 1:
                                DemSpells.Q.Cast(target.Position);
                                break;
                        }
                    }
                }

                if (check(harass, "HW") && DemSpells.W.IsReady() && DemSpells.W.IsInRange(target.Position) &&
                   !target.IsZombie && !target.IsInvulnerable)
                {
                    switch (comb(pred, "WPREDMODE"))
                    {
                        case 0:
                            if (wpred.HitChancePercent >= slider(pred, "WPred")) { DemSpells.W.Cast(wpred.CastPosition); }
                            break;
                        case 1:
                            DemSpells.W.Cast(target.Position);
                            break;

                    }
                }
            }

        }

        private static void Laneclear()
        {
            var minions = EntityManager.MinionsAndMonsters.GetLaneMinions(EntityManager.UnitTeam.Enemy, myhero.Position, DemSpells.W.Range).ToArray();

            if (minions != null)
            {
             
                var wpred = EntityManager.MinionsAndMonsters.GetLineFarmLocation(minions, DemSpells.W.Width, (int)DemSpells.W.Range);

                if (check(laneclear, "LQ") && DemSpells.Q.IsLearned && DemSpells.Q.IsReady())
                {
                    foreach (var minion in minions.Where(x => x.IsValid() && !x.IsDead && x.Health > 15))
                    {
                        if (comb(pred, "QPREDMODE") == 0 &&
                            Prediction.Position.PredictUnitPosition(minion,DemSpells.Q.CastDelay).Distance(myhero.Position) <= (DemSpells.Q.Range - 50) )
                        { DemSpells.Q.Cast(minion.Position); }

                        else { DemSpells.Q.Cast(minion.Position); }

                    }
                }

                if (check(laneclear, "LW") && DemSpells.W.IsLearned && DemSpells.W.IsReady())
                {
                    if(slider(laneclear, "LWMIN") == 1)
                    {
                        switch (comb(pred, "WPREDMODE"))
                        {
                            case 0:
                                if (wpred.HitNumber == slider(pred, "WPred")) { DemSpells.W.Cast(wpred.CastPosition); }
                                break;
                            case 1:
                                DemSpells.W.Cast(minions.Where(x => x.Distance(myhero.Position) < DemSpells.W.Range &&
                                                               !x.IsDead && x.Health > 25 && x.IsValid()).OrderBy(x => x.Distance(myhero.Position))
                                                                                                         .FirstOrDefault().Position);
                                break;
                        }
                    }
                    else
                    {
                        if(wpred.HitNumber >= slider(laneclear, "LWMIN"))
                        { DemSpells.W.Cast(wpred.CastPosition); }
                    }
                }

                if (check(laneclear, "LE") && DemSpells.E.IsLearned && DemSpells.E.IsReady())
                {
                    int count = minions.Where(x => x.IsValid() && !x.IsDead && x.Distance(myhero.Position) <= 170).Count();

                    if (count >= slider(laneclear, "LEMIN"))
                    {
                        DemSpells.E.Cast();
                    }

                }
            }
        }

        private static void Flee()
        {
            if (myhero.CountEnemiesInRange(1000) < slider(fleee, "FLEEMIN")) return;

            if(check(fleee, "QFLEE") && DemSpells.Q.IsLearned && DemSpells.Q.IsReady())
            {
                DemSpells.Q.Cast(Game.CursorPos);
            }

            if(check(fleee, "YOMUSFLEE") && yomus.IsOwned() && yomus.IsReady() )
            {
                yomus.Cast();
            }
        }

        private static void Misc()
        {
            var target = TargetSelector.GetTarget(1000, DamageType.Magical, Player.Instance.Position);


            if (check(misc, "skinhax")) myhero.SetSkinId((int)misc["skinID"].Cast<ComboBox>().CurrentValue);

            if (target != null)
            {
                var qpred = DemSpells.Q.GetPrediction(target);
                var wpred = DemSpells.W.GetPrediction(target);

                if(check(misc, "ksQ") && DemSpells.Q.IsLearned && DemSpells.Q.IsReady() && target.IsValidTarget(DemSpells.Q.Range) &&
                   !target.IsZombie && !target.IsInvulnerable && QDamage(target) > target.Health && slider(pred, "QPred") >= qpred.HitChancePercent)
                {
                    switch (comb(pred, "QPREDMODE"))
                    {
                        case 0:
                            PassiveManager.castQhelper(target);
                            break;
                        case 1:
                            DemSpells.Q.Cast(target.Position);
                            break;
                    }                 
                }

                if (check(misc, "ksW") && DemSpells.W.IsLearned && DemSpells.W.IsReady() && target.IsValidTarget(DemSpells.W.Range) &&
                   !target.IsZombie && !target.IsInvulnerable && WDamage(target) > target.Health)
                {
                    switch(comb(pred,"WPREDMODE"))
                    {
                        case 0:
                            if (wpred.HitChancePercent >= slider(pred, "WPred")) { DemSpells.W.Cast(wpred.CastPosition); }
                            break;
                        case 1:
                            DemSpells.W.Cast(target.Position);
                            break;

                    }
                }

                if (check(misc, "autoign") && ignt.IsReady() &&
                    ignt.IsInRange(target) && myhero.GetSummonerSpellDamage(target, DamageLibrary.SummonerSpells.Ignite) > target.Health)
                {
                    ignt.Cast(target);
                }
            }
         
        }

        private static void OnDraw(EventArgs args)
        {
            if (myhero.IsDead) return;

            if (check(draw, "drawQ") && DemSpells.Q.Level > 0 && !myhero.IsDead && !check(draw, "nodraw"))
            {

                if (check(draw, "drawonlyrdy"))
                { Circle.Draw(DemSpells.Q.IsOnCooldown ? SharpDX.Color.Transparent : SharpDX.Color.Fuchsia, 750, myhero.Position); }

                else if (!check(draw, "drawonlyrdy")) { Circle.Draw(SharpDX.Color.Fuchsia, 750, myhero.Position); }

            }

            if (check(draw, "drawW") && DemSpells.W.Level > 0 && !myhero.IsDead && !check(draw, "nodraw"))
            {

                if (check(draw, "drawonlyrdy"))
                { Circle.Draw(DemSpells.W.IsOnCooldown ? SharpDX.Color.Transparent : SharpDX.Color.Fuchsia, DemSpells.W.Range, myhero.Position); }

                else if (!check(draw, "drawonlyrdy")) { Circle.Draw(SharpDX.Color.Fuchsia, DemSpells.W.Range, myhero.Position); }

            }

            if (check(draw, "drawR") && DemSpells.R.Level > 0 && !myhero.IsDead && !check(draw, "nodraw"))
            {

                if (check(draw, "drawonlyrdy"))
                { Circle.Draw(DemSpells.R.IsOnCooldown ? SharpDX.Color.Transparent : SharpDX.Color.Fuchsia, DemSpells.R.Range, myhero.Position); }

                else if (!check(draw, "drawonlyrdy")) { Circle.Draw(SharpDX.Color.Fuchsia, DemSpells.R.Range, myhero.Position); }

            }

            foreach (var enemy in EntityManager.Heroes.Enemies)
            {
                if (check(draw, "drawkillable") && !check(draw, "nodraw") && enemy.IsVisible &&
                    enemy.IsHPBarRendered && !enemy.IsDead && ComboDamage(enemy) > enemy.Health)
                {
                    Drawing.DrawText(Drawing.WorldToScreen(enemy.Position).X,
                                     Drawing.WorldToScreen(enemy.Position).Y - 30,
                                     Color.Green, "Killable With Combo");
                }
                else if (check(draw, "drawkillable") && !check(draw, "nodraw") && enemy.IsVisible &&
                         enemy.IsHPBarRendered && !enemy.IsDead &&
                         ComboDamage(enemy) + myhero.GetSummonerSpellDamage(enemy, DamageLibrary.SummonerSpells.Ignite) > enemy.Health)
                {
                    Drawing.DrawText(Drawing.WorldToScreen(enemy.Position).X, Drawing.WorldToScreen(enemy.Position).Y - 30, Color.Green, "Combo + Ignite");
                }
            }
        }

        public static void DatMenu()
        {
            menu = MainMenu.AddMenu("T7 Fiora", "fiora");
            combo = menu.AddSubMenu("Combo", "combo");
            harass = menu.AddSubMenu("Harass", "harass");
            laneclear = menu.AddSubMenu("Laneclear", "lclear");
            draw = menu.AddSubMenu("Drawings", "draw");
            misc = menu.AddSubMenu("Misc", "misc");
            fleee = menu.AddSubMenu("Flee", "fleeee");
            pred = menu.AddSubMenu("Prediction", "pred");

            menu.AddGroupLabel("Welcome to T7 Fiora And Thank You For Using!");
            menu.AddGroupLabel("Version 1.0 21/6/2016");
            menu.AddGroupLabel("Author: Toyota7");
            menu.AddSeparator();
            menu.AddGroupLabel("Please Report Any Bugs And If You Have Any Requests Feel Free To PM Me <3");

            combo.AddGroupLabel("Spells");
            combo.Add("CQ", new CheckBox("Use Q", true));
            combo.Add("CQGAP", new CheckBox("Use Q To Gapclose If Enemy Out Of Range", true));
            combo.AddSeparator();
            combo.Add("CW", new CheckBox("Use W", true));
            combo.AddSeparator();
            combo.Add("CE", new CheckBox("Use E", true));
            combo.Add("CERESET", new CheckBox("Only Use E After AA(Reset AA)", true));
            combo.Add("CR", new CheckBox("Use R", true));
            combo.Add("CRTURRET", new CheckBox("Dont Use R When Close To Enemy Turrets", true));
            combo.Add("CRMIN", new Slider("Min % Health To Use R", 35, 1, 99));
            combo.AddSeparator();
            combo.Add("Cignt", new CheckBox("Use Ignite", false));
            combo.Add("ITEMS", new CheckBox("Use Items", true));

            harass.AddGroupLabel("Spells");
            harass.Add("HQ", new CheckBox("Use Q", true));
            harass.Add("HQMAX", new Slider("Dont Use Q If More Than X Enemies Nearby", 1, 1, 5));
            harass.AddSeparator();
            harass.Add("HW", new CheckBox("Use W", true));
            harass.AddSeparator();
            harass.Add("HMIN", new Slider("Min Mana % To Harass", 50, 0, 100));

            laneclear.AddGroupLabel("Spells");
            laneclear.Add("LQ", new CheckBox("Use Q", true));
            laneclear.AddSeparator();
            laneclear.Add("LW", new CheckBox("Use W", true));
            laneclear.Add("LWMIN", new Slider("Min Minions To Hit With W",2,1,6));
            laneclear.AddSeparator();
            laneclear.Add("LE", new CheckBox("Use E", true));
            laneclear.Add("LEMIN", new Slider("Min Minions To Hit With E", 2, 1, 10));
            laneclear.AddSeparator();
            laneclear.Add("LMIN", new Slider("Min Mana % To Laneclear", 50, 0, 100));

            draw.Add("nodraw", new CheckBox("Disable All Drawings", false));
            draw.AddSeparator();
            draw.Add("drawQ", new CheckBox("Draw Q Range", true));
            draw.Add("drawW", new CheckBox("Draw W Range", true));
            draw.Add("drawR", new CheckBox("Draw R Range", true));
            draw.Add("drawonlyrdy", new CheckBox("Draw Only Ready Spells", false));
            draw.Add("drawkillable", new CheckBox("Draw Killable Enemies", true));

            misc.AddGroupLabel("Killsteal");
            misc.Add("ksQ", new CheckBox("Killsteal with Q", false));
            misc.Add("ksW", new CheckBox("Killsteal with W", true));
            misc.Add("autoign", new CheckBox("Auto Ignite If Killable", true));
            misc.AddSeparator();
            misc.AddGroupLabel("Auto Level Up Spells");
            misc.Add("autolevel", new CheckBox("Activate Auto Level Up Spells", true));
            misc.AddSeparator();
            misc.AddGroupLabel("Skin Hack");
            misc.Add("skinhax", new CheckBox("Activate Skin hack", true));
            misc.Add("skinID", new ComboBox("Skin Hack", 3, "Default", "Royal Guard", "Nightraven", "Headmistress", "PROJECT:"));

            fleee.AddGroupLabel("Spells/Items To Use On Flee Mode");
            fleee.Add("QFLEE", new CheckBox("Use Q To Flee", true));
            fleee.AddLabel("(Casts To Mouse Position)",1);
            fleee.AddSeparator();
            fleee.Add("YOMUSFLEE", new CheckBox("Use Youmuu's Ghostblade While In Flee Mode",true));
            fleee.AddSeparator();
            fleee.Add("FLEEMIN", new Slider("Min Enemies In Range To Flee", 0, 0, 5));         

            pred.AddGroupLabel("Prediction");
            pred.AddLabel("Q :");
            pred.Add("QPREDMODE", new ComboBox("Use Prediction For Q", 0, "On", "Off"));
            pred.Add("QPred", new Slider("Select % Hitchance", 90, 1, 100));
            pred.AddSeparator();
            pred.AddLabel("W :");
            pred.Add("WPREDMODE", new ComboBox("Use Prediction For W", 0, "On", "Off"));
            pred.Add("WPred", new Slider("Select % Hitchance", 80, 1, 100));
        }
    }
    public static class DemSpells
    {
        public static Spell.Skillshot Q { get; private set; }
        public static Spell.Skillshot W { get; private set; }
        public static Spell.Active E { get; private set; }
        public static Spell.Targeted R { get; private set; }

        static DemSpells()
        {
            Q = new Spell.Skillshot(SpellSlot.Q, 750, SkillShotType.Linear, 250, 500, 0);
            W = new Spell.Skillshot(SpellSlot.W, 750, SkillShotType.Linear, 500, 3200, 70);
            E = new Spell.Active(SpellSlot.E , 175);
            E.CastDelay = 0;

            R = new Spell.Targeted(SpellSlot.R,500);
            R.CastDelay = (int).066f;
        }
    }
}