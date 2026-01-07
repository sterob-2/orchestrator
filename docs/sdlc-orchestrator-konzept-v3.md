# AI-SDLC Orchestrator - Konsolidiertes Konzept

**Version:** 3.1  
**Status:** Konsolidiert nach Code-Analyse  
**Letzte Aktualisierung:** 2025-01

**Änderungshistorie:**
| Version | Änderung |
|---------|----------|
| 3.1 | DoD Gate: CI/SonarQube Integration statt eigenem Build |
| 3.0 | Code Review Step, Prototyp-Bewertung, Zielarchitektur |
| 2.0 | Initiales konsolidiertes Konzept |

---

## 1. Vision

> **"Die AI als Teammitglied, nicht als Ersatz"**

Ein Tool für professionelle Softwareentwicklung, bei dem:
- Entwickler aus ihrem **normalen Board-Workflow** heraus Aufgaben an AI delegieren
- Die AI als **asynchroner Kollege** arbeitet
- Der Mensch **jederzeit eingreifen** kann
- Ergebnisse **professionellen Standards** entsprechen

---

## 2. Kernprinzip: Minimaler Handlungsrahmen

> **"Offene Anweisungen führen selten zum gewünschten Ergebnis."**

Die gesamte Architektur basiert auf diesem Prinzip:

| Phase | Zweck |
|-------|-------|
| **Refinement** | Eliminiert Unsicherheit VORHER |
| **Playbook** | Architektur-Constraints binden die AI |
| **TechLead Spec** | Präzise Vorgaben (Touch List, Interfaces, Szenarien) |
| **Gates** | Automatische Qualitätsprüfung |
| **Code Review** | AI prüft AI-Output vor Mensch |

**Je kleiner der Handlungsrahmen, desto besser das Ergebnis.**

---

## 3. Architektur

### 3.1 Zwei-Ebenen-Modell

```
┌─────────────────────────────────────────────────────────────────┐
│  EBENE 1: GITHUB (Sichtbarkeit für Menschen)                    │
│                                                                 │
│  Labels = View auf Workflow-Zustand                             │
│  • Menschen sehen auf dem Board wo jedes Ticket steht           │
│  • Labels werden vom System synchronisiert                      │
│  • Mensch kann via Label eingreifen                             │
├─────────────────────────────────────────────────────────────────┤
│  EBENE 2: WORKFLOW ENGINE (Steuerung)                           │
│                                                                 │
│  MS Agent Framework Workflow = Source of Truth                  │
│  • Steuert Ablauf (Graph mit Conditional Edges)                 │
│  • Speichert Zustand (Checkpointing)                            │
│  • Handled Fehler und Retries                                   │
│  • Aktualisiert Labels automatisch                              │
└─────────────────────────────────────────────────────────────────┘
```

### 3.2 Zielarchitektur (Ordnerstruktur)

```
src/Orchestrator.App/
├── Program.cs                          # Minimal: Config laden, Watcher starten
│
├── Core/
│   ├── Models/
│   │   ├── WorkItem.cs
│   │   ├── WorkflowInput.cs
│   │   ├── GateResult.cs
│   │   ├── ParsedSpec.cs
│   │   ├── TouchListEntry.cs
│   │   └── ProjectContext.cs
│   │
│   ├── Interfaces/
│   │   ├── IGitHubClient.cs
│   │   ├── IRepoGit.cs
│   │   ├── IRepoWorkspace.cs
│   │   └── ILlmClient.cs
│   │
│   └── Configuration/
│       ├── OrchestratorConfig.cs
│       ├── LabelConfig.cs
│       └── WorkflowConfig.cs
│
├── Workflows/
│   ├── WorkflowFactory.cs
│   ├── WorkflowRunner.cs
│   │
│   ├── Executors/
│   │   ├── ExecutorBase.cs
│   │   ├── RefinementExecutor.cs
│   │   ├── TechLeadExecutor.cs
│   │   ├── DevExecutor.cs
│   │   ├── CodeReviewExecutor.cs         # NEU
│   │   └── ReleaseExecutor.cs
│   │
│   ├── Gates/
│   │   ├── DorGateExecutor.cs
│   │   ├── SpecGateExecutor.cs
│   │   └── DodGateExecutor.cs
│   │
│   └── EventHandlers/
│       ├── LabelSyncHandler.cs
│       └── HumanInLoopHandler.cs
│
├── Infrastructure/
│   ├── GitHub/
│   │   └── OctokitGitHubClient.cs        # BEHALTEN
│   ├── Git/
│   │   └── RepoGit.cs                    # BEHALTEN
│   ├── Filesystem/
│   │   └── RepoWorkspace.cs              # BEHALTEN
│   ├── Llm/
│   │   └── LlmClient.cs                  # BEHALTEN
│   └── Mcp/
│       └── McpClientManager.cs           # BEHALTEN
│
├── Parsing/
│   ├── SpecParser.cs
│   ├── TouchListParser.cs
│   ├── GherkinValidator.cs
│   └── PlaybookParser.cs
│
├── Utilities/
│   ├── CodeHelpers.cs                    # Ex AgentHelpers.cs
│   └── TemplateUtil.cs                   # Ex AgentTemplateUtil.cs
│
└── Watcher/
    └── GitHubIssueWatcher.cs
```

