using System;
using System.Collections.Generic;
using UnityEngine;

namespace PassportFrenzy
{
    /// <summary>Named sound hooks. Clips may be null (silent placeholder). See <see cref="SoundIds"/>.</summary>
    [CreateAssetMenu(menuName = "Passport Frenzy/Sound Library", fileName = "SoundLibrary")]
    public class SoundLibrary : ScriptableObject
    {
        [Serializable]
        public class Entry
        {
            public string id;
            public AudioClip clip;
            [Range(0f, 1f)] public float volume = 1f;
        }

        public List<Entry> entries = new List<Entry>();

        public Entry Find(string id)
        {
            foreach (var e in entries) if (e != null && e.id == id) return e;
            return null;
        }
    }

    /// <summary>Sound hook ids. Each has an entry in SoundLibrary.asset; drop a clip in to hear it.</summary>
    public static class SoundIds
    {
        public const string Jump = "jump";
        public const string Hit = "hit";
        public const string Block = "block";
        public const string ShieldBreak = "shield_break";
        public const string Photo = "photo";
        public const string Pickup = "pickup";
        public const string UseItem = "use_item";
        public const string Booking = "booking";
        public const string PhoneOpen = "phone_open";
        public const string Mash = "mash";
        public const string Stamp = "stamp";
        public const string Rejected = "rejected";
        public const string Buy = "buy";
        public const string Takeoff = "takeoff";
        public const string FlightMissed = "flight_missed";
        public const string VisaOverstay = "visa_overstay";
        public const string Win = "win";
        /// <summary>Music entries are named "music_&lt;countryId&gt;", e.g. music_jp.</summary>
        public const string MusicPrefix = "music_";

        public static readonly string[] All =
        {
            Jump, Hit, Block, ShieldBreak, Photo, Pickup, UseItem, Booking, PhoneOpen,
            Mash, Stamp, Rejected, Buy, Takeoff, FlightMissed, VisaOverstay, Win,
        };
    }
}
