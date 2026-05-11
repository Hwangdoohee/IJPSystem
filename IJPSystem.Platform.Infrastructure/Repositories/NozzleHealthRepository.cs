using Dapper;
using IJPSystem.Platform.Common.Constants;
using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.IO;

namespace IJPSystem.Platform.Infrastructure.Repositories
{
    /// <summary>
    /// Drop Watcher 노즐 검사 결과를 시계열로 누적.
    /// SPC 차트(Health Rate 추세 + UCL/LCL) 와 노즐별 수명 분석에 사용.
    /// </summary>
    public static class NozzleHealthRepository
    {
        private const int RetentionDays = 90;   // 3개월 보관

        private static readonly object _initLock = new();
        private static bool _initialized;
        private static string _connStr = string.Empty;

        private static void EnsureInitialized()
        {
            if (_initialized) return;
            lock (_initLock)
            {
                if (_initialized) return;

                if (!Directory.Exists(AppConstants.LogFolder))
                    Directory.CreateDirectory(AppConstants.LogFolder);
                string path = Path.Combine(AppConstants.LogFolder, "NozzleHealth.db");
                _connStr = $"Data Source={path}";

                using var conn = new SqliteConnection(_connStr);
                conn.Open();
                conn.Execute(@"
                    CREATE TABLE IF NOT EXISTS InspectionSnapshot (
                        Id            INTEGER PRIMARY KEY AUTOINCREMENT,
                        Time          DATETIME NOT NULL,
                        RecipeName    TEXT,
                        NozzleCount   INTEGER NOT NULL,
                        GoodCount     INTEGER NOT NULL,
                        WeakCount     INTEGER NOT NULL,
                        MissingCount  INTEGER NOT NULL,
                        HealthPct     REAL    NOT NULL,
                        Score         REAL    NOT NULL,
                        IsPass        INTEGER NOT NULL
                    );
                    CREATE INDEX IF NOT EXISTS IX_InspectionSnapshot_Time ON InspectionSnapshot(Time);

                    CREATE TABLE IF NOT EXISTS InspectionDetail (
                        SnapshotId   INTEGER NOT NULL,
                        NozzleIndex  INTEGER NOT NULL,
                        State        INTEGER NOT NULL,
                        PRIMARY KEY (SnapshotId, NozzleIndex)
                    );");

                // 보관 기간 초과 자동 정리
                var cutoff = DateTime.Now.AddDays(-RetentionDays);
                conn.Execute(@"
                    DELETE FROM InspectionDetail WHERE SnapshotId IN
                        (SELECT Id FROM InspectionSnapshot WHERE Time < @cutoff);
                    DELETE FROM InspectionSnapshot WHERE Time < @cutoff;",
                    new { cutoff });

                _initialized = true;
            }
        }

        /// <summary>검사 결과 1건 저장. 실패해도 예외 던지지 않음 (txt 로그가 fallback).</summary>
        /// <param name="nozzleStates">키=노즐번호(1-based), 값=상태(0=Unknown,1=Good,2=Weak,3=Missing)</param>
        public static long Save(DateTime time, string? recipeName, int nozzleCount,
                                int good, int weak, int missing, double score, bool isPass,
                                IDictionary<int, int> nozzleStates)
        {
            try
            {
                EnsureInitialized();
                using var conn = new SqliteConnection(_connStr);
                conn.Open();
                using var trans = conn.BeginTransaction();

                double healthPct = nozzleCount == 0 ? 0 : good * 100.0 / nozzleCount;

                long snapshotId = conn.QuerySingle<long>(@"
                    INSERT INTO InspectionSnapshot
                        (Time, RecipeName, NozzleCount, GoodCount, WeakCount, MissingCount, HealthPct, Score, IsPass)
                    VALUES
                        (@time, @recipeName, @nozzleCount, @good, @weak, @missing, @healthPct, @score, @isPass);
                    SELECT last_insert_rowid();",
                    new { time, recipeName, nozzleCount, good, weak, missing, healthPct, score, isPass = isPass ? 1 : 0 },
                    trans);

                foreach (var kv in nozzleStates)
                {
                    conn.Execute(
                        "INSERT INTO InspectionDetail (SnapshotId, NozzleIndex, State) VALUES (@id, @idx, @state)",
                        new { id = snapshotId, idx = kv.Key, state = kv.Value },
                        trans);
                }

                trans.Commit();
                return snapshotId;
            }
            catch
            {
                return -1;
            }
        }

        // ── 조회 ────────────────────────────────────────────────────────────

        public class Snapshot
        {
            public long     Id           { get; set; }
            public DateTime Time         { get; set; }
            public string?  RecipeName   { get; set; }
            public int      NozzleCount  { get; set; }
            public int      GoodCount    { get; set; }
            public int      WeakCount    { get; set; }
            public int      MissingCount { get; set; }
            public double   HealthPct    { get; set; }
            public double   Score        { get; set; }
            public int      IsPass       { get; set; }
        }

        /// <summary>최근 N건 (시간 오름차순 — 차트 X축에 그대로 사용 가능)</summary>
        public static IReadOnlyList<Snapshot> GetRecent(int count = 200)
        {
            try
            {
                EnsureInitialized();
                using var conn = new SqliteConnection(_connStr);
                var rows = conn.Query<Snapshot>(@"
                    SELECT * FROM (
                        SELECT * FROM InspectionSnapshot ORDER BY Time DESC LIMIT @count
                    ) ORDER BY Time ASC;", new { count });
                return new List<Snapshot>(rows);
            }
            catch
            {
                return Array.Empty<Snapshot>();
            }
        }

        /// <summary>특정 노즐 1개의 상태 변화 이력 (수명 분석용)</summary>
        public static IReadOnlyList<(DateTime Time, int State)> GetNozzleHistory(int nozzleIndex, int maxRows = 200)
        {
            try
            {
                EnsureInitialized();
                using var conn = new SqliteConnection(_connStr);
                var rows = conn.Query<(DateTime Time, int State)>(@"
                    SELECT s.Time, d.State
                    FROM InspectionDetail d
                    JOIN InspectionSnapshot s ON s.Id = d.SnapshotId
                    WHERE d.NozzleIndex = @idx
                    ORDER BY s.Time ASC
                    LIMIT @max;", new { idx = nozzleIndex, max = maxRows });
                return new List<(DateTime, int)>(rows);
            }
            catch
            {
                return Array.Empty<(DateTime, int)>();
            }
        }
    }
}