### 3.3 Code-Migration (Prototyp → Ziel)

| Kategorie | Behalten | Wegwerfen |
|-----------|----------|-----------|
| **Infrastructure** | OctokitGitHubClient, RepoGit, RepoWorkspace, LlmClient, McpClientManager | - |
| **Utilities** | AgentHelpers, AgentTemplateUtil | - |
| **Agents** | - | Alle (PlannerAgent, TechLeadAgent, DevAgent, etc.) |
| **Orchestration** | - | Program.cs (738 Zeilen), SDLCWorkflow, WorkflowExecutors |

**Begründung:** Infrastructure ist solide. Orchestrierung muss neu wegen Paradigmenwechsel (Label-State-Machine → Workflow Graph).

---

## 4. Workflow Pipeline

### 4.1 Vollständiger Graph

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                                                                             │
│   [ready-for-agent]                                                         │
│          │                                                                  │
│          ▼                                                                  │
│   ┌──────────────┐                                                          │
│   │   Context    │  Playbook laden, Referenzen einfrieren                   │
│   │   Builder    │                                                          │
│   └──────┬───────┘                                                          │
│          │                                                                  │
│          ▼                                                                  │
│   ┌──────────────┐     ┌──────────────┐                                     │
│   │  Refinement  │────▶│   DoR Gate   │                                     │
│   │   Executor   │◀────│              │ FAIL: zurück mit Feedback           │
│   └──────────────┘     └──────┬───────┘                                     │
│                               │ PASS                                        │
│                               ▼                                             │
│                        ┌──────────────┐     ┌──────────────┐                │
│                        │   TechLead   │────▶│  Spec Gate   │                │
│                        │   Executor   │◀────│              │ FAIL           │
│                        └──────────────┘     └──────┬───────┘                │
│                                                    │ PASS                   │
│                                                    ▼                        │
│                                             ┌──────────────┐                │
│                                             │     Dev      │                │
│                                             │   Executor   │                │
│                                             └──────┬───────┘                │
│                                                    │                        │
│                                                    ▼                        │
│                                             ┌──────────────┐                │
│                                             │    Code      │                │
│                                             │   Review     │◀───────┐       │
│                                             └──────┬───────┘        │       │
│                                                    │                │       │
│                                    ┌───────────────┴────────┐       │       │
│                                    │                        │       │       │
│                                    ▼                        ▼       │       │
│                              (APPROVED)            (CHANGES_REQ)    │       │
│                                    │                        │       │       │
│                                    ▼                        └───────┘       │
│                             ┌──────────────┐           (zurück zu Dev)      │
│                             │  DoD Gate    │                                │
│                             └──────┬───────┘                                │
│                                    │                                        │
│                        ┌───────────┴───────────┐                            │
│                        │                       │                            │
│                        ▼                       ▼                            │
│                     (PASS)                  (FAIL)                          │
│                        │                       │                            │
│                        ▼                       └──────▶ Dev Executor        │
│                 ┌──────────────┐                                            │
│                 │   Release    │                                            │
│                 │   Executor   │                                            │
│                 └──────┬───────┘                                            │
│                        │                                                    │
│                        ▼                                                    │
│                     [done]                                                  │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

### 4.2 Workflow als Code

