# Architektur-Review: SDLC Orchestrator

**Review-Datum:** 2026-01-10
**Reviewer:** Critical Software Architect
**Scope:** Complete Architecture & Code Review

---

## 0) Repo-Orientierung

**Tech-Stack:**
- .NET 8, C# 12 (nullable enabled)
- Microsoft.Agents.AI.Workflows (Preview!) v1.0.0-preview.251219.1
- ModelContextProtocol (Preview!) v0.5.0-preview.1
- LibGit2Sharp, Octokit, OpenAI SDK
- YamlDotNet für Playbook-Parsing

**Build-System:** .NET SDK, Docker/docker-compose

**Entry Points:**
- `src/Orchestrator.App/Program.cs` (137 LOC)
- Startet `GitHubIssueWatcher` (Polling) + `GitHubWebhookListener` (Event-driven)

**Modulstruktur:**
```
src/Orchestrator.App/ (17.162 LOC)
├── Core/
│   ├── Configuration/     (Config, Labels, Workflow)
│   ├── Interfaces/        (IGitHubClient, IRepoGit, ILlmClient, etc.)
│   └── Models/            (WorkContext, WorkItem, Records)
├── Infrastructure/
│   ├── Filesystem/        (Workspace Adapter)
│   ├── Git/               (LibGit2Sharp Wrapper)
│   ├── GitHub/            (Octokit Wrapper)
│   ├── Llm/               (OpenAI Client)
│   └── Mcp/               (MCP Client Manager)
├── Workflows/
│   ├── Executors/         (14 Executors, 2.319 LOC)
│   ├── Gates/             (DoR, Spec, DoD Validators)
│   ├── Prompts/           (LLM Prompt Builder)
│   └── EventHandlers/     (LabelSync, HumanInLoop, Metrics)
├── Parsing/               (Spec, Playbook, Markdown, Gherkin)
├── Utilities/             (File Ops, Templates, Markdown Builder)
└── Watcher/               (Issue Watcher, Webhook Listener)

tests/ (Mirror-Struktur + Integration Tests)
```

**CI/CD:** GitHub Actions (ci.yml, release.yml, security.yml, docker.yml, sonarcloud.yml)

---

## 1) Schnellüberblick

**Was ist das System?**
Ein AI-gesteuerter SDLC-Orchestrator, der GitHub Issues durch einen definierten Workflow-Graph (Refinement → DoR → TechLead → SpecGate → Dev → CodeReview → DoD → Release) führt. Nutzt LLMs (GPT-5) für Requirements-Refinement, Spec-Generierung und Code-Implementation. Synchronisiert Workflow-State mit GitHub Labels und blockiert bei Gate-Failures für Human-in-the-Loop Eingriffe.

**Hauptkomponenten:**
1. **Watcher/Webhook:** Event-Detection via Polling + GitHub Webhooks
2. **WorkflowRunner:** Orchestriert Graph-Execution mit Iteration Limits & Checkpointing
3. **14 Executors:** Stage-spezifische Logik (Refinement, TechLead, Dev, Gates, etc.)
4. **Infrastructure Layer:** Abstractions für Git, GitHub, Filesystem, LLM, MCP
5. **Event Handlers:** Label-Synchronisation, Metrics Recording, Human-in-Loop

**Zentrale Flows:**
- Label `ready-for-agents` → Watcher triggert Workflow → Executors führen Stages aus → Gates validieren → Labels synchronisiert → Bei Blockierung: Human Review
- Jeder Executor: Lädt State, ruft LLM, schreibt Files, committed zu Git, speichert State

**Boundaries:**
- **Core:** Domain Models, Interfaces, Configuration (keine Abhängigkeiten nach außen)
- **Infrastructure:** Adapter für externe Systeme (GitHub, Git, LLM, MCP)
- **Workflows:** Orchestrierung, Executors, Gates (abhängig von Core + Infrastructure)
- **Parsing:** Utilities für Spec/Playbook/Markdown Parsing
- **Watcher:** Entry Layer, triggert Workflows

---

## 2) Komplexitäts-Check: "Ist das der richtige Weg?"

**Urteil: NEIN – System leidet unter massivem Overengineering**

### Kritische Komplexitätstreiber:

#### 1. **Preview-Framework-Abhängigkeit (Microsoft.Agents.AI)**
**Risiko:** KRITISCH
- `Orchestrator.App.csproj:16-18` - Drei Preview-Packages von Microsoft.Agents.AI (Dezember 2024!)
- **Problem:** Production-System baut auf instabiler API, Breaking Changes garantiert
- **Beweis:** Refactoring-Plan zeigt 10 Workstreams zur Migration – System wurde komplett umgebaut
- **Alternative:** Eigene State-Machine statt Framework-Lock-in

#### 2. **WorkContext als God Object**
**Risiko:** HOCH
```csharp
// Core/Models/Models.cs:5-22
internal sealed record WorkContext(
    WorkItem, IGitHubClient, OrchestratorConfig, IRepoWorkspace,
    IRepoGit, ILlmClient, IMetricsRecorder?, McpClientManager?,
    ConcurrentDictionary SharedState, ConcurrentDictionary State)
```
- 9 Dependencies in einem Record
- Wird in **JEDEM** Executor herumgereicht
- Verletzt Interface Segregation Principle
- **Alternative:** Executor-spezifische Context-Records

#### 3. **RefinementExecutor: 545 LOC Single Class**
**Risiko:** HOCH
```
Workflows/Executors/RefinementExecutor.cs: 545 Zeilen
- ExecuteAsync() (95 LOC)
- BuildRefinementAsync() (270 LOC)
- AssignStableQuestionNumbers() (40 LOC)
- PostAmbiguousQuestionsCommentAsync() (40 LOC)
- WriteRefinementFileAsync() (35 LOC)
- DetermineNextStage() (50 LOC)
```
- **Verantwortungen:** State Management, LLM Calls, File I/O, Git Commits, GitHub Comments, Question Routing
- **SRP-Verletzung:** 6 verschiedene Responsibilities
- **Alternative:** Separate Services für FileWriter, QuestionRouter, StateManager

