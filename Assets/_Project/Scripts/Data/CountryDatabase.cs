using System.Collections.Generic;
using UnityEngine;

namespace PassportFrenzy
{
    /// <summary>Registry of countries plus the shared chunks every country falls back to.</summary>
    [CreateAssetMenu(menuName = "Passport Frenzy/Country Database", fileName = "CountryDatabase")]
    public class CountryDatabase : ScriptableObject
    {
        public CountryDefinition home;
        public List<CountryDefinition> destinations = new List<CountryDefinition>();

        [Header("Demo")]
        [Tooltip("Only the demo destinations can be booked, and owning them all wins. Off = every destination.")]
        public bool demoRotationOnly = true;
        public List<CountryDefinition> demoDestinations = new List<CountryDefinition>();

        [Header("Shared fallback chunk pools")]        public WeightedChunk[] genericChunkPool;
        public WeightedChunk[] defaultAirportChasePool;

        [Header("Shared single chunks")]
        [Tooltip("Festival chunk used for planned festivals (named from the country's festivalNames).")]
        public Chunk genericFestivalChunk;
        [Tooltip("Short plain chunk used to land the airport entrance exactly at the end of a country run.")]
        public Chunk fillerChunk;
        [Tooltip("First chunk after landing in a country.")]
        public Chunk arrivalChunk;
        [Tooltip("End of the country run; its FlowTrigger opens the booking app.")]
        public Chunk airportEntranceChunk;
        [Tooltip("Obstacle-free filler while phone menus are open.")]
        public Chunk airportSafeChunk;
        [Tooltip("Spawned once when the freezone shop opens (decor only).")]
        public Chunk freezoneChunk;
        [Tooltip("End of the chase; its FlowTrigger boards the plane.")]
        public Chunk gateChunk;
        [Tooltip("Main-menu idle chunk at the home airport.")]
        public Chunk homeLobbyChunk;
        [Tooltip("Door the player runs through after pressing Play.")]
        public Chunk homeDoorChunk;

        [Header("Shared prefabs")]
        public BuffPickup pickupPrefab;
        public FestivalZone defaultFestivalZonePrefab;

        /// <summary>Destinations in play: the demo rotation when it is on (and filled in), otherwise all of them.</summary>
        public IList<CountryDefinition> ActiveDestinations =>
            demoRotationOnly && demoDestinations != null && demoDestinations.Count > 0 ? demoDestinations : destinations;

        /// <summary>Owned passports among the active destinations (a save may hold passports from outside the rotation).</summary>
        public int CountOwned(SaveData data)
        {
            int n = 0;
            foreach (var c in ActiveDestinations) if (c != null && data.Owns(c.id)) n++;
            return n;
        }

        public CountryDefinition GetById(string id)
        {
            if (home != null && home.id == id) return home;
            foreach (var c in destinations) if (c != null && c.id == id) return c;
            return null;
        }

        public IList<WeightedChunk> ChunkPoolFor(CountryDefinition c) =>
            c != null && c.chunkPool != null && c.chunkPool.Length > 0 ? c.chunkPool : genericChunkPool;

        public IList<WeightedChunk> AirportPoolFor(CountryDefinition c) =>
            c != null && c.airportChunkPool != null && c.airportChunkPool.Length > 0 ? c.airportChunkPool : defaultAirportChasePool;

        public FestivalZone FestivalPrefabFor(CountryDefinition c) =>
            c != null && c.festivalZonePrefab != null ? c.festivalZonePrefab : defaultFestivalZonePrefab;
    }
}