```csharp
public Workflow Build()
{
    var contextBuilder = new ContextBuilderStep(_services);
    var refinement = new RefinementExecutor(_services);
    var dorGate = new DorGateExecutor();
    var techLead = new TechLeadExecutor(_services);
    var specGate = new SpecGateExecutor(_services);
    var dev = new DevExecutor(_services);
    var codeReview = new CodeReviewExecutor(_services);
    var dodGate = new DodGateExecutor(_services);
    var release = new ReleaseExecutor(_services);

    return new WorkflowBuilder()
        .SetStartExecutor(contextBuilder)
        
        // Context → Refinement
        .AddEdge(contextBuilder, refinement)
        
        // Refinement ↔ DoR Gate (Loop)
        .AddEdge(refinement, dorGate)
        .AddEdge(dorGate, techLead, r => r is GateResult g && g.Passed)
        .AddEdge(dorGate, refinement, r => r is GateResult g && !g.Passed)
        
        // TechLead ↔ Spec Gate (Loop)
        .AddEdge(techLead, specGate)
        .AddEdge(specGate, dev, r => r is GateResult g && g.Passed)
        .AddEdge(specGate, techLead, r => r is GateResult g && !g.Passed)
        
        // Dev → Code Review
        .AddEdge(dev, codeReview)
        
        // Code Review ↔ Dev (Loop bei Findings)
        .AddEdge(codeReview, dodGate, r => r is CodeReviewResult cr && cr.Approved)
        .AddEdge(codeReview, dev, r => r is CodeReviewResult cr && !cr.Approved)
        
        // DoD Gate ↔ Dev (Loop)
        .AddEdge(dodGate, release, r => r is GateResult g && g.Passed)
        .AddEdge(dodGate, dev, r => r is GateResult g && !g.Passed)
        
        // Release → Ende
        .WithCheckpointing(_checkpointStore)
        .Build();
}
```

---

## 5. Labels

### 5.1 Trigger-Labels (Mensch → System)

| Label | Wirkung |
|-------|---------|
| `ready-for-agent` | Startet Workflow |
| `mode:batch` | Überschreibt Development Mode |
| `mode:tdd` | Überschreibt Development Mode |
| `reset` | Workflow abbrechen, von vorne |

### 5.2 Status-Labels (System → Board)

| Label | Bedeutung |
|-------|-----------|
| `agent:refinement` | Refinement läuft |
| `agent:techlead` | Tech Spec wird erstellt |
| `agent:dev` | Implementierung läuft |
| `agent:code-review` | AI Code Review läuft |
| `agent:blocked` | Wartet auf menschliche Antwort |
| `dor:passed` | Definition of Ready erfüllt |
| `spec:approved` | Spec Gate bestanden |
| `review:approved` | Code Review bestanden |
| `done` | Workflow abgeschlossen |

### 5.3 Gate-Labels

| Label | Bedeutung |
|-------|-----------|
| `dor:passed` / `dor:failed` | DoR Gate Ergebnis |
| `spec:approved` / `spec:failed` | Spec Gate Ergebnis |
| `review:approved` / `review:changes-requested` | Code Review Ergebnis |
| `dod:passed` / `dod:failed` | DoD Gate Ergebnis |

---

## 6. Die Executors

### 6.1 RefinementExecutor

**Aufgabe:** Story zur "Definition of Ready" bringen

| Darf | Darf NICHT |
|------|------------|
| Fragen stellen | Anforderungen erfinden |
| Unklarheiten identifizieren | Annahmen treffen |
| Akzeptanzkriterien vorschlagen | Features hinzufügen |
| Komplexitäts-Indikatoren liefern | Story Points festlegen |

**Output:** `RefinementResult`
```csharp
record RefinementResult(
    string ClarifiedStory,
    List<string> AcceptanceCriteria,
    List<string> OpenQuestions,
    ComplexityIndicators Complexity
);
```

### 6.2 TechLeadExecutor

**Aufgabe:** Präzise technische Spezifikation erstellen

| Darf | Darf NICHT |
|------|------------|
| Architektur aus Playbook ableiten | Neue Patterns erfinden |
| Interfaces definieren | Anforderungen ändern |
| Testszenarien formulieren | Scope erweitern |
| Touch List erstellen | Playbook ignorieren |

**Output:** `TechLeadResult`
```csharp
record TechLeadResult(
    string SpecPath,
    ParsedSpec ParsedSpec,
    List<string> UsedFrameworks,
    List<string> ReferencedPatterns
);
```

