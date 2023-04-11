using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using ClassicUO.AiEngine.AiEngineTasks;
using ClassicUO.Game.AiEngine.Helpers;
using ClassicUO.Game.AiEngine.Memory;
using ClassicUO.Game.Data;
using ClassicUO.Game.Enums;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Managers;
using ClassicUO.Game.UI.Gumps;

namespace ClassicUO.Game.AiEngine.Tasks {
    public class SelfBandageAiTask : BaseAITask {
        private Stopwatch _bandageTimer = Stopwatch.StartNew();
        private Mobile _lastHealedPet = null;
        private bool _hadBuff = false;

        public SelfBandageAiTask() : base("Self Bandage Task")
        {
        }

        public override int Priority() {
            if (World.Player == null) {
                return 0;
            }
            return 100-World.Player.HitsPercentage;
        }

        private bool HasHealingBuff() {
            var buffIcons = World.Player.BuffIcons;
            foreach (var buff in buffIcons)
            {
                if (buff.Key == BuffIconType.Healing && buff.Value is { Graphic: 30102 })
                {
                    return true;
                }
            }

            if (buffIcons.ContainsKey(BuffIconType.Healing))
            {
                return true;
            }

            return false;
        }

        private bool HasVetHealingBuff()
        {
            var buffIcons = World.Player.BuffIcons;
            foreach (var buff in buffIcons)
            {
                if (buff.Key == BuffIconType.Veterinary)
                {
                    return true;
                }
            }

            if (buffIcons.ContainsKey(BuffIconType.Veterinary))
            {
                return true;
            }

            return false;
        }

        private bool ShouldHealSelf() {
            return Player.HitsPercentage <= AiSettings.Instance.SelfHealPercent || Player.IsPoisoned;
        }

        private Mobile GetMostDamagedPet() {
            Mobile bestPet = null;
            var bestPetHealth = AiSettings.Instance.PetHealPercent;

            foreach (var pet in MobilesMemory.Instance.Pets) {
                var foundPet = World.Get(pet.Serial);

                if (foundPet == null) {
                    continue;
                }

                if (pet.HitsPercentage <= bestPetHealth) {
                    bestPetHealth = pet.HitsPercentage;
                    bestPet = pet;
                }
            }

            if (bestPet == null) {
                bestPet = MobilesMemory.Instance.Pets.Where(d => d.IsPoisoned).OrderBy(d => d.Distance).FirstOrDefault();
            }

            if (bestPet != null) {
                var pet = World.Get(bestPet.Serial);

                if (pet != null && pet is Mobile mobile) {
                    return mobile;
                }
            }

            return bestPet;
        }

        private async Task<bool> PulseSelfHealing() {
            if (!ShouldHealSelf()) {
                return false;
            }

            GameActions.Say("[bandself");

            if (await WaitForHelper.WaitFor(() => HasHealingBuff(), 1000)) {
                return true;
            }

            return false;
        }

        private async Task<bool> PulseHealPet(Mobile pet) {
            _lastHealedPet = pet;

            if (_lastHealedPet.Distance > 1) {
                if (AiSettings.Instance.AllowPetMovementControl) {
                    await PopupCommandPet(pet, 2);
                    await WaitForHelper.WaitFor(() => TargetManager.IsTargeting, 2000);

                    if (TargetManager.IsTargeting) {
                        TargetManager.Target(Player.Serial);
                    }

                    await WaitForHelper.WaitFor(() => _lastHealedPet.Distance <= 1, 5000);
                }
                else if (_lastHealedPet.Distance < 7 && Player.Mana > 20) {
                    var startHits = pet.Hits;
                    await CastSpellOnMobile(SpellConsts.Greaterheal, pet, 1000, true, 2000, () => pet.Hits > startHits);
                }

                return true;
            }


            if (pet.IsPoisoned) {
                if (GetSkill("Magery").Value < 60 || Player.Mana < 25) {
                    return true;
                }

                await CastSpellOnMobile(SpellConsts.Archcure, pet);
                return true;
            }

            var bandage = World.Player.FindBandage();

            if (bandage != null) {
                GameActions.DoubleClick(bandage);
                await WaitForHelper.WaitFor(() => TargetManager.IsTargeting, 2000);

                if (TargetManager.IsTargeting) {
                    TargetManager.Target(pet.Serial);
                    await WaitForHelper.WaitFor(() => HasVetHealingBuff(), 1000);
                }

                return true;
            }

            return false;
        }

        private async Task<bool> PopupCommandPet(Mobile pet, ushort index) {
            GameActions.OpenPopupMenu(pet.Serial);
            await WaitForHelper.WaitFor(() => GumpHelper.GetPopupMenu() != null, 1000);
            var popup = GumpHelper.GetPopupMenu();

            if (popup != null)
            {
                GameActions.ResponsePopupMenu(pet.Serial, index); //3 is guard, 2 is follow
                await Task.Delay(100);
                AiCore.GumpsToClose.Push(popup);
                await Task.Delay(500);
            }
            return true;
        }

        public override async Task<bool> Pulse() {
            try {
                if (!AiSettings.Instance.SelfBandageHealing || _bandageTimer.ElapsedMilliseconds < 1000) {
                    return false;
                }

                if (HasHealingBuff() || HasVetHealingBuff()) {
                    _hadBuff = true;
                    return false;
                }

                if (_hadBuff) {
                    _hadBuff = false;
                    await Task.Delay(900);
                }

                if (await PulseSelfHealing()) {
                    _bandageTimer = Stopwatch.StartNew();

                    return true;
                }

                var mostDamagedPet = GetMostDamagedPet();

                if (_lastHealedPet != null && mostDamagedPet != _lastHealedPet && AiSettings.Instance.AllowPetMovementControl) {
                    var preCastTime = DateTime.Now;
                    await PopupCommandPet(_lastHealedPet, 3); // Set the pet to guard
                    await Task.Delay(100);
                    var entriesAfterTime = JournalHelper.GetJournalEntriesContaining("guarding you", preCastTime);

                    if (entriesAfterTime.Count > 0) {
                        _lastHealedPet = null;
                    }

                    return true;
                }

                if (mostDamagedPet != null) {
                    if (await PulseHealPet(mostDamagedPet)) {
                        return true;
                    }
                }
            }
            catch (Exception e) {
                return false;
            }


            return true;
        }
    }
}