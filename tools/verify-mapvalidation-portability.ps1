<#
    验证 MapValidation 模块的「可移植性」：
    把 Scripts/MapValidation/ 里**除适配器以外**的文件原样拷到一个空项目，
    用一个 3 节点的玩具图跑一遍 —— 不需要 Godot 就能跑通，就说明这个目录能直接带走。

    用法：
      powershell -ExecutionPolicy Bypass -File tools\verify-mapvalidation-portability.ps1
#>
param(
    [string]$TempRoot = (Join-Path $env:TEMP "MapValidationPortability")
)

$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent $PSScriptRoot
$srcDir = Join-Path $repoRoot 'Scripts\MapValidation'

if (-not (Test-Path $srcDir)) {
    Write-Error "找不到模块目录: $srcDir"
    exit 1
}

Write-Host "=== MapValidation 可移植性验证 ==="
Write-Host "源目录: $srcDir"
Write-Host "临时项目: $TempRoot"
Write-Host ""

# ---------- 1. 拷贝模块（排除依赖项目类型的适配器） ----------
if (Test-Path $TempRoot) {
    Remove-Item -Recurse -Force $TempRoot
}
New-Item -ItemType Directory -Force -Path $TempRoot | Out-Null
$dstDir = Join-Path $TempRoot 'MapValidation'
New-Item -ItemType Directory -Force -Path $dstDir | Out-Null

$copied = @()
Get-ChildItem $srcDir -Filter *.cs | ForEach-Object {
    if ($_.Name -eq 'HexMapGraph.cs') { return }   # 适配器依赖项目内的 HexMap，不属于可移植部分
    Copy-Item $_.FullName -Destination $dstDir -Force
    $copied += $_.Name
}
Write-Host "已拷贝 $($copied.Count) 个源文件: $($copied -join ', ')"

# ---------- 2. 检查有没有 Godot / 项目内依赖 ----------
$dirty = @()
Get-ChildItem $dstDir -Filter *.cs | ForEach-Object {
    $text = Get-Content $_.FullName -Raw
    if ($text -match 'using\s+Godot' -or $text -match 'FishEatFish') {
        $dirty += $_.Name
    }
}
if ($dirty.Count -gt 0) {
    Write-Error "以下文件含 Godot / 项目内依赖，模块不干净: $($dirty -join ', ')"
    exit 1
}
Write-Host "依赖检查: 无 Godot / 项目内依赖 OK"
Write-Host ""

# ---------- 3. 生成空项目 ----------
@'
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net8.0</TargetFramework>
    <Nullable>disable</Nullable>
    <ImplicitUsings>disable</ImplicitUsings>
    <AssemblyName>MapValidationPortability</AssemblyName>
  </PropertyGroup>
</Project>
'@ | Set-Content -Path (Join-Path $TempRoot 'MapValidationPortability.csproj') -Encoding UTF8

# ---------- 4. 玩具图 + 断言 ----------
@'
using System;
using System.Collections.Generic;
using MapValidation;

public static class Program
{
    private static int _failed;
    private static int _total;

    public static int Main()
    {
        Console.WriteLine("=== 空项目里跑 MapValidation（无 Godot）===");

        // 3 节点直线图：必须零告警、可通关
        ToyGraph ok = ToyGraph.Build(3);
        ok.Connect(0, 1, MapEdgeKind.Normal);
        ok.Connect(1, 2, MapEdgeKind.Normal);
        MapValidationReport okReport = new MapValidator().Validate(ok);
        Check("3 节点图零告警", okReport.IsValid, okReport.ToString());
        Check("3 节点图可通关", okReport.CanCompleteGoal);
        Check("3 节点图只有一个簇", okReport.ClusterCount == 1, "实际 " + okReport.ClusterCount);

        // 单向边进死胡同：必须报软锁
        ToyGraph soft = ToyGraph.Build(3);
        soft.Connect(0, 1, MapEdgeKind.OneWay);
        soft.Connect(0, 2, MapEdgeKind.Normal);
        MapValidationReport softReport = new MapValidator().Validate(soft);
        Check("单向边进死胡同报软锁", softReport.CountOf(MapIssueType.SoftLock) >= 1);

        // 消耗型格子：状态空间必须能跑
        ToyGraph hole = ToyGraph.Build(4);
        hole.End = 1;
        hole.Connect(0, 1, MapEdgeKind.Normal);
        hole.Connect(0, 2, MapEdgeKind.Normal);
        hole.Connect(2, 3, MapEdgeKind.Normal);
        hole.Consumables.Add(2);
        hole.Required.Add(3);
        MapValidationReport holeReport = new MapValidator().Validate(hole);
        Check("消耗型格子场景状态空间有枚举", holeReport.ExploredStateCount > 0,
            "实际 " + holeReport.ExploredStateCount);
        Check("消耗型格子场景报状态死锁", holeReport.CountOf(MapIssueType.StateDeadlock) >= 1);

        Console.WriteLine();
        Console.WriteLine($"=== 断言结束：{_total - _failed}/{_total} 通过 ===");
        return _failed == 0 ? 0 : 1;
    }