#### 4. **Doppelte State-Verwaltung**
**Risiko:** MITTEL
```csharp
// RefinementExecutor.cs:34-35
await context.QueueStateUpdateAsync(WorkflowStateKeys.RefinementResult, serialized, cancellationToken);
WorkContext.State[WorkflowStateKeys.RefinementResult] = serialized;
```
- State wird DOPPELT gespeichert: IWorkflowContext (persistent) + WorkContext.State (in-memory)
- Inkonsistenzen vorprogrammiert
- **Beweis:** Refactoring-Plan Workstream 3 - "MS AF checkpointing not integrated"

#### 5. **Abstraktions-Overhead ohne Variation**
**Risiko:** MITTEL
- `Infrastructure/ServiceFactory.cs` - Factory Pattern ohne echte Variation (nur Octokit, LibGit2Sharp)
- `Core/Interfaces/` - 5 Interfaces mit jeweils 1 Implementierung
- **YAGNI-Verletzung:** Kein zweiter GitHub-Client, kein zweiter Git-Provider
- **Alternative:** Direkte Abhängigkeiten, bei Bedarf extrahieren

#### 6. **14 Executors mit repetitiver Struktur**
```
Executors/ - 2.319 LOC über 14 Files
- RefinementExecutor (545), CodeReviewExecutor (280), GateExecutor (177)
- Alle erben von WorkflowStageExecutor (138 LOC)
- Repetitive Patterns: State Load, LLM Call, File Write, Git Commit
```
- **Template Method Pattern** könnte 60% der Duplikation eliminieren
- **Alternative:** Executor-Builder mit Composition statt Inheritance

### Hot Spots (Git History, 3 Monate):
```bash
13× .github/workflows/ci.yml         # CI-Thrashing
9×  Program.cs                        # Entry Point instabil
9×  docs/refactoring-plan.md          # Konstante Architektur-Änderungen
7×  SDLCWorkflow.cs / WorkflowFactory # Kern-Logik volatil
```
**Interpretation:** System in aktivem Umbau, Architektur nicht stabilisiert.