### 6.3 DevExecutor

**Aufgabe:** Code implementieren der Spec entspricht

| Darf | Darf NICHT |
|------|------------|
| Code schreiben der Spec entspricht | Von Spec abweichen |
| Tests schreiben | Anforderungen interpretieren |
| Rückfragen stellen | Annahmen treffen |
| Nur Touch List Dateien ändern | Andere Dateien ändern |

**Output:** `DevResult`
```csharp
record DevResult(
    bool Success,
    int IssueNumber,
    List<string> ChangedFiles,
    string CommitSha
);
```

### 6.4 CodeReviewExecutor (NEU)

**Aufgabe:** AI Code Review vor menschlichem Review

| Prüft | Severity |
|-------|----------|
| Correctness (Logikfehler, Null Refs) | BLOCKER |
| Spec Compliance (AKs erfüllt?) | BLOCKER/MAJOR |
| Code Quality (Naming, Duplication) | MINOR |
| Architecture (Playbook-Verletzungen) | MAJOR |
| Security (Input Validation) | BLOCKER |
| Testing (Tests vorhanden?) | MAJOR |

**Regeln:**
- ≥1 BLOCKER → zurück zu Dev
- ≥3 MAJOR → zurück zu Dev
- Max 3 Review-Fix-Zyklen, danach Mensch

**Output:** `CodeReviewResult`
```csharp
record CodeReviewResult(
    bool Approved,
    List<ReviewFinding> Findings,
    string Summary,
    string ReviewPath,
    bool RequiresHumanReview = false
);
```

### 6.5 ReleaseExecutor

**Aufgabe:** PR finalisieren

| Darf | Darf NICHT |
|------|------------|
| PR-Beschreibung erstellen | Code ändern |
| Release Notes generieren | Ohne Approval mergen |
| Labels aktualisieren | DoD überspringen |

**Output:** `ReleaseResult`
```csharp
record ReleaseResult(
    int PrNumber,
    string PrUrl,
    bool Merged
);
```

---

## 7. Gates

### 7.1 DoR Gate (Definition of Ready)

**7 Automatische Prüfkriterien:**

| ID | Kriterium | Prüflogik |
|----|-----------|-----------|
| DoR-01 | Titel vorhanden | `!string.IsNullOrWhiteSpace(title)` |
| DoR-02 | Beschreibung ≥50 Zeichen | `description.Length >= 50` |
| DoR-03 | Min. 3 Akzeptanzkriterien | `acceptanceCriteria.Count >= 3` |
| DoR-04 | AKs testbar formuliert | Enthält Given/When/Then oder Aktionsverben |
| DoR-05 | Keine offenen Fragen | `openQuestions.Count == 0` |
| DoR-06 | Schätzung vorhanden | Label `estimate:*` vorhanden |
| DoR-07 | Kein Blocker | Kein Label `blocked` |

**Bei FAIL:** Zurück zu Refinement mit Checkliste was fehlt

### 7.2 Spec Gate

**4 Prüfkategorien:**

| Kategorie | Prüft |
|-----------|-------|
| **Struktur** | Alle Pflicht-Sektionen vorhanden |
| **Inhalt** | Touch List Format, Gherkin gültig, Test-Dateien |
| **Referenz** | Frameworks im Playbook, Dateien existieren |
| **Konsistenz** | Szenarien ↔ Testmatrix, Komponenten ↔ Touch List |

**Pflicht-Sektionen:**
1. Ziel
2. Nicht-Ziele
3. Komponenten
4. Touch List (modify/add/delete/forbidden)
5. Interfaces
6. Szenarien (min. 3 Gherkin)
7. Sequenz (min. 2 Schritte)
8. Testmatrix

**Bei FAIL:** Zurück zu TechLead mit Fehlerdetails

### 7.3 DoD Gate (Definition of Done)

**Designprinzip:** CI als Source of Truth - kein eigener Build/Test, nur API-Abfragen.

**5 Prüfkategorien:**

| Kategorie | Quelle | Kriterien |
|-----------|--------|-----------|
| CI Status | GitHub Checks API | DoD-01 bis DoD-03 |
| Quality Gate | SonarQube API | DoD-10 bis DoD-15 |
| Spec Compliance | Eigene Prüfung | DoD-20 bis DoD-23 |
| Code Review | Workflow State | DoD-30, DoD-31 |
| Cleanup | Eigene Prüfung | DoD-40 bis DoD-42 |

