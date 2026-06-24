**Simple Coding Agent built using Microsoft Agent Framework**

[![.NET 10](https://img.shields.io/badge/.NET-10.0-512bd4?style=for-the-badge&logo=.net)](https://dotnet.microsoft.com/)
[![License: MIT](https://img.shields.io/badge/License-MIT-green.svg?style=for-the-badge)](LICENSE)

---

### Core Components

| Directory     | Purpose                                          |
|---------------|--------------------------------------------------|
| `Agents/`     | Factory methods for planner, builder, and question agents |
| `Executors/`  | Router, CodePlanner, CodeBuilder, DirectAnswer executors |
| `Events/`     | Workflow event types and progress data classes   |
| `skills/`     | Domain-specific SKILL.md files and plugin directories |
| `AgentTools.cs` | Shared agent utility helpers                  |

---

## Prerequisites

- **.NET 10 SDK** – [download](https://dotnet.microsoft.com/download)
- A local or remote Ollama instance (default: `http://localhost:11434/v1/`) running a supported model
- Any OpenAI-compatible model API endpoint is also supported

---

## Setup

### 1. Clone the repository

### 2. Configure your environment

Edit `.env` to point at your desired model endpoint:

```env
# Model ID — any Hugging Face or Ollama model name
MODEL_ID=qwen3.6:35b-a3b

# API key -- "ollama" for local; your actual key for cloud endpoints
API_KEY=ollama

# OpenAI-compatible API endpoint
OPENAI_ENDPOINT=http://localhost:11434/v1/
```

> **Tip:** Any vendor that implements the OpenAI chat completions API (Groq, Azure OpenAI, Together AI, etc.) works out of the box.

### 3. Restore packages and build

```bash
dotnet restore
dotnet build
```

### 4. Run the agent locally

```bash
dotnet run
```

You'll see an interactive prompt:

```
dotnuxt - .NET Coding Agent (Microsoft Agent Framework)
Model: qwen3.6:35b-a3b
Workspace: /path/to/current/project
Skills directory: /path/to/dotnuxt/skills
Usage: /build <prompt>  — Write code, create files, build projects
       /ask   <prompt>  — Answer questions, explain concepts, provide guidance
       /skills          — List available skills
       /plugins         — List available plugins
       /exit            — Quit

> /ask hello, world!
```

### 5. Package and install as a CLI tool

Create a local NuGet package:

```bash
dotnet pack -c Release
```

Install it as a global .NET tool:

```bash
dotnet tool install --global dotnuxt --add-source ./nupkg
```

Update an existing global install:

```bash
dotnet tool update --global dotnuxt --add-source ./nupkg
```

Run it from any project directory:

```bash
cd /path/to/another/project
dotnuxt
```

Or explicitly choose a workspace:

```bash
dotnuxt --cwd /path/to/another/project
```

The installed tool keeps bundled `skills/` and `.agents/` assets next to the executable, while the workspace remains the directory where you launch `dotnuxt` or the directory passed to `--cwd`.

---

## Required Environment Variables

| Variable          | Description                                | Default                      |
|-------------------|--------------------------------------------|------------------------------|
| `MODEL_ID`       | Model identifier (e.g. `qwen3.6:35b-a3b`) | `qwen3.6:35b-a3b`           |
| `API_KEY`        | API key or `"ollama"` for local            | `ollama`                    |
| `OPENAI_ENDPOINT`| OpenAI-compatible chat completions URL     | `http://localhost:11434/v1/`|

---

## Environment Configuration Order

`dotnuxt` loads configuration in this precedence order:

1. `.env` in the active workspace
2. `%USERPROFILE%\.dotnuxt\.env` on Windows, or `$HOME/.dotnuxt/.env` on macOS/Linux
3. Existing process environment variables
4. Built-in defaults
