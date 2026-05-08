using Dapper;
using IJPSystem.Platform.Common.Enums;
using IJPSystem.Platform.Domain.Common;
using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows.Data;
using System.Windows.Input;

namespace IJPSystem.Platform.HMI.ViewModels
{
    /// <summary>
    /// 두 레시피의 모든 파라미터/티칭 좌표를 비교해 차이를 표시.
    /// 활성 레시피 격리를 위해 RecipeViewModel 의 상태를 건드리지 않고 DB 에서 직접 조회한다.
    /// </summary>
    public class RecipeDiffViewModel : ViewModelBase
    {
        public enum Side { Left, Right }

        public class DiffEntry
        {
            public string  Category   { get; init; } = "";
            public string  Parameter  { get; init; } = "";
            public string  LeftValue  { get; init; } = "";
            public string  RightValue { get; init; } = "";
            public bool    IsDifferent { get; init; }
        }

        private readonly string _connStr;

        public ObservableCollection<string> RecipeNames { get; } = new();
        public ObservableCollection<DiffEntry> Entries { get; } = new();
        public ICollectionView FilteredEntries { get; }

        private string? _leftRecipe;
        public string? LeftRecipe
        {
            get => _leftRecipe;
            set
            {
                if (SetProperty(ref _leftRecipe, value))
                    Recompute();
            }
        }

        private string? _rightRecipe;
        public string? RightRecipe
        {
            get => _rightRecipe;
            set
            {
                if (SetProperty(ref _rightRecipe, value))
                    Recompute();
            }
        }

        private bool _showOnlyDifferences = true;
        public bool ShowOnlyDifferences
        {
            get => _showOnlyDifferences;
            set
            {
                if (SetProperty(ref _showOnlyDifferences, value))
                    FilteredEntries.Refresh();
            }
        }

        public int TotalCount      => Entries.Count;
        public int DifferenceCount => Entries.Count(e => e.IsDifferent);

        public ICommand SwapCommand    { get; }
        public ICommand RefreshCommand { get; }

        public RecipeDiffViewModel(string connectionString, string? defaultLeft = null, string? defaultRight = null)
        {
            _connStr = connectionString;

            FilteredEntries = CollectionViewSource.GetDefaultView(Entries);
            FilteredEntries.Filter = o => !ShowOnlyDifferences || (o is DiffEntry e && e.IsDifferent);

            LoadRecipeNames();

            _leftRecipe  = defaultLeft  ?? RecipeNames.FirstOrDefault();
            _rightRecipe = defaultRight ?? RecipeNames.Skip(1).FirstOrDefault() ?? _leftRecipe;

            SwapCommand    = new RelayCommand(_ =>
            {
                (LeftRecipe, RightRecipe) = (RightRecipe, LeftRecipe);
            });
            RefreshCommand = new RelayCommand(_ => { LoadRecipeNames(); Recompute(); });

            Recompute();
        }

        private void LoadRecipeNames()
        {
            RecipeNames.Clear();
            try
            {
                using var conn = new SqliteConnection(_connStr);
                var names = conn.Query<string>("SELECT Name FROM Recipes ORDER BY Name");
                foreach (var n in names) RecipeNames.Add(n);
            }
            catch { /* 무시 — 빈 목록 유지 */ }
        }

        private void Recompute()
        {
            Entries.Clear();

            if (string.IsNullOrEmpty(LeftRecipe) || string.IsNullOrEmpty(RightRecipe))
            {
                OnPropertyChanged(nameof(TotalCount));
                OnPropertyChanged(nameof(DifferenceCount));
                FilteredEntries.Refresh();
                return;
            }

            var left  = LoadRecipe(LeftRecipe);
            var right = LoadRecipe(RightRecipe);

            // ── 1. PurgeTime ──────────────────────────────────────────────
            Add("Recipe", "PurgeTime",
                left.PurgeTime?.ToString() ?? "-",
                right.PurgeTime?.ToString() ?? "-");

            // ── 2. Motor 파라미터 ──────────────────────────────────────────
            // axisNo 합집합으로 비교 — 한쪽에만 있는 축도 표시
            var axes = left.Motors.Keys.Union(right.Motors.Keys).OrderBy(a => a);
            foreach (var ax in axes)
            {
                left.Motors.TryGetValue(ax, out var lm);
                right.Motors.TryGetValue(ax, out var rm);
                CompareMotor($"Motor [{ax}]", lm, rm);
            }

            // ── 3. 티칭 좌표 ──────────────────────────────────────────────
            // (PointName, AxisName) 합집합
            var allPoints = left.Points.Keys.Union(right.Points.Keys)
                .OrderBy(k => k.Point).ThenBy(k => k.Axis);
            foreach (var k in allPoints)
            {
                left.Points.TryGetValue(k, out var lv);
                right.Points.TryGetValue(k, out var rv);
                Add($"Point [{k.Point}]", k.Axis,
                    lv == null ? "-" : $"{lv.Pos:F3}{(lv.Used ? "" : " (unused)")}",
                    rv == null ? "-" : $"{rv.Pos:F3}{(rv.Used ? "" : " (unused)")}");
            }

            OnPropertyChanged(nameof(TotalCount));
            OnPropertyChanged(nameof(DifferenceCount));
            FilteredEntries.Refresh();
        }