**CI-Kriterien (von GitHub):**
- DoD-01: CI Workflow grün
- DoD-02: Alle Required Checks grün
- DoD-03: Keine Pending Checks

**SonarQube-Kriterien:**
- DoD-10: Quality Gate OK
- DoD-11: Keine neuen Bugs
- DoD-12: Keine neuen Vulnerabilities
- DoD-13: Keine Critical Code Smells
- DoD-14: Coverage ≥ Threshold
- DoD-15: Duplication ≤ Threshold

**Spec-Kriterien:**
- DoD-20: Alle AKs abgehakt
- DoD-21: Touch List eingehalten
- DoD-22: Forbidden Files unverändert
- DoD-23: Alle geplanten Dateien geändert

**Review-Kriterien:**
- DoD-30: AI Code Review bestanden
- DoD-31: Keine Blocker offen

**Cleanup-Kriterien:**
- DoD-40: Keine TODOs in geänderten Dateien
- DoD-41: Keine FIXMEs in geänderten Dateien
- DoD-42: Spec STATUS = COMPLETE

**Bei FAIL:** Zurück zu Dev mit Details und Links zu CI/Sonar

---

## 8. Spec-Schema (Zielformat)

```markdown
# Spec: [Issue-Titel]

STATUS: DRAFT | COMPLETE
UPDATED: [UTC Timestamp]

## Ziel
[Was soll erreicht werden - 2-3 Sätze]

## Nicht-Ziele
[Was explizit nicht Teil dieser Story ist]

## Komponenten
[Betroffene Module/Namespaces]

## Touch List

| Aktion | Pfad | Beschreibung |
|--------|------|--------------|
| modify | src/... | ... |
| add    | src/... | ... |
| add    | tests/... | Test für ... |
| forbidden | src/Core/... | Nicht anfassen |

## Interfaces

```csharp
public record SomeCommand(...);
public interface ISomeService { }
```

## Szenarien

```gherkin
Scenario: Happy Path
  Given ...
  When ...
  Then ...

Scenario: Error Case
  Given ...
  When ...
  Then ...

Scenario: Edge Case
  Given ...
  When ...
  Then ...
```

## Sequenz

1. Schritt 1 mit Input/Output
2. Schritt 2 mit Input/Output
3. ...

## Testmatrix

| Akzeptanzkriterium | Szenario | Test-Typ | Test-Datei |
|--------------------|----------|----------|------------|
| AK-1 | Happy Path | Unit | SomeTests.cs |
| AK-2 | Error Case | Unit | SomeTests.cs |

## Architektur-Referenzen

- Pattern: PAT-01 (Result Pattern) - siehe `src/Common/Result.cs`
- Pattern: PAT-02 (Command Handler) - siehe `src/Handlers/`
- Playbook-Version: 1.0
- Keine verbotenen Patterns verwendet

## Risiken

| Risiko | Mitigation |
|--------|------------|
| ... | ... |
```

---

## 9. Architecture Playbook

### 9.1 Zweck

Das Playbook bindet die AI an Architektur-Entscheidungen des Teams:
- Erlaubte Frameworks und Versionen
- Erlaubte Patterns mit Referenzdateien
- Verbotene Patterns
- Coding Standards

### 9.2 Struktur

```yaml
# docs/architecture-playbook.yaml

project: "Orchestrator"
version: "1.0"

allowed_frameworks:
  - id: FW-01
    name: "ASP.NET Core"
    version: "8.x"
    
forbidden_frameworks:
  - name: "Newtonsoft.Json"
    use_instead: "System.Text.Json"

allowed_patterns:
  - id: PAT-01
    name: "Result Pattern"
    reference: "src/Common/Result.cs"
  - id: PAT-02
    name: "Command Handler"
    reference: "src/Handlers/BaseHandler.cs"

forbidden_patterns:
  - id: BAN-01
    name: "Service Locator"
  - id: BAN-02
    name: "Singleton mit Mutable State"
```

### 9.3 Validierung

- **ContextBuilder:** Lädt Playbook beim Workflow-Start
- **TechLeadExecutor:** Muss Patterns aus Playbook referenzieren
- **SpecGate:** Prüft ob verwendete Frameworks/Patterns erlaubt sind
- **CodeReviewExecutor:** Prüft auf verbotene Patterns im Code

