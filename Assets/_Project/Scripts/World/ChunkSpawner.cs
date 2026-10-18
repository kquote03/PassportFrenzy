using System.Collections.Generic;
using UnityEngine;

namespace PassportFrenzy
{
    public enum SpawnMode { Home, Country, AirportSafe, Chase }

    /// <summary>
    /// Streams chunk prefabs ahead of the player and destroys them behind.
    /// GameFlow switches the mode; one-off chunks (door, airport entrance, gate...) are queued with EnqueueOnce.
    /// </summary>
    public class ChunkSpawner : MonoBehaviour
    {
        [SerializeField] Transform chunkParent;
        [Tooltip("World X where the first chunk starts after a reset (player starts at 0).")]
        [SerializeField] float startX = -10f;

        readonly List<Chunk> _active = new List<Chunk>();
        readonly Queue<Chunk> _forced = new Queue<Chunk>();
        readonly System.Random _rng = new System.Random();

        float _nextX;
        SpawnMode _mode;
        CountryDefinition _palette;   // whose colors
        CountryDefinition _country;   // whose festivals/obstacles
        int _tier = 1;
        float _chaseRemaining;
        bool _chasePending;
        bool _warnedEmpty;

        // Country run: fixed length ending in the airport entrance, with planned festivals.
        float _countryStartX, _countryLength;
        readonly List<FestivalSlot> _festivals = new List<FestivalSlot>();
        int _festivalIndex;
        string _pendingFestivalName;

        public SpawnMode Mode => _mode;
        /// <summary>World X of the gate trigger once the gate chunk is spawned, else NaN.</summary>
        /// <summary>World X where a reset world's first chunk starts (nothing exists to the left of it).</summary>
        public float StartX => startX;
        public float GateX { get; private set; } = float.NaN;
        public float ChaseStartX { get; private set; } = float.NaN;
        public float PlannedGateX => float.IsNaN(GateX) ? ChaseStartX + _plannedChaseLength : GateX;
        /// <summary>World X of the airport entrance trigger: exact once spawned, planned before that (NaN outside a country run).</summary>
        public float AirportX => !float.IsNaN(_airportX) ? _airportX
            : _countryLength > 0f ? _countryStartX + _countryLength : float.NaN;
        float _airportX = float.NaN;
        float _plannedChaseLength;

        CountryDatabase Db => GameRoot.I.countries;
        GameTuning T => GameRoot.I.tuning;
        Transform Parent => chunkParent != null ? chunkParent : transform;

        /// <summary>
        /// Start a country run of a fixed world length: random country chunks (+ the planned festivals at their
        /// distances), then the airport entrance at the end. The visa timer is the deadline to get there.
        /// </summary>
        public void BeginCountry(CountryDefinition country, int tier, float length, List<FestivalSlot> festivals, Chunk firstChunk,
            float playerStartX = 0f)
        {
            ResetWorld(SpawnMode.Country, country, tier, firstChunk, fill: false);
            _countryStartX = playerStartX;
            _countryLength = length;
            if (festivals != null) _festivals.AddRange(festivals);
            _festivals.Sort((a, b) => a.distance.CompareTo(b.distance));
            FillAhead(0f);
        }

        public void ResetWorld(SpawnMode mode, CountryDefinition country, int tier, Chunk firstChunk = null, bool fill = true)
        {
            foreach (var c in _active) if (c != null) Destroy(c.gameObject);
            _active.Clear();
            _forced.Clear();
            _nextX = startX;
            GateX = float.NaN;
            ChaseStartX = float.NaN;
            _chasePending = false;
            _countryLength = 0f;
            _airportX = float.NaN;
            _festivals.Clear();
            _festivalIndex = 0;
            _pendingFestivalName = null;
            SetMode(mode, country, tier);
            if (firstChunk != null) EnqueueOnce(firstChunk);
            if (fill) FillAhead(0f);
        }

        public void SetMode(SpawnMode mode, CountryDefinition country, int tier)
        {
            _mode = mode;
            _palette = country;
            _country = country;
            _tier = tier;
        }

        public void SetMode(SpawnMode mode) => _mode = mode;

        public void EnqueueOnce(Chunk prefab)
        {
            if (prefab != null) _forced.Enqueue(prefab);
        }

        /// <summary>Destroys chunks that start beyond playerX + keepDistance so queued chunks arrive sooner.</summary>
        public void TrimAhead(float keepDistance)
        {
            float px = PlayerX;
            for (int i = _active.Count - 1; i >= 0; i--)
            {
                var c = _active[i];
                if (c == null || c.transform.position.x <= px + keepDistance) break;
                Destroy(c.gameObject);
                _active.RemoveAt(i);
                _nextX = c.transform.position.x;
            }
        }

        /// <summary>Chase uses the destination's tier but the current airport's palette.</summary>
        public void BeginChase(CountryDefinition airportCountry, int destinationTier, float terminalLength)
        {
            _mode = SpawnMode.Chase;
            _palette = airportCountry;
            _country = airportCountry;
            _tier = destinationTier;
            _chaseRemaining = terminalLength;
            _plannedChaseLength = terminalLength;
            _chasePending = true;
            GateX = float.NaN;
            ChaseStartX = _nextX;
        }

        float PlayerX => GameRoot.I.player != null ? GameRoot.I.player.transform.position.x : 0f;