        private void Add(string category, string param, string l, string r)
        {
            Entries.Add(new DiffEntry
            {
                Category    = category,
                Parameter   = param,
                LeftValue   = l,
                RightValue  = r,
                IsDifferent = !string.Equals(l, r, StringComparison.Ordinal),
            });
        }

        private void CompareMotor(string category, MotorRow? l, MotorRow? r)
        {
            string F(double? v) => v.HasValue ? v.Value.ToString("F2") : "-";
            Add(category, "Move.Vel",   F(l?.MoveVel),   F(r?.MoveVel));
            Add(category, "Move.Acc",   F(l?.MoveAcc),   F(r?.MoveAcc));
            Add(category, "Move.Dec",   F(l?.MoveDec),   F(r?.MoveDec));
            Add(category, "Jog.Vel",    F(l?.JogVel),    F(r?.JogVel));
            Add(category, "Jog.Acc",    F(l?.JogAcc),    F(r?.JogAcc));
            Add(category, "Jog.Dec",    F(l?.JogDec),    F(r?.JogDec));
            Add(category, "Print.Vel",  F(l?.PrintVel),  F(r?.PrintVel));
            Add(category, "Print.Acc",  F(l?.PrintAcc),  F(r?.PrintAcc));
            Add(category, "Print.Dec",  F(l?.PrintDec),  F(r?.PrintDec));
        }

        // ── DB 로딩 헬퍼 ────────────────────────────────────────────────────
        private record MotorRow(double MoveVel, double MoveAcc, double MoveDec,
                                 double JogVel, double JogAcc, double JogDec,
                                 double PrintVel, double PrintAcc, double PrintDec);

        private record PointKey(string Point, string Axis);
        private record PointRow(double Pos, bool Used);

        private class RecipeData
        {
            public int? PurgeTime;
            public Dictionary<string, MotorRow> Motors = new();
            public Dictionary<PointKey, PointRow> Points = new();
        }

        private RecipeData LoadRecipe(string name)
        {
            var data = new RecipeData();
            try
            {
                using var conn = new SqliteConnection(_connStr);
                conn.Open();

                var row = conn.QueryFirstOrDefault("SELECT Id, PurgeTime FROM Recipes WHERE Name = @name", new { name });
                if (row == null) return data;
                long id = (long)row.Id;
                data.PurgeTime = row.PurgeTime is null ? null : (int)(long)row.PurgeTime;

                foreach (var m in conn.Query("SELECT * FROM RecipeDetails_Motor WHERE RecipeId = @id", new { id }))
                {
                    string axisNo = (string)m.AxisNo;
                    data.Motors[axisNo] = new MotorRow(
                        (double)m.MoveVel, (double)m.MoveAcc, (double)m.MoveDec,
                        (double)m.JogVel,  (double)m.JogAcc,  (double)m.JogDec,
                        (double)m.PrintVel, (double)m.PrintAcc, (double)m.PrintDec);
                }

                foreach (var p in conn.Query("SELECT * FROM RecipeDetails_Position WHERE RecipeId = @id", new { id }))
                {
                    var key = new PointKey((string)p.PointName, (string)p.AxisName);
                    bool used = p.IsUsed is null ? true : (long)p.IsUsed != 0;
                    data.Points[key] = new PointRow((double)p.PosValue, used);
                }
            }
            catch { /* 무시 */ }
            return data;
        }
    }
}
