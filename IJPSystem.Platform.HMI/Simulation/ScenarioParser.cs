using IJPSystem.Platform.HMI.Simulation.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace IJPSystem.Platform.HMI.Simulation
{
    public static class ScenarioParser
    {
        // snake_case JSON 키를 PascalCase C# 프로퍼티로 매핑
        private static readonly JsonSerializerOptions Options = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true,
        };

        public static ScenarioDef Load(string path)
        {
            using var stream = File.OpenRead(path);
            return JsonSerializer.Deserialize<ScenarioDef>(stream, Options)
                ?? throw new InvalidDataException($"빈 시나리오 파일: {path}");
        }

        public static IReadOnlyList<(string Path, ScenarioDef Def)> LoadAll(string folder)
        {
            if (!Directory.Exists(folder))
                return Array.Empty<(string, ScenarioDef)>();

            return Directory.EnumerateFiles(folder, "*.json")
                            .OrderBy(p => p)
                            .Select(p => (p, Load(p)))
                            .ToList();
        }
    }
}