---

## 10. Development Modes

### 10.1 Drei Modi

| Modus | Beschreibung | LLM-Calls | Wann |
|-------|--------------|-----------|------|
| **minimal** | DevAgent macht Code + Tests + Self-Check | ~3-5 | Standard |
| **batch** | Tests zuerst, dann implementieren | ~6-10 | Bei Qualitätsproblemen |
| **tdd** | RED→GREEN→REFACTOR Loop | ~15-30 | Kritische Features |

### 10.2 Konfiguration

```yaml
development:
  default_mode: minimal
  
  # Per Label überschreibbar: mode:batch, mode:tdd
  
  escalation:
    to_batch_if_approval_rate_below: 70
    to_tdd_if_approval_rate_below: 50
```

---

## 11. Iteration Limits

Um Endlosschleifen zu vermeiden:

| Loop | Max Iterationen | Bei Überschreitung |
|------|-----------------|-------------------|
| Refinement ↔ DoR | 5 | Human-in-the-Loop |
| TechLead ↔ Spec Gate | 3 | Human-in-the-Loop |
| Dev ↔ Code Review | 3 | Human Review Required |
| Dev ↔ DoD Gate | 3 | Human-in-the-Loop |

---

## 12. Metriken

```yaml
pro_workflow:
  - issue_number
  - mode (minimal/batch/tdd)
  - llm_call_count
  - tokens_used
  - duration_minutes
  - refinement_iterations
  - techlead_iterations
  - dev_iterations
  - code_review_iterations
  - code_review_findings_count
  - pr_approved (boolean)

aggregiert:
  - approval_rate
  - avg_iterations_per_stage
  - avg_cost_per_story
  - code_review_first_pass_rate
  - finding_distribution_by_category
```

---

## 13. Technische Umsetzung

### 13.1 Stack

| Komponente | Technologie |
|------------|-------------|
| Runtime | .NET 10 |
| Workflow Engine | Microsoft Agent Framework |
| Board Integration | GitHub Issues + Projects |
| LLM | OpenAI-kompatible API |
| Container | Docker |

### 13.2 Keine Jira-Abstraktion (YAGNI)

- Direkt gegen GitHub implementieren
- Interface nur für Unit-Tests
- Bei Bedarf später abstrahieren

---

## 14. Nicht im Scope

| Thema | Entscheidung | Begründung |
|-------|--------------|------------|
| Epic Splitting | Manuell | Braucht Domänenwissen |
| Estimation Agent | Nein | Team soll schätzen |
| AI-zu-AI Eskalation | Nein | Direkt zu Mensch |
| Jira-Abstraktion | Später | YAGNI |
| Multi-Repo | Später | V1 = ein Repo |

---

## 15. Erfolgskriterien

| KPI | Ziel (nach 3 Monaten) |
|-----|----------------------|
| Approval Rate | > 75% |
| Code Review First-Pass-Rate | > 60% |
| Avg Iterations (Dev) | < 2 |
| Stories ohne manuellen Eingriff | > 50% |

---

## Anhang A: Entscheidungslog

| Thema | Entscheidung | Begründung |
|-------|--------------|------------|
| Labels vs Workflow | Hybrid | Labels = View, Workflow = Control |
| Code Review Step | Ja, vor DoD Gate | AI prüft AI vor Mensch |
| Jira-Abstraktion | Nein (YAGNI) | Spätere Abstraktion einfacher |
| Prototyp-Code | Infrastructure behalten, Rest neu | Paradigmenwechsel |
| Spec-Schema | Strukturiert mit Pflicht-Sektionen | Minimaler Handlungsrahmen |
| Playbook | Verpflichtend | Bindet AI an Team-Architektur |

---

## Anhang B: Referenzdokumente

| Dokument | Beschreibung |
|----------|--------------|
| `dor-gate-spezifikation.md` | Vollständige DoR Gate Spezifikation |
| `spec-gate-spezifikation.md` | Vollständige Spec Gate Spezifikation |
| `code-review-step-spezifikation.md` | Code Review Executor Details |
| `architecture-playbook-template-v2.md` | Playbook Template |
| `code-bewertung-behalten-wegwerfen.md` | Prototyp-Analyse |
| `zielarchitektur-refactoring-plan.md` | Implementierungsplan |
