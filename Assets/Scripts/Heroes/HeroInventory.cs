using LaneSurvivor.Save;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace LaneSurvivor.Heroes
{
    public static class HeroInventory
    {
        public static bool OwnsHero(SaveGameData data, string heroId)
        {
            // Normalize ensures the owned list exists and removes empty ids.
            data?.Normalize();
            return data != null && data.ownedHeroIds.Contains(heroId);
        }

        public static bool GrantHero(SaveGameData data, string heroId)
        {
            // Only catalog heroes can be granted into the local inventory.
            if (data == null || HeroCatalog.GetById(heroId) == null)
            {
                return false;
            }

            // Normalize before mutation so migrated saves have a valid owned list.
            data.Normalize();
            if (data.ownedHeroIds.Contains(heroId))
            {
                return false;
            }

            // Store only ids in the save so definitions can remain data-driven later.
            data.ownedHeroIds.Add(heroId);
            return true;
        }

        public static bool EquipHero(SaveGameData data, string heroId)
        {
            // Equipping requires ownership so corrupted saves cannot activate unearned heroes.
            if (!OwnsHero(data, heroId))
            {
                return false;
            }

            // Save the equipped id directly; Normalize will clear it if ownership is later removed.
            data.equippedHeroId = heroId;
            return true;
        }

        public static IReadOnlyList<HeroDefinition> GetOwnedHeroes(SaveGameData data)
        {
            // Normalize before reading so older saves have a valid, de-duplicated owned list.
            data?.Normalize();
            if (data == null)
            {
                return new List<HeroDefinition>();
            }

            // Map saved ids back to catalog definitions and ignore unknown ids from corrupted saves.
            return data.ownedHeroIds
                .Select(HeroCatalog.GetById)
                .Where(definition => definition != null)
                .ToList();
        }

        public static HeroDefinition GetEquippedHero(SaveGameData data)
        {
            // A null or empty equipped id means no hero stat bonus should apply.
            if (data == null || string.IsNullOrWhiteSpace(data.equippedHeroId))
            {
                return null;
            }

            // Ownership is required even when a catalog definition exists.
            return OwnsHero(data, data.equippedHeroId)
                ? HeroCatalog.GetById(data.equippedHeroId)
                : null;
        }

        public static int GetStartingSquadBonus(SaveGameData data)
        {
            // Starting squad bonuses are additive with the HQ bonus.
            HeroDefinition hero = GetEquippedHero(data);
            return Mathf.Max(0, hero?.startingSquadBonus ?? 0);
        }

        public static float GetDamageBonus(SaveGameData data)
        {
            // Damage bonuses are flat additions to the level's starting damage per squad member.
            HeroDefinition hero = GetEquippedHero(data);
            return Mathf.Max(0f, hero?.damageBonus ?? 0f);
        }
    }
}
