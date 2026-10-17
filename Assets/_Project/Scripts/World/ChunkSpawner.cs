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

        public SpawnMode Mode => _mode;
        /// <summary>World X of the gate trigger once the gate chunk is spawned, else NaN.</summary>
        public float GateX { get; private set; } = float.NaN;
        public float ChaseStartX { get; private set; } = float.NaN;
        public float PlannedGateX => float.IsNaN(GateX) ? ChaseStartX + _plannedChaseLength : GateX;
        float _plannedChaseLength;

        CountryDatabase Db => GameRoot.I.countries;
        GameTuning T => GameRoot.I.tuning;
        Transform Parent => chunkParent != null ? chunkParent : transform;

        public void ResetWorld(SpawnMode mode, CountryDefinition country, int tier, Chunk firstChunk = null)
        {
            foreach (var c in _active) if (c != null) Destroy(c.gameObject);
            _active.Clear();
            _forced.Clear();
            _nextX = startX;
            GateX = float.NaN;
            ChaseStartX = float.NaN;
            _chasePending = false;
            SetMode(mode, country, tier);
            if (firstChunk != null) EnqueueOnce(firstChunk);
            FillAhead(0f);
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
                case SpawnMode.Country: return PickWeighted(Db.ChunkPoolFor(_country));
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

            if (_mode == SpawnMode.Chase && chunk.kind == ChunkKind.AirportChase) _chaseRemaining -= chunk.Length;
            if (chunk.kind == ChunkKind.Gate)
            {
                var trigger = chunk.GetComponentInChildren<FlowTrigger>();
                GateX = trigger != null ? trigger.transform.position.x : _nextX + chunk.Length * 0.5f;
            }
            _nextX += chunk.Length;
            return chunk;
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
