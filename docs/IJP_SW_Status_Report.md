---
marp: true
theme: default
size: 16:9
paginate: true
header: 'IJP System — C# 기반 잉크젯 HMI 구조 및 참조 관계'
style: |
  section {
    background: #ffffff;
    font-family: 'Malgun Gothic', 'Segoe UI', sans-serif;
    padding: 36px 46px;
  }
  h1 {
    background: #F1F3F5;
    color: #1F2937;
    padding: 12px 20px;
    border-radius: 8px;
    font-size: 26px;
    display: inline-block;
    margin: 0 0 18px 0;
  }
  h2 {
    color: #1F2937;
    font-size: 17px;
    font-weight: 700;
    margin: 0 0 8px 0;
    border-bottom: 2px solid #93C5FD;
    padding-bottom: 4px;
  }
  .cols { display: grid; grid-template-columns: 1fr 1fr; gap: 14px; }
  .box {
    background: #FAFBFC;
    border: 1px solid #D1D5DB;
    border-radius: 12px;
    padding: 14px 18px;
  }
  .box-blue  { border-color: #93C5FD; background: #EFF6FF; }
  .box-green { border-color: #86EFAC; background: #F0FDF4; }
  .box-amber { border-color: #FCD34D; background: #FFFBEB; }
  ul, ol { margin: 4px 0; padding-left: 18px; font-size: 12.5px; line-height: 1.5; }
  li { margin: 2px 0; }
  code, pre { font-family: 'Cascadia Code', 'Consolas', monospace; }
  pre { font-size: 11px; line-height: 1.35; background: #1E293B; color: #E2E8F0; padding: 10px 14px; border-radius: 8px; overflow-x: auto; }
  :not(pre) > code { background:#F3F4F6; color:#1F2937; padding:1px 6px; border-radius:4px; font-size:0.92em; }
  table { width:100%; border-collapse: collapse; font-size: 11.5px; }
  th, td { padding: 4px 8px; border-bottom: 1px solid #E5E7EB; text-align: left; vertical-align: top; }
  th { background:#F3F4F6; color:#1F2937; font-size: 11px; }
  .lead { color: #2563EB; font-weight: 700; }
  .sub  { color: #6B7280; font-size: 11px; }
  .arrow { color:#2563EB; font-weight:700; }
  .footer { position:absolute; bottom:14px; left:46px; right:46px; color:#6B7280; font-size:10.5px; }
---

<!-- ═══════════════ Slide 1 : Solution / Project Structure ═══════════════ -->

# 1. 솔루션 구성 — 9 Project / One-Way Reference

<div class="cols">
<div class="box">

## 프로젝트 계층 (4 Tier)

```
[Tier 4 — Presentation]
└─ IJPSystem.Platform.HMI            (WPF, MVVM)

[Tier 3 — Composition]
└─ IJPSystem.Machines.Inkjet5G       (Machine 조립)

[Tier 2 — Application / Drivers]
├─ IJPSystem.Platform.Application    (Sequence Engine)
├─ IJPSystem.Drivers.IO              (IO HAL 구현)
├─ IJPSystem.Drivers.Motion          (Motion HAL 구현)
└─ IJPSystem.Drivers.Vision          (Vision HAL 구현)

[Tier 1 — Foundation]
├─ IJPSystem.Platform.Infrastructure (SQLite Repository)
├─ IJPSystem.Platform.Domain         (Interface / Model)
└─ IJPSystem.Platform.Common         (Utility / Constants)
```

<div class="sub">
※ <b>One-Way Reference 원칙</b> — 상위 Tier만 하위 Tier 참조. 역참조 금지.<br>
※ <b>146개 .cs</b> / <b>37개 .xaml</b> (런타임 파일 제외)
</div>

</div>
<div class="box box-blue">

## 정확한 ProjectReference 의존성

| 프로젝트 | 참조 |
|---|---|
| `Platform.Common` | **(없음)** — root |
| `Platform.Domain` | Common |
| `Platform.Infrastructure` | Common, Domain |
| `Platform.Application` | Common, Domain |
| `Drivers.IO` | Common, Domain |
| `Drivers.Motion` | Common, Domain |
| `Drivers.Vision` | Common, Domain |
| `Machines.Inkjet5G` | Domain, Application,<br>Drivers.IO/Motion/Vision |
| `Platform.HMI` | Infrastructure, Application,<br>Drivers.IO/Vision,<br>Machines.Inkjet5G |

<div class="sub">※ <b>Domain 은 어디서나 참조됨</b> — Interface/Model 의 공통 어휘 역할<br>※ HMI 가 Drivers.Motion 을 직접 참조하지 않음 → Machines.Inkjet5G 가 캡슐화</div>

</div>
</div>

<div class="footer">Build : <code>dotnet build IJPSystem.slnx</code> (.slnx 단일 솔루션) · Target : <code>net8.0-windows</code> · DB : SQLite (Dapper)</div>

---

<!-- ═══════════════ Slide 2 : HAL & Sequence Engine ═══════════════ -->

# 2. HAL 추상화 및 Sequence Engine 참조 구조

<div class="cols">
<div class="box box-green">

## HAL 인터페이스 ↔ 구현체

| Interface (`Platform.Domain.Interfaces`) | 구현체 (`Drivers.*` / `Machines.*`) |
|---|---|
| `IIODriver` | `VirtualIODriver`<br>`EtherCatIODriver` *(TODO)* |
| `IMotionDriver` | `VirtualMotionDriver` |
| `IVisionDriver` | `VirtualVisionDriver` |
| `IMachine` | `InkjetMachine` *(partial × 6)* |
| `IMotionService` | `MotionServiceAdapter` *(HMI)* |
| `INozzleClassifier` | `RandomNozzleClassifier` *(Phase 1)*<br>`OnnxNozzleClassifier` *(Phase 2)* |

```csharp
// 드라이버 교체 = 인스턴스 한 줄
IMotionDriver motion = new VirtualMotionDriver();
// → AcsMotionDriver / KomizoaMotionDriver 로 대체 가능
```

<div class="sub">※ <b>HAL = Hardware Abstraction Layer</b> — 상위 로직(Sequence/HMI)이 구현체를 알지 못함</div>

</div>
<div class="box box-amber">

## Sequence Engine (Application Layer)

```
Application/Sequences/
├─ SequenceDefinition.cs   ← 시퀀스 메타 (Name, Desc)
├─ SequenceStepDef.cs      ← 단계 정의 (Name, Action)
├─ SequenceRegistry.cs     ← 6종 시퀀스 등록
├─ WaitHelper.cs           ← IO/Motion 폴링 헬퍼
│
├─ AutoPrintSequence.cs    ← 11 step
├─ InitializeSequence.cs   ← 7 step
├─ PurgeSequence.cs        ← 6 step
├─ BlottingSequence.cs     ← 6 step
├─ MaintenanceSequence.cs  ← 6 step
└─ NJISequence.cs          ← 7 step
```

**호출 흐름 (Auto Print 예시)**

```
HMI: MainDashboardVM.RunAutoPrintAsync()
  ↓ async/await
Application: AutoPrintSequence (steps)
  ↓ IMotionService / IIODriver 호출
Drivers: VirtualMotionDriver.MoveAbs(...)
  ↓ Timer 50ms 시뮬레이션
Domain Model: AxisStatus 업데이트
  ↓ PropertyChanged
HMI: UI 갱신 (DispatcherTimer 폴링)
```

</div>
</div>

<div class="footer">CancellationToken 으로 시퀀스 중단/재개 · 글로벌 예외 핸들러(UI/Task/AppDomain) 3중 catch</div>

---

<!-- ═══════════════ Slide 3 : HMI MVVM & Data Flow ═══════════════ -->

# 3. HMI MVVM 참조 구조 및 데이터 흐름

<div class="cols">
<div class="box box-blue">

## View ↔ ViewModel ↔ Repository

| View (`Views/`) | ViewModel | Repository / Service |
|---|---|---|
| `MainWindow` | `MainViewModel` | — (Facade) |
| `MainDashboardView` | `MainDashboardViewModel` | `InkjetController` |
| `RecipeView` | `RecipeViewModel` | SQLite — `Recipes`,<br>`RecipeDetails_Motor/Position`,<br>`RecipeChangeLogs` |
| `AlarmHistoryView` | `AlarmViewModel` | `AlarmRepository` |
| `LogWindowView` | `LogViewModel` | `SystemLogRepository` |
| `DropWatcherView` | `DropWatcherViewModel` | `INozzleClassifier`,<br>`NozzleHealthRepository` |
| `NozzleTrendWindow` | `NozzleTrendViewModel` | `NozzleHealthRepository`<br>+ LiveCharts2 |
| `IOMonitorView` | `IOMonitorViewModel` | (Driver 직접) |
| `MotorControlView` | `MotorControlViewModel` | `IMotionService` |

<div class="sub">
※ 17 ViewModel · 20 Views · 다국어 171 strings (`ko-KR.xaml` / `en-US.xaml`)<br>
※ <b>MainViewModel = Facade</b> — 모든 ViewModel 의 공통 컨텍스트(Log/Alarm/Recipe/Controller 공유)
</div>

</div>
<div class="box box-green">

## Data Flow — Nozzle Health 예시

```
[1] User Click "INSPECT ALL"
       ↓
[2] DropWatcherVM.ExecuteInspectAllAsync()
       ↓ await
[3] IVisionDriver.CaptureAndInspectAsync()
       ↓
[4] InspectionResult (Score, Image, IsPass)
       ↓
[5] INozzleClassifier.Classify(result, 128)
       ↓ IDictionary<int,int>
[6] ApplyNozzleStates() → ObservableCollection<NozzleStatusItem>
       ↓ PropertyChanged
[7] NozzleHealthRepository.Save(...)
       ↓ INSERT InspectionSnapshot + Detail
[8] DB: C:/Logs/NozzleHealth.db (90일 보관)

       ─── 별도 시점 ───
[9] User Click "📈 TREND"
       ↓
[10] NozzleTrendViewModel ctor
       ↓
[11] NozzleHealthRepository.GetRecent(200)
       ↓
[12] SPC 계산 (mean, σ, UCL/LCL)
       ↓
[13] LiveCharts2 CartesianChart 바인딩
```

**핵심 참조** — `DropWatcherViewModel.cs:50`
```csharp
private readonly INozzleClassifier _classifier
    = new RandomNozzleClassifier();
```
<div class="sub">→ Phase 2 에서 `OnnxNozzleClassifier` 로 한 줄 교체</div>

</div>
</div>

<div class="footer">
ViewModelBase / RelayCommand : <code>Platform.Domain.Common</code> · Loc.T(key) : <code>Platform.HMI.Common</code> · CurrentView setter 에서 IDisposable 자동 해제
</div>
