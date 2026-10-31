using System;
using UnityEngine;

namespace NightCourier
{
    [Serializable]
    public sealed class CourierCharacter
    {
        [Tooltip("Stable stop ID from Stops.csv. Reordering this list does not change the mapping.")]
        [Range(0, 5)] public int stopId;
        public string id;
        public string displayName;
        [TextArea(2, 4)] public string personality;
        public Sprite portrait;
        public Color accent = new Color(.45f, .94f, .77f);
    }

    [CreateAssetMenu(menuName = "Night Courier/Character Catalog")]
    public sealed class CourierCharacterCatalog : ScriptableObject
    {
        public Sprite fallbackPortrait;
        public CourierCharacter[] characters = new CourierCharacter[0];

        public CourierCharacter ForStop(int stopId)
        {
            if (characters != null)
                foreach (var character in characters)
                    if (character != null && character.stopId == stopId) return character;
            return null;
        }

        public Sprite PortraitFor(CourierCharacter character)
        {
            return character != null && character.portrait != null ? character.portrait : fallbackPortrait;
        }
    }
}