        void Update()
        {
            if (GameRoot.I == null) return;
            FillAhead(PlayerX);

            float behind = PlayerX - T.despawnBehindDistance;
            while (_active.Count > 0 && _active[0] != null && _active[0].transform.position.x + _active[0].Length < behind)
            {
                Destroy(_active[0].gameObject);
                _active.RemoveAt(0);
            }
            _active.RemoveAll(c => c == null);
        }

        void FillAhead(float px)
        {
            int guard = 0;
            while (_nextX < px + T.spawnAheadDistance && guard++ < 32) SpawnNext();
        }

        void SpawnNext()
        {
            Chunk prefab = _forced.Count > 0 ? _forced.Dequeue() : PickForMode();
            if (prefab == null)
            {
                prefab = Db.airportSafeChunk;
                if (!_warnedEmpty) { Debug.LogWarning($"[ChunkSpawner] No chunk for mode {_mode}; using safe chunk."); _warnedEmpty = true; }
                if (prefab == null) { _nextX += 10f; return; }
            }
            Spawn(prefab);
        }

        Chunk PickForMode()
        {
            switch (_mode)
            {
                case SpawnMode.Home: return Db.homeLobbyChunk;
                case SpawnMode.Country:
                {
                    if (_countryLength <= 0f) return PickWeighted(Db.ChunkPoolFor(_country));
                    // The entrance trigger must sit exactly countryLength from the player's start (visa deadline fairness),
                    // so whole chunks are placed only while they fit, and short fillers close the remaining gap.
                    float entranceStart = _countryStartX + _countryLength - EntranceTriggerOffset();
                    float gap = entranceStart - _nextX;
                    var filler = Db.fillerChunk;
                    if (gap < (filler != null ? filler.Length * 0.5f : 0.5f))
                    {
                        _mode = SpawnMode.AirportSafe; // after the entrance: obstacle-free lobby
                        return Db.airportEntranceChunk;
                    }
                    float d = _nextX - _countryStartX;
                    if (_festivalIndex < _festivals.Count)
                    {
                        var slot = _festivals[_festivalIndex];
                        // Place it at its planned distance, or earlier if the run is about to run out of room for it.
                        bool due = d >= slot.distance || gap < slot.chunk.Length * 2f;
                        if (due && slot.chunk.Length <= gap)
                        {
                            _festivalIndex++;
                            _pendingFestivalName = slot.festivalName;
                            return slot.chunk;
                        }
                    }
                    var pick = PickWeighted(Db.ChunkPoolFor(_country));
                    if (pick != null && pick.Length <= gap) return pick;
                    if (filler != null) return filler;
                    _mode = SpawnMode.AirportSafe;
                    return Db.airportEntranceChunk;
                }
                case SpawnMode.Chase:
                    if (_chaseRemaining > 0f) return PickWeighted(Db.AirportPoolFor(_country));
                    if (_chasePending) { _chasePending = false; return Db.gateChunk; }
                    return Db.airportSafeChunk;
                default: return Db.airportSafeChunk;
            }
        }

        Chunk PickWeighted(IList<WeightedChunk> pool)
        {
            var w = WeightedPicker.Pick(pool, c => c != null && c.prefab != null ? c.weight : 0f, _rng.NextDouble());
            return w?.prefab;
        }

        Chunk Spawn(Chunk prefab)
        {
            var chunk = Instantiate(prefab, new Vector3(_nextX, 0f, 0f), Quaternion.identity, Parent);
            chunk.OnSpawned(new SpawnContext { palette = _palette, country = _country, tier = _tier });
            _active.Add(chunk);
            if (_pendingFestivalName != null)
            {
                foreach (var z in chunk.GetComponentsInChildren<FestivalZone>(true)) z.SetFestivalName(_pendingFestivalName);
                Debug.Log($"[Festival] {_pendingFestivalName} at {_nextX - _countryStartX:0} m");
                _pendingFestivalName = null;
            }
            foreach (var trig in chunk.GetComponentsInChildren<FlowTrigger>(true))
                if (trig.id == FlowTriggerId.AirportEntrance) _airportX = trig.transform.position.x;

            if (_mode == SpawnMode.Chase && chunk.kind == ChunkKind.AirportChase) _chaseRemaining -= chunk.Length;
            if (chunk.kind == ChunkKind.Gate)
            {
                var trigger = chunk.GetComponentInChildren<FlowTrigger>();
                GateX = trigger != null ? trigger.transform.position.x : _nextX + chunk.Length * 0.5f;
            }
            _nextX += chunk.Length;
            return chunk;
        }

        float EntranceTriggerOffset()
        {
            var entrance = Db.airportEntranceChunk;
            if (entrance == null) return 0f;
            var trig = entrance.GetComponentInChildren<FlowTrigger>(true);
            return trig != null ? trig.transform.localPosition.x : entrance.Length * 0.5f;
        }

        /// <summary>Debug: drop a festival zone a little ahead of the player.</summary>
        public void SpawnFestivalAhead(float distance)
        {
            var prefab = Db.FestivalPrefabFor(_country);
            if (prefab == null) return;
            var zone = Instantiate(prefab, new Vector3(PlayerX + distance, 0f, 0f), Quaternion.identity, Parent);
            zone.SetCountry(_country);
            foreach (var p in zone.GetComponentsInChildren<PaletteTint>(true)) p.Apply(_palette);
            Destroy(zone.gameObject, 30f);
        }
    }
}