### Komplexität in vermeintlich stabilen Bereichen:
- **Parsing/** (778 LOC) - 4 Parser für verschiedene Formate (Spec, Touch List, Gherkin, Playbook)
- **Watcher/** (566 LOC) - GitHubIssueWatcher (297) + WebhookListener (269) laufen PARALLEL
- **Utilities/** (316 LOC) - TemplateUtil (122), RefinementMarkdownBuilder (113)

**Fazit:** Komplexität wächst in Bereichen, die Stabilität brauchen (Parsing, Watcher). Core-Workflow bleibt volatil.

---

## 3) Prinzipien-Review

### SOLID

#### **S - Single Responsibility: VERLETZT**
**Bewertung: ❌ Verletzt**

**Beweise:**
1. `RefinementExecutor.cs:18-95` - ExecuteAsync() macht:
   - State Management
   - LLM Orchestration
   - File I/O
   - Git Commits (mit Exception Handling)
   - GitHub API Calls
   - Routing Logic

2. `GitHubIssueWatcher.cs:297 LOC` - Mischt:
   - Issue Polling
   - Workflow Triggering
   - Label Filtering
   - Checkpointing

3. `WorkContext` - "Knows too much":
   - Configuration
   - All Infrastructure Services
   - Metrics
   - Two State Dictionaries

**Konkretes Beispiel:**
```csharp
// RefinementExecutor.cs:57-84 - Git Commit Logic IN Executor
try {
    var branchName = $"issue-{input.WorkItem.Number}";
    WorkContext.Repo.CommitAndPush(branchName, commitMessage, new[] { refinementPath });
} catch (LibGit2Sharp.LibGit2SharpException ex) {
    Logger.Warning($"[Refinement] Git commit failed (continuing anyway): {ex.Message}");
}
```
→ **Executor sollte nicht wissen, wie Git funktioniert**

#### **O - Open/Closed: TEILWEISE**
**Bewertung: ⚠️ Teilweise**

**Positiv:**
- `WorkflowStageGraph.cs` - Graph ist konfigurierbar via Edges
- Executor-System ist erweiterbar (neue Stage = neuer Executor)

**Negativ:**
```csharp
// WorkflowStageExecutor.cs:123-136 - Hardcoded Switch
private static int MaxIterationsForStage(WorkflowConfig config, WorkflowStage stage)
{
    return stage switch
    {
        WorkflowStage.ContextBuilder => 1,
        WorkflowStage.Refinement or WorkflowStage.DoR => config.MaxRefinementIterations,
        // ... 6 weitere Hardcoded Cases
        _ => 1
    };
}
```
→ **Jede neue Stage erfordert Code-Änderung**

#### **L - Liskov Substitution: ERFÜLLT**
**Bewertung: ✅ Erfüllt**

- Alle Executors erben korrekt von `WorkflowStageExecutor`
- Interfaces werden konsistent verwendet (IGitHubClient, IRepoGit)
- Keine Substitutions-Verletzungen gefunden

#### **I - Interface Segregation: VERLETZT**
**Bewertung: ❌ Verletzt**

**Beweise:**
```csharp
// Core/Interfaces/IRepoGit.cs (Annahme basierend auf Nutzung)
// Jeder Executor bekommt VOLLES IRepoGit Interface, braucht aber nur:
// - RefinementExecutor: CommitAndPush()
// - TechLeadExecutor: CommitAndPush()
// - ContextBuilder: CreateBranch(), CheckoutBranch()
```

**WorkContext zwingt Executors ALLE Dependencies zu kennen:**
```csharp
// RefinementExecutor.cs - Braucht nur: Llm, Workspace, GitHub
// Bekommt aber: Repo, Config, Metrics, Mcp, State
```

#### **D - Dependency Inversion: ERFÜLLT**
**Bewertung: ✅ Erfüllt**

- Infrastructure Interfaces in Core/ definiert
- Implementations in Infrastructure/
- Keine direkten Abhängigkeiten zu Octokit/LibGit2Sharp in Workflows

### KISS (Keep It Simple, Stupid)

**Bewertung: ❌ Verletzt**

**Beweise:**

1. **Workflow-Start benötigt 8 Objekte:**
```csharp
// Program.cs:65-84
var checkpoints = new InMemoryWorkflowCheckpointStore();
var labelSync = new LabelSyncHandler(...);
var humanInLoop = new HumanInLoopHandler(...);
var metricsStore = new FileWorkflowMetricsStore(...);
var runner = new WorkflowRunner(labelSync, humanInLoop, metricsStore, checkpoints);
var watcher = new GitHubIssueWatcher(cfg, github, runner, workItemFactory, checkpoints, ...);
var webhook = new GitHubWebhookListener(...);
```
→ **Kein Dependency Injection Container, aber DI-Komplexität**

2. **Einfache Operation = 100 LOC:**
```csharp
// RefinementExecutor.cs:97-197 - BuildRefinementAsync()
// Lädt State (20 LOC) → Parsed Markdown (30 LOC) → Incorporates Answers (50 LOC) →
// Filters Ambiguous (30 LOC) → Assigns Numbers (40 LOC) → Builds Result (20 LOC)
```
→ **190 LOC für "Build Refinement" - nicht SIMPLE**

### YAGNI (You Aren't Gonna Need It)

**Bewertung: ❌ Verletzt**

**Beweise:**

1. **Premature Abstraction:**
```csharp
// Infrastructure/ServiceFactory.cs - Factory mit 1 Implementierung
public static Services Create(OrchestratorConfig cfg)
{
    return new Services(
        GitHub: new OctokitGitHubClient(...),  // Einzige GitHub-Impl
        RepoGit: new RepoGit(...),             // Einzige Git-Impl
        Workspace: new RepoWorkspace(...),     // Einzige Workspace-Impl
        Llm: new LlmClient(...)                // Einzige LLM-Impl
    );
}
```

2. **Metrics-System für 0 Dashboards:**
```csharp
// WorkflowMetricsStore.cs:151 LOC
// WorkflowMetricsRecorder.cs:108 LOC
// → 259 LOC für Metrics, die nirgends visualisiert werden
// Refactoring-Plan: "Aggregate metrics scaffolding for future dashboards"
```

3. **Mode-System ohne Mehrwert:**
```csharp
// Unterstützt Modes: minimal, batch, tdd
// Refactoring-Plan: "Mode override labels (`mode:batch`, `mode:tdd`) not parsed"
// → Feature existiert, funktioniert aber nicht
```

### Clean Architecture

#### **Abhängigkeitsrichtung: TEILWEISE**
**Bewertung: ⚠️ Teilweise**

**Positiv:**
- `Core/` hat keine Dependencies (außer .NET)
- `Infrastructure/` implementiert Core Interfaces

**Negativ:**
```csharp
// Workflows/Executors/RefinementExecutor.cs:77-78
catch (LibGit2Sharp.LibGit2SharpException ex)
```
→ **Workflow Layer kennt Infrastructure-Details (LibGit2Sharp)**

#### **Use-Case-Zentrierung: VERLETZT**
**Bewertung: ❌ Verletzt**

- Kein expliziter Use-Case Layer
- Executors sind technische Stages, keine Business Use-Cases
- "Refine Issue", "Generate Spec", "Implement Code" als Use-Cases fehlen
- Stattdessen: Technische Orchestrierung über Workflow-Graph

#### **Interface-Disziplin (Ports/Adapters): TEILWEISE**
**Bewertung: ⚠️ Teilweise**

**Positiv:**
- Infrastructure Services hinter Interfaces (IGitHubClient, IRepoGit)

**Negativ:**
- **Kein Application Service Layer** - Executors mischen Orchestration + Business Logic
- **Keine Domain Services** - LLM Prompts direkt in Executors
- **MCP Integration durchbricht Abstraktion:**
```csharp
// WorkContext.cs:19
public McpFileOperations? McpFiles => Mcp != null ? new McpFileOperations(Mcp) : null;
// → WorkContext kennt konkrete MCP-Implementation
```

#### **Framework-Isolation: VERLETZT**
**Bewertung: ❌ Verletzt**

**Microsoft.Agents.AI durchdringt die gesamte Codebase:**
```csharp
// Workflows/Executors/WorkflowStageExecutor.cs:1
using Microsoft.Agents.AI.Workflows;

// Alle 14 Executors erben von Microsoft-Framework-Klasse
internal sealed class RefinementExecutor : WorkflowStageExecutor
```
→ **Komplette Abhängigkeit von Preview-Framework**

#### **Boundary-Verletzungen: JA**
**Bewertung: ❌ Verletzt**

```csharp
// Workflows importiert direkt Infrastructure-Types
Workflows/Executors/RefinementExecutor.cs:77 → LibGit2Sharp.LibGit2SharpException

// Parsing importiert Core.Models
Parsing/SpecParser.cs → ParsedSpec (OK)

// Workflows kennt Parsing (eigentlich OK)
Workflows/Executors/TechLeadExecutor.cs → SpecParser
```

### Not Invented Here Syndrome

**Bewertung: ⚠️ Teilweise**

**Positiv (verwendet externe Libraries):**
- Octokit für GitHub API
- LibGit2Sharp für Git
- YamlDotNet für YAML
- OpenAI SDK für LLM

**Negativ (Re-Implementation):**
- **Eigenes Workflow-System** statt z.B. Elsa Workflows / Workflow Core
- **Eigene Markdown-Parser** statt Markdig Extensions
- **Eigene State-Maschine** im WorkflowStageGraph

**Gerechtfertigt?**
- Workflow-Graph ist domain-spezifisch → OK
- Markdown-Parsing für Refinement/Spec ist custom → OK
- **Aber:** Microsoft.Agents.AI wird genutzt UND eigene Abstraktion gebaut → Confusion

---

## 4) Architekturrisiken und Wartbarkeit

### Top 5 Risiken (Impact × Wahrscheinlichkeit)

#### **1. Preview-Framework-Lock-in** ⚠️ **KRITISCH (Impact: 5/5, Wahrscheinlichkeit: 5/5)**

**Symptom:**
```xml
<!-- Orchestrator.App.csproj:16-18 -->
<PackageReference Include="Microsoft.Agents.AI" Version="1.0.0-preview.251219.1" />
<PackageReference Include="Microsoft.Agents.AI.Workflows" Version="1.0.0-preview.251219.1" />
<PackageReference Include="ModelContextProtocol" Version="0.5.0-preview.1" />
```

**Warum gefährlich:**
- Preview-APIs können Breaking Changes haben (keine SemVer-Garantie)
- Microsoft kann Framework discontinued machen
- Alle 14 Executors erben von `WorkflowStageExecutor` (Framework-Klasse)
- Refactoring-Plan zeigt: 10 Workstreams nötig für Migration → **Massiver Umbau bereits passiert**

**Wie messen:**
- `grep -r "Microsoft.Agents" src/ | wc -l` → **Coupling-Metrik**
- Breaking Changes in Preview-Updates tracken

**Konkrete Reduktion:**
1. **Abstraktion einführen:**
   ```csharp
   // Neues Interface in Core/
   internal interface IWorkflowExecutor
   {
       Task<ExecutorResult> ExecuteAsync(ExecutorContext ctx, CancellationToken ct);
   }

   // Executors implementieren eigenes Interface, nicht Framework-Klasse
   internal sealed class RefinementExecutor : IWorkflowExecutor
   {
       // Framework-Adapter nur in Infrastructure/Workflows/
   }
   ```

2. **Migration Path:**
   - Woche 1-2: Interface-Abstraktion einführen
   - Woche 3-4: Executors von Framework entkoppeln
   - Woche 5-6: Workflow-Graph ohne Microsoft.Agents.AI implementieren
   - **Aufwand:** 6 Wochen, 2 Entwickler

---

#### **2. WorkContext God Object** ⚠️ **HOCH (Impact: 4/5, Wahrscheinlichkeit: 4/5)**

**Symptom:**
```csharp
// Core/Models/Models.cs:5-22
internal sealed record WorkContext(
    WorkItem, IGitHubClient, OrchestratorConfig, IRepoWorkspace,
    IRepoGit, ILlmClient, IMetricsRecorder?, McpClientManager?,
    ConcurrentDictionary SharedState, ConcurrentDictionary State)
```

**Warum gefährlich:**
- **Jeder Executor kennt ALLE Dependencies** (Interface Segregation verletzt)
- **Testing wird schwer:** Mock 9 Dependencies für jeden Test
- **Coupling:** Änderungen an WorkContext betreffen alle 14 Executors
- **Memory Leaks:** State-Dictionaries wachsen unbegrenzt

**Wie messen:**
```bash
# Zähle WorkContext Usages
grep -r "WorkContext" src/Orchestrator.App/Workflows/Executors/*.cs | wc -l
# → 150+ Referenzen über 14 Files
```

**Smells:**
- Constructor mit >7 Parameters
- Record mit >5 Properties
- God Object Pattern

**Konkrete Reduktion:**
```csharp
// VORHER: Monolithic WorkContext
internal record WorkContext(9 Dependencies)

// NACHHER: Executor-spezifische Contexts
internal record RefinementContext(
    WorkItem Item,
    ILlmClient Llm,
    IFileWriter Files,
    IQuestionRouter Router
);

internal record TechLeadContext(
    WorkItem Item,
    ILlmClient Llm,
    ISpecWriter SpecWriter,
    IPlaybookValidator Playbook
);

// Workflow erstellt Contexts on-demand
var refinementCtx = new RefinementContext(
    item, services.Llm, services.Files, services.Router);
```

**Aufwand:** 1 Woche, 1 Entwickler

---

#### **3. RefinementExecutor Monolith** ⚠️ **HOCH (Impact: 4/5, Wahrscheinlichkeit: 3/5)**

**Symptom:**
```
RefinementExecutor.cs: 545 LOC
├── ExecuteAsync()                        95 LOC
├── BuildRefinementAsync()               270 LOC  ← MONSTER METHOD
├── AssignStableQuestionNumbers()         40 LOC
├── PostAmbiguousQuestionsCommentAsync()  40 LOC
├── WriteRefinementFileAsync()            35 LOC
└── DetermineNextStage()                  50 LOC
```

**Warum gefährlich:**
- **Cyclomatic Complexity:** BuildRefinementAsync() hat >15 Branches
- **6 verschiedene Responsibilities** (SRP-Verletzung)
- **Kein Refactoring möglich** ohne Breaking Tests
- **Bug-Surface:** Jede Änderung kann 6 Dinge kaputt machen

**Wie messen:**
- Cyclomatic Complexity (SonarQube)
- Lines per Method (>50 = Red Flag)
- Class Cohesion Metrics (LCOM)

**Konkrete Reduktion:**
```csharp
// VORHER: Monolithischer Executor
internal sealed class RefinementExecutor : WorkflowStageExecutor
{
    protected override async ValueTask<(bool, string)> ExecuteAsync()
    {
        // 95 LOC mixing 6 concerns
    }
}

// NACHHER: Composition-based Design
internal sealed class RefinementExecutor : IWorkflowExecutor
{
    private readonly IRefinementBuilder _builder;
    private readonly IQuestionNumberAssigner _numberAssigner;
    private readonly IRefinementFileWriter _fileWriter;
    private readonly IGitCommitter _gitCommitter;
    private readonly IQuestionRouter _router;

    public async Task<ExecutorResult> ExecuteAsync(...)
    {
        var refinement = await _builder.BuildAsync(...);
        var numbered = _numberAssigner.Assign(refinement);
        await _fileWriter.WriteAsync(numbered);
        await _gitCommitter.CommitAsync(numbered);
        return _router.DetermineNext(numbered);
    }
}
```

**Aufwand:** 3-4 Tage, 1 Entwickler

---

#### **4. Doppelte State-Verwaltung** ⚠️ **MITTEL (Impact: 3/5, Wahrscheinlichkeit: 4/5)**

**Symptom:**
```csharp
// RefinementExecutor.cs:34-35
var serialized = WorkflowJson.Serialize(refinement);
await context.QueueStateUpdateAsync(WorkflowStateKeys.RefinementResult, serialized, ct);
WorkContext.State[WorkflowStateKeys.RefinementResult] = serialized;
// ↑ State wird DOPPELT gespeichert
```

**Warum gefährlich:**
- **Inkonsistenzen:** In-Memory State kann von Persistent State abweichen
- **Race Conditions:** Concurrent Access auf SharedState Dictionary
- **Memory Leaks:** In-Memory State wächst unbegrenzt
- **Debugging-Nightmare:** Welcher State ist "truth"?

**Wie messen:**
```bash
# Finde alle doppelten State-Writes
grep -A 1 "QueueStateUpdateAsync" src/Orchestrator.App/Workflows/Executors/*.cs | grep "WorkContext.State"
# → ~20 Stellen mit doppeltem Write
```

**Smells:**
- State in zwei Locations
- `ConcurrentDictionary` ohne Bounded Size
- Keine State-Cleanup Logic

**Konkrete Reduktion:**
```csharp
// LÖSUNG 1: Single Source of Truth
// Entscheide: Persistent State (IWorkflowContext) ODER In-Memory (WorkContext)

// OPTION A: Nur Persistent State
await context.QueueStateUpdateAsync(key, value, ct);
// WorkContext.State entfernen ← EINFACHSTE LÖSUNG

// OPTION B: Cache mit Sync
internal class StateCache
{
    private readonly IWorkflowContext _persistent;
    private readonly ConcurrentDictionary<string, string> _cache;

    public async Task<string> GetAsync(string key)
    {
        if (_cache.TryGetValue(key, out var value)) return value;
        value = await _persistent.ReadOrInitStateAsync(key, ...);
        _cache[key] = value;
        return value;
    }
}
```

**Aufwand:** 2-3 Tage, 1 Entwickler

---

#### **5. Parser-Sprawl ohne Unified Schema** ⚠️ **MITTEL (Impact: 3/5, Wahrscheinlichkeit: 3/5)**

**Symptom:**
```
Parsing/
├── SpecParser.cs              (178 LOC) → Parses Spec Markdown
├── RefinementMarkdownParser.cs(184 LOC) → Parses Refinement Markdown
├── TouchListParser.cs         (62 LOC)  → Parses Touch List
├── WorkItemParsers.cs         (222 LOC) → Parses Issue Body
├── GherkinValidator.cs        (31 LOC)  → Validates Gherkin
└── PlaybookParser.cs          (28 LOC)  → Parses YAML Playbook
```

**Warum gefährlich:**
- **6 verschiedene Parser** für ähnliche Strukturen (Markdown mit YAML-Frontmatter)
- **Keine Unified Schema:** Jeder Parser hat eigene Logic
- **Duplikation:** Markdown-Parsing-Code in 4 Files
- **Fragile:** Änderung am Format = 4 Parser ändern

**Wie messen:**
- Code Duplication Metrics (SonarQube)
- Parser Lines per Format
- **Regression-Test Coverage** für Parser (aktuell: unbekannt)

**Smells:**
```csharp
// SpecParser.cs:45 - Regex-based Parsing
var match = Regex.Match(content, @"##\s+Touch List");

// RefinementMarkdownParser.cs:67 - Unterschiedliches Regex
var match = Regex.Match(line, @"^\s*-\s*\[\s*\]\s*\*\*Question\s+#(\d+):");

// WorkItemParsers.cs:89 - Wieder anderes Pattern
var match = Regex.Match(line, @"^-\s+(.+)$");
```

**Konkrete Reduktion:**
```csharp
// UNIFIED PARSER mit Markdig
internal class MarkdownSchemaParser
{
    public ParsedDocument Parse(string markdown)
    {
        var doc = Markdown.Parse(markdown);
        return new ParsedDocument
        {
            Frontmatter = ExtractYaml(doc),
            Sections = ExtractSections(doc),
            Lists = ExtractLists(doc)
        };
    }
}

// Spezifische Parser werden Adapters
internal class SpecParser
{
    private readonly MarkdownSchemaParser _parser;

    public ParsedSpec Parse(string content)
    {
        var doc = _parser.Parse(content);
        return new ParsedSpec(
            TouchList: doc.GetSection("Touch List").AsList(),
            Scenarios: doc.GetSection("Scenarios").AsGherkin(),
            ...
        );
    }
}
```

**Aufwand:** 1 Woche, 1 Entwickler

---

## 5) Konkrete Verbesserungen

### Biggest Wins (1-3 Tage)

#### **1. WorkContext Splitting (1 Tag)**
**Pfad:** `Core/Models/Models.cs:5-22`, alle Executors

**Maßnahme:**
```csharp
// Ersetze monolithischen WorkContext durch Executor-spezifische Contexts
internal record RefinementContext(WorkItem Item, ILlmClient Llm, IFileWriter Files);
internal record TechLeadContext(WorkItem Item, ILlmClient Llm, ISpecWriter Spec);
// ... etc für jeden Executor
```

**Impact:** Reduziert Coupling, verbessert Testability, klarere Dependencies

---

#### **2. State Management Cleanup (2 Tage)**
**Pfad:** Alle Executors (`Workflows/Executors/*.cs`)

**Maßnahme:**
```bash
# REMOVE: WorkContext.State Dictionary
# KEEP: Nur IWorkflowContext (persistent state)

git grep "WorkContext.State\[" src/ | wc -l  # → ~40 Stellen zu ändern
```

**Impact:** Eliminiert State-Inkonsistenzen, reduziert Memory Leaks

---

#### **3. RefinementExecutor Refactoring (3 Tage)**
**Pfad:** `Workflows/Executors/RefinementExecutor.cs`

**Maßnahme:**
```csharp
// Extrahiere Services:
// 1. RefinementBuilder (BuildRefinementAsync → eigene Klasse)
// 2. QuestionNumberAssigner (AssignStableQuestionNumbers → eigene Klasse)
// 3. RefinementFileWriter (WriteRefinementFileAsync → eigene Klasse)
// 4. AmbiguousQuestionHandler (PostAmbiguousQuestionsCommentAsync → eigene Klasse)

// Executor wird zu Orchestrator (15-20 LOC)
```

**Impact:** SRP erfüllt, Testability, Wiederverwendbarkeit

---

#### **4. Remove Unused Mode System (1 Tag)**
**Pfad:** `Workflows/`, `Core/Configuration/`

**Maßnahme:**
```bash
# Refactoring-Plan sagt: "Mode override labels not parsed"
# → Feature existiert, funktioniert nicht

# ENTWEDER: Implementieren (2-3 Tage)
# ODER: Entfernen (1 Tag) ← EMPFEHLUNG

git grep "Mode:" src/ tests/ | wc -l  # → Cleanup-Scope ermitteln
```

**Impact:** YAGNI-Compliance, weniger toten Code

---

#### **5. Interface Segregation für WorkContext (2 Tage)**
**Pfad:** `Core/Models/`, Executor-Layer

**Maßnahme:**
```csharp
// Executor braucht nicht ALLE Services
internal interface IRefinementServices
{
    ILlmClient Llm { get; }
    IFileWriter Files { get; }
}

internal interface ITechLeadServices
{
    ILlmClient Llm { get; }
    ISpecWriter Spec { get; }
    IPlaybookValidator Playbook { get; }
}

// WorkContext implementiert alle Interfaces
// Executor nimmt nur benötigte
```

**Impact:** Interface Segregation Principle erfüllt

---

#### **6. Extract LLM Prompts to separate Files (1 Tag)**
**Pfad:** `Workflows/Prompts/*.cs`

**Aktuell:**
```csharp
// Prompts sind hardcoded in C#
var system = "You are an SDLC refinement assistant. Do not invent requirements...";
```

**Empfohlen:**
```
Workflows/Prompts/
├── refinement.system.txt
├── refinement.user.txt
├── techlead.system.txt
└── ...
```

**Impact:** LLM Prompt Engineering ohne Recompile, Version Control für Prompts

---

#### **7. Metrics Store optional machen (0.5 Tage)**
**Pfad:** `Program.cs:68`, `WorkflowRunner.cs`

**Aktuell:**
```csharp
var metricsStore = new FileWorkflowMetricsStore(...);
var runner = new WorkflowRunner(labelSync, humanInLoop, metricsStore, checkpoints);
```

**Problem:** Metrics wird nie visualisiert, aber 259 LOC Code dafür

**Maßnahme:**
```csharp
// Feature Flag oder Env Var
var metricsStore = cfg.EnableMetrics
    ? new FileWorkflowMetricsStore(...)
    : null;

// IWorkflowMetricsStore? ist bereits optional
```

**Impact:** Reduziert Startup-Overhead, macht Feature explizit optional

---

#### **8. Git Error Handling standardisieren (1 Tag)**
**Pfad:** Alle Executors mit Git-Commits

**Aktuell:**
```csharp
// Jeder Executor hat eigenes try-catch
try {
    WorkContext.Repo.CommitAndPush(...);
} catch (LibGit2Sharp.LibGit2SharpException ex) {
    Logger.Warning($"Git commit failed: {ex.Message}");
} catch (InvalidOperationException ex) { ... }
```

**Maßnahme:**
```csharp
// Infrastructure/Git/CommitService.cs
internal class CommitService
{
    public async Task<CommitResult> TryCommitAsync(...)
    {
        try {
            await _repo.CommitAndPush(...);
            return CommitResult.Success();
        } catch (Exception ex) {
            _logger.Warning($"Git commit failed: {ex.Message}");
            return CommitResult.Failed(ex);
        }
    }
}
```

**Impact:** DRY, konsistentes Error Handling

---

#### **9. ServiceFactory durch Simple Constructor Injection ersetzen (1 Tag)**
**Pfad:** `Infrastructure/ServiceFactory.cs`, `Program.cs`

**Aktuell:**
```csharp
// ServiceFactory mit 1 Implementierung pro Interface = Overkill
var services = Infrastructure.ServiceFactory.Create(cfg);
```

**Maßnahme:**
```csharp
// Program.cs - Direkte Instanziierung
var github = new OctokitGitHubClient(cfg);
var repo = new RepoGit(cfg);
var workspace = new RepoWorkspace(cfg);
var llm = new LlmClient(cfg);

// Oder: Microsoft.Extensions.DependencyInjection wenn DI gewünscht
```

**Impact:** YAGNI, weniger Indirection

---

#### **10. Parallele Watcher/Webhook zu Single Entry Point (2 Tage)**
**Pfad:** `Program.cs:84-92`, `Watcher/`

**Aktuell:**
```csharp
// Beide laufen parallel
await Task.WhenAll(
    watcher.RunAsync(cts.Token),  // Polling
    webhookTask);                  // Event-driven
```

**Problem:** Zwei Trigger-Mechanismen = Race Conditions möglich

**Maßnahme:**
```csharp
// ENTWEDER: Nur Webhook (Event-driven) ← EMPFEHLUNG
// ODER: Nur Polling
// ODER: Webhook triggert Scan, Watcher disabled

if (cfg.WebhookEnabled) {
    await webhook.StartAsync(cts.Token);
} else {
    await watcher.RunAsync(cts.Token);
}
```

**Impact:** Reduziert Komplexität, eindeutiger Trigger-Path

---

### Strategische Änderungen (3-5 Actions)

#### **1. Microsoft.Agents.AI Framework Exit Strategy**

**Trade-offs:**
- **Pro:** Volle Kontrolle, keine Breaking Changes bei Preview-Updates
- **Contra:** Eigene State Machine = Maintenance Burden
- **Migration:** 6-8 Wochen, 2 Entwickler

**Migrationspfad:**
1. **Phase 1 (Woche 1-2):** Interface-Abstraktion (`IWorkflowExecutor`)
2. **Phase 2 (Woche 3-4):** Custom Workflow-Engine implementieren
3. **Phase 3 (Woche 5-6):** Framework-Abhängigkeit entfernen
4. **Phase 4 (Woche 7-8):** Testing & Rollout

**Risiken:**
- Regression bei State Management (aktuell: IWorkflowContext von Framework)
- Performance-Unterschiede (Framework evtl. optimiert)

---

#### **2. Domain-Driven Design Refactoring**

**Entscheidung:** Einführung eines Application Service Layers

**Kontext:**
- Aktuell: Executors mischen Orchestration + Business Logic
- Problem: Schwer zu testen, hohe Coupling

**Struktur:**
```
src/Orchestrator.App/
├── Application/         (NEU)
│   ├── UseCases/
│   │   ├── RefineIssue/
│   │   │   ├── RefineIssueCommand.cs
│   │   │   └── RefineIssueHandler.cs
│   │   ├── GenerateSpec/
│   │   └── ImplementCode/
│   └── Services/
│       ├── QuestionRouter.cs
│       ├── SpecValidator.cs
│       └── PlaybookEnforcer.cs
├── Domain/              (NEU)
│   ├── Entities/        (WorkItem, Refinement, Spec)
│   ├── ValueObjects/    (Question, AcceptanceCriteria)
│   └── Services/        (LlmPromptBuilder, MarkdownSerializer)
├── Infrastructure/      (UNCHANGED)
└── Workflows/           (WIRD ZU: Thin Orchestration)
    └── Executors/       → Ruft nur Application Services
```

**Trade-offs:**
- **Pro:** Clean Architecture, bessere Testability
- **Contra:** Mehr Indirection, Initial Overhead
- **Aufwand:** 4-6 Wochen

---

#### **3. Event-Sourcing für Workflow State**

**Entscheidung:** State als Event-Stream statt Snapshots

**Kontext:**
- Aktuell: State-Snapshots in IWorkflowContext
- Problem: Keine Historie, schwer zu debuggen

**Struktur:**
```csharp
// Events
internal record RefinementStartedEvent(int IssueNumber, DateTimeOffset Timestamp);
internal record QuestionsGeneratedEvent(int IssueNumber, List<Question> Questions);
internal record QuestionAnsweredEvent(int QuestionId, string Answer, string AnsweredBy);

// State Projection
internal class WorkflowState
{
    public static WorkflowState From(IEnumerable<WorkflowEvent> events)
    {
        // Replay events → Build current state
    }
}
```

**Trade-offs:**
- **Pro:** Full Audit Trail, Time Travel Debugging, Replay Workflows
- **Contra:** Komplexität, Storage Growth, Query Performance
- **Aufwand:** 6-8 Wochen

**Empfehlung:** NICHT jetzt - erst wenn Audit-Anforderungen kommen

---

#### **4. Unified Schema mit JSON Schema Validation**

**Entscheidung:** Alle Markdown-Formate folgen einheitlichem Schema

**Kontext:**
- 6 verschiedene Parser für ähnliche Formate
- Keine Schema-Validation

**Implementierung:**
```json
// schemas/refinement.schema.json
{
  "$schema": "http://json-schema.org/draft-07/schema#",
  "type": "object",
  "properties": {
    "clarifiedStory": { "type": "string", "minLength": 50 },
    "acceptanceCriteria": {
      "type": "array",
      "minItems": 3,
      "items": { "type": "string", "pattern": "(given|when|then|should|must)" }
    },
    "openQuestions": {
      "type": "array",
      "items": {
        "type": "object",
        "properties": {
          "questionNumber": { "type": "integer" },
          "question": { "type": "string" }
        }
      }
    }
  }
}
```

**Trade-offs:**
- **Pro:** Validierung, Dokumentation, Tooling (VS Code IntelliSense)
- **Contra:** Schema-Maintenance, Breaking Changes Management
- **Aufwand:** 2-3 Wochen

---

#### **5. Observability & Distributed Tracing**

**Entscheidung:** OpenTelemetry Integration

**Kontext:**
- Aktuell: Nur Logs, keine Tracing
- Problem: Workflow-Debugging bei Failures schwer

**Implementierung:**
```csharp
// Program.cs
using OpenTelemetry;
using OpenTelemetry.Trace;

var tracerProvider = Sdk.CreateTracerProviderBuilder()
    .AddSource("Orchestrator")
    .AddJaegerExporter()
    .Build();

// Executors
using var activity = ActivitySource.StartActivity("RefinementExecutor.Execute");
activity?.SetTag("issue.number", input.WorkItem.Number);
```

**Trade-offs:**
- **Pro:** End-to-End Visibility, Performance Insights
- **Contra:** Infrastructure Dependency (Jaeger/Zipkin)
- **Aufwand:** 1 Woche

---

### "Nicht anfassen" (Stabile Bereiche)

Diese Bereiche funktionieren gut und sollten als Anker dienen:

1. **Core/Models/** - Domain Models sind klar definiert (Records, Immutable)
2. **Infrastructure/GitHub/** - Octokit-Wrapper funktioniert stabil
3. **Infrastructure/Git/** - LibGit2Sharp-Wrapper hat klare Contracts
4. **Parsing/GherkinValidator.cs** - Kleine, fokussierte Validator
5. **Utilities/CodeHelpers.cs** - Pure Functions ohne State
6. **Tests/TestHelpers/** - Mock-Infrastruktur ist solide (9× geändert = aktiv gepflegt)

---

## 6) ADR-Check

### Implizite Architekturentscheidungen (aus Code abgeleitet)

1. **Microsoft.Agents.AI Framework als Workflow-Engine**
   - Status: ✅ Implementiert
   - Risiko: ⚠️ Preview-Framework in Production

2. **Workflow als State Machine mit Graph**
   - Status: ✅ Implementiert (WorkflowStageGraph.cs)
   - Qualität: ⚠️ Hardcoded Stages, keine dynamische Config

3. **Dual Trigger-Mechanismus (Polling + Webhook)**
   - Status: ✅ Implementiert
   - Qualität: ⚠️ Race Conditions möglich

4. **State in dual Locations (Persistent + In-Memory)**
   - Status: ✅ Implementiert
   - Qualität: ❌ Inkonsistenzen, Memory Leaks

5. **LLM Prompts als Hardcoded Strings**
   - Status: ✅ Implementiert
   - Qualität: ⚠️ Kein Prompt Engineering ohne Recompile

6. **Infrastructure Abstractions (IGitHubClient, IRepoGit, ILlmClient)**
   - Status: ✅ Implementiert
   - Qualität: ✅ Gut, aber 1:1 Mapping (kein echter Variation-Point)

7. **WorkContext als Global Context Object**
   - Status: ✅ Implementiert
   - Qualität: ❌ God Object Anti-Pattern

8. **File-based Metrics Store ohne Visualization**
   - Status: ✅ Implementiert
   - Qualität: ⚠️ YAGNI-Verletzung

### Fehlende/Fragwürdige Entscheidungen

1. **❌ FEHLT: Persistence Strategy (Event Sourcing vs Snapshots)**
2. **❌ FEHLT: Error Recovery Strategy (Retry, Circuit Breaker)**
3. **❌ FEHLT: Schema Evolution Strategy (Breaking Changes in Markdown)**
4. **⚠️ FRAGWÜRDIG: Preview-Framework in Production ohne Exit-Strategy**
5. **⚠️ FRAGWÜRDIG: Dual State Management ohne Synchronisation-Garantie**

---

### ADR-Entwürfe

#### **ADR-001: Workflow State Management**

**Entscheidung:** Verwende IWorkflowContext (persistent) als Single Source of Truth, entferne WorkContext.State (in-memory).

**Kontext:**
- Aktuell: State wird doppelt gespeichert (persistent + in-memory)
- Problem: Inkonsistenzen, Race Conditions, Memory Leaks
- Alternative: Nur in-memory (verliert State bei Restart)

**Konsequenzen:**
- (+) Konsistenz garantiert
- (+) Kein Memory Management nötig
- (-) Potentiell langsamerer Zugriff (IO statt RAM)
- (-) IWorkflowContext-API muss async sein

---

#### **ADR-002: Framework Independence Strategy**

**Entscheidung:** Führe IWorkflowExecutor Interface ein, entkoppelt von Microsoft.Agents.AI.

**Kontext:**
- Risiko: Preview-Framework kann Breaking Changes haben
- Refactoring-Plan zeigt: Bereits 10 Workstreams für Migration nötig
- Alternative: Akzeptiere Framework-Lock-in

**Konsequenzen:**
- (+) Framework austauschbar
- (+) Kein Breaking-Change-Risiko
- (-) Eigene Abstraktion = Maintenance
- (-) Migration 6-8 Wochen

---

#### **ADR-003: Executor Responsibility Boundaries**

**Entscheidung:** Executors orchestrieren Use-Cases, Business Logic in Application Services.

**Kontext:**
- Problem: RefinementExecutor hat 545 LOC mit 6 Responsibilities
- Alternative: Alles in Executor (Status Quo)

**Konsequenzen:**
- (+) SRP erfüllt
- (+) Testability (Mock Services statt vollständigen Executor)
- (+) Wiederverwendbarkeit (Services in mehreren Executors)
- (-) Mehr Indirection
- (-) Initial Refactoring-Aufwand

---

#### **ADR-004: Trigger Strategy**

**Entscheidung:** Nutze ausschließlich Webhook-basiertes Triggering, deaktiviere Polling.

**Kontext:**
- Aktuell: Polling + Webhook laufen parallel
- Problem: Race Conditions, doppelte Workflow-Starts möglich
- GitHub Webhooks sind reliable genug (Retry-Mechanismus)

**Konsequenzen:**
- (+) Einfacherer Code-Path
- (+) Weniger Race Conditions
- (+) Ressourcen-Effizienz (kein Polling)
- (-) Webhook-Setup erforderlich (Operator-Burden)
- (-) Fallback bei Webhook-Ausfall fehlt

---

#### **ADR-005: Metrics Strategy**

**Entscheidung:** Mache Metrics-Subsystem optional (Feature Flag), implementiere Visualization oder entferne.

**Kontext:**
- 259 LOC für Metrics-System ohne Visualisierung
- Refactoring-Plan: "scaffolding for future dashboards"
- YAGNI-Verletzung

**Konsequenzen:**
- **OPTION A (Visualisierung):**
  - (+) Metrics werden genutzt
  - (-) Zusätzlicher Entwicklungsaufwand (Dashboard)

- **OPTION B (Removal):**
  - (+) Code-Reduktion
  - (-) Metrics bei Bedarf neu implementieren

- **OPTION C (Optional mit Flag):**
  - (+) Feature bleibt für zukünftige Nutzung
  - (+) Kein Performance-Overhead wenn disabled
  - (-) Dead Code Risk

**Empfehlung:** Option C kurzfristig, dann A oder B nach Nutzungs-Analyse

---

## 7) Abschluss: Gesamturteil

### Gesamturteil (6-10 Sätze)

Das System leidet unter **massivem Overengineering** nach einem großangelegten Refactoring (10 Workstreams). Die Architektur ist **noch nicht stabilisiert** - Hot Spots zeigen aktive Änderungen an Kern-Komponenten (WorkflowRunner, SDLCWorkflow, Program.cs). **Kritisches Risiko:** Abhängigkeit von zwei Preview-Frameworks (Microsoft.Agents.AI + MCP) ohne Exit-Strategy. Der Refactoring-Plan ist zu **95% complete**, aber 5 offene "Review Findings" zeigen fundamentale Architektur-Probleme (State Management, Label-basierte Steuerung, fehlende MS-Framework-Integration).

**RefinementExecutor (545 LOC)** ist ein Monolith mit 6 Responsibilities – klare **SRP-Verletzung**. **WorkContext** ist ein God Object mit 9 Dependencies – verletzt **Interface Segregation**. **Doppelte State-Verwaltung** (persistent + in-memory) führt zu Inkonsistenzen und Memory Leaks. Die Codebase hat **gute Ansätze** (Clean Architecture Layers, Infrastructure Abstractions), aber **schlechte Execution** (Framework-Lock-in, God Objects, Monolith-Klassen).

Für ein System mit **17.162 LOC** ist die Komplexität **nicht gerechtfertigt**: Keine echte Variation bei Infrastructure (1 GitHub-Client, 1 Git-Provider), keine Metrics-Visualisierung trotz 259 LOC Code, Mode-System existiert aber funktioniert nicht. Das System braucht **Vereinfachung, nicht mehr Features**.

---

### Größte Schwächen

1. **Preview-Framework-Lock-in ohne Exit-Strategy** → Existenzielles Risiko für Production-System
2. **God Objects (WorkContext, RefinementExecutor)** → Verletzt SOLID, untestbar, unwartbar
3. **Doppelte State-Verwaltung** → Race Conditions, Memory Leaks, Inkonsistenzen
4. **Overengineering (Abstraktionen ohne Variation)** → ServiceFactory, Interfaces mit 1 Impl
5. **Instabile Architektur trotz Completion** → 13× CI-Änderungen, 9× Program.cs-Änderungen in 3 Monaten

---

### Empfehlung: **KURS KORRIGIEREN mit Vereinfachung**

**Konkret:**

1. **Kurzfristig (1-2 Wochen):**
   - God Objects splitten (WorkContext → Executor-Contexts)
   - Doppelten State entfernen (nur IWorkflowContext)
   - RefinementExecutor in 5 Services refactoren

2. **Mittelfristig (1-2 Monate):**
   - Framework-Abstraktion einführen (IWorkflowExecutor)
   - Parser-Sprawl konsolidieren (Unified Schema)
   - Webhook ODER Polling entscheiden (nicht beide)

3. **Langfristig (3-6 Monate):**
   - Microsoft.Agents.AI Exit-Strategy umsetzen
   - Domain-Driven Design Layer einführen
   - Metrics implementieren ODER entfernen (kein Scaffolding)

**Nicht tun:**
- ❌ Weitere Features vor Architektur-Stabilisierung
- ❌ Event-Sourcing (zu komplex für aktuellen State)
- ❌ Mehr Abstractions (erst Variation nachweisen)

**Stabilisierungs-Kriterium:**
Zero Hot-Spots in Kern-Komponenten (WorkflowRunner, Executors) über 4 Wochen → **Dann** neue Features.

---

**Ende des Reviews**