    private static void Check(string name, bool passed, string detail = "")
    {
        _total++;
        if (!passed) _failed++;
        Console.WriteLine((passed ? "PASS  " : "FAIL  ") + name + (detail.Length > 0 ? "  -> " + detail : ""));
    }
}

internal class ToyGraph : IMapGraph
{
    private readonly List<int>[] _out;
    private readonly List<int>[] _in;
    private readonly Dictionary<long, MapEdgeKind> _kinds = new Dictionary<long, MapEdgeKind>();

    public int Start;
    public int End;
    public HashSet<int> Consumables = new HashSet<int>();
    public HashSet<int> Required = new HashSet<int>();

    public int NodeCount { get { return _out.Length; } }

    private ToyGraph(int n)
    {
        _out = new List<int>[n];
        _in = new List<int>[n];
        for (int i = 0; i < n; i++) { _out[i] = new List<int>(); _in[i] = new List<int>(); }
        Start = 0;
        End = n - 1;
    }

    public static ToyGraph Build(int n) { return new ToyGraph(n); }

    public void Connect(int from, int to, MapEdgeKind kind)
    {
        if (!_out[from].Contains(to)) _out[from].Add(to);
        if (!_in[to].Contains(from)) _in[to].Add(from);
        _kinds[((long)from << 32) | (uint)to] = kind;

        if (kind != MapEdgeKind.OneWay)
        {
            if (!_out[to].Contains(from)) _out[to].Add(from);
            if (!_in[from].Contains(to)) _in[from].Add(to);
            _kinds[((long)to << 32) | (uint)from] = kind;
        }
    }

    public IReadOnlyList<int> GetOutgoingEdges(int node) { return _out[node]; }
    public IReadOnlyList<int> GetIncomingEdges(int node) { return _in[node]; }
    public int GetStartNode() { return Start; }
    public int GetEndNode() { return End; }
    public string GetNodeLabel(int node) { return "n" + node; }
    public bool IsBlocking(int node) { return false; }
    public IReadOnlyCollection<int> GetConsumableNodes() { return Consumables; }
    public IReadOnlyCollection<int> GetRequiredNodes() { return Required; }

    public MapEdgeKind GetEdgeKind(int from, int to)
    {
        MapEdgeKind kind;
        return _kinds.TryGetValue(((long)from << 32) | (uint)to, out kind) ? kind : MapEdgeKind.Normal;
    }
}
'@ | Set-Content -Path (Join-Path $TempRoot 'Program.cs') -Encoding UTF8

# ---------- 5. 跑 ----------
Write-Host "dotnet run ..."
Push-Location $TempRoot
try {
    $output = & dotnet run --nologo 2>&1
    $exitCode = $LASTEXITCODE
}
finally {
    Pop-Location
}

$output | ForEach-Object { Write-Host $_ }

if ($exitCode -ne 0) {
    Write-Error "可移植性验证失败（退出码 $exitCode）"
    exit $exitCode
}

Write-Host ""
Write-Host "OK: MapValidation 模块可在无 Godot 的空项目里独立运行 —— 整个目录可直接拷到其他项目"
exit 0
