# Semantic Search — Developer Setup Guide

Everything needed to get "find a poem about..." search running locally, plus the real problems
encountered building it and their actual fixes — not a theoretical list, the specific things that
actually went wrong during development, in enough detail to recognize them again.

If you just want to *use* the published embeddings data, [`ganjoor-embeddings`](https://github.com/ganjoor/ganjoor-embeddings)
is the place to look. This document is about running the .NET side of the feature (the code in
this repo) against that data.

## Table of contents

- [What this feature actually is](#what-this-feature-actually-is)
- [Prerequisites](#prerequisites)
- [Getting the model](#getting-the-model)
- [Getting the embeddings data](#getting-the-embeddings-data)
- [Configuration](#configuration)
- [Running it locally](#running-it-locally)
- [Architecture, briefly](#architecture-briefly)
- [Known issues and their actual fixes](#known-issues-and-their-actual-fixes)

## What this feature actually is

A user types a description of what they're looking for ("a poem about the world's
unfaithfulness") instead of exact keywords. Their query gets embedded into a 1024-dimensional
vector using the same model that embedded every poem's AI-written summary offline; the poems
whose vectors are closest (cosine similarity) come back as results. See
[`ganjoor-embeddings`](https://github.com/ganjoor/ganjoor-embeddings)'s docs for how the poem-side
embeddings were generated — this guide covers the query-time side, which lives in this repo.

## Prerequisites

- **.NET 10 SDK** — check with `dotnet --version`. An older SDK can't build a `net10.0` target at
  all; you'll get a clear build error, not a subtle one, if this is wrong.
- **Windows**, if you're testing against IIS the way production runs — the ONNX Runtime native
  binaries differ per OS/architecture, and at least one real bug (see below) only reproduced on
  Windows, not macOS, despite identical code.
- The base model + tokenizer files, and the embeddings data — see the next two sections.

## Getting the model

**[`onnx-community/Qwen3-Embedding-0.6B-ONNX`](https://huggingface.co/onnx-community/Qwen3-Embedding-0.6B-ONNX)**
on Hugging Face. You need, together in one folder:

- `model.onnx` + `model.onnx_data` (fp32), **or** `model_quantized.onnx` (int8 — what production
  actually uses; ~2.9x faster with negligible accuracy cost, per the model's own published
  benchmarks)
- `vocab.json`
- `merges.txt`

```python
from huggingface_hub import snapshot_download
snapshot_download("onnx-community/Qwen3-Embedding-0.6B-ONNX", local_dir="./ganjoor-model")
```

**Watch the folder structure** — this repo's download layout puts `model*.onnx` files inside an
`onnx/` subfolder, but `vocab.json`/`merges.txt`/`tokenizer.json` in the *parent* folder. RMuseum's
config needs `ModelPath` pointing into `onnx/` and `VocabPath`/`MergesPath` pointing at the parent
— don't assume everything landed in the same directory. If you get a "file not found" for the
tokenizer files, this is almost certainly why.

## Getting the embeddings data

Either download the published data (`embeddings.f32` + `embeddings-index.json`) from
[`ganjoor-embeddings`'s releases](https://github.com/ganjoor/ganjoor-embeddings/releases) or
[Hugging Face](https://huggingface.co/datasets/ganjoor/ganjoor-poem-embeddings), or generate it
yourself using that repo's scripts. Either way you should end up with those two files in one
folder — `verify_embeddings.py` in that repo will confirm the data is intact before you trust it.

## Configuration

`appsettings.json`:

```json
"SemanticSearch": {
  "Enabled": "True",
  "EmbeddingsDirectory": "C:\\path\\to\\embeddings-data",
  "ModelPath": "C:\\path\\to\\ganjoor-model\\onnx\\model_quantized.onnx",
  "VocabPath": "C:\\path\\to\\ganjoor-model\\vocab.json",
  "MergesPath": "C:\\path\\to\\ganjoor-model\\merges.txt",
  "Dimension": "1024"
}
```

**`Enabled` defaults to `False` if the key is missing** — deliberately fail-closed, so an instance
that shouldn't load a ~1GB+ model into memory (see "Architecture" below) never does so by
accident. You need `"True"` explicitly to actually use this locally.

## Running it locally

```bash
dotnet build
dotnet run --project RMuseum
```

Confirm the endpoint works directly before assuming the UI is the problem if something looks off:

```bash
curl -X POST http://localhost:5000/api/ganjoor/search/semantic \
  -H "Content-Type: application/json" \
  -d '{"query": "a poem about the world'"'"'s unfaithfulness", "topK": 5}'
```

## Architecture, briefly

- **`EmbeddingIndex`** — loads `embeddings.f32`/`embeddings-index.json` into memory once, does
  brute-force cosine similarity search (fast enough at this corpus size — no vector database
  needed).
- **`QueryEmbedder`** — embeds a single query using the same ONNX model + tokenizer setup as the
  offline generation pipeline. Must match it exactly — see "Known issues" below for what "exactly"
  turned out to mean in practice.
- **`LazySemanticSearchResources`** / **`LazyQueryScopeIndex`** — defer loading until first real
  use and never throw. This exists because an earlier version's eager, throwing DI factories meant
  a load failure prevented `GanjoorController` itself from being constructed — a 503 on *every*
  endpoint under `/api/ganjoor`, not just this feature, in actual production. Any future resource
  this feature needs to load lazily should follow this same pattern, not a throwing factory.
- **`SemanticSearchController`** — deliberately its own controller, not a method on
  `GanjoorController`, for the same isolation reason.
- **Production runs this on a physically separate domain/app pool** (`ganjgah.ir`, distinct from
  `api.ganjoor.net`) behind the `SemanticSearch:Enabled` flag — so a problem with this one feature
  (a native crash, a memory issue) can't take down the main site. `GanjooRazor`'s
  `APIRoot.SemanticSearchUrl` points at whichever domain should actually serve this.

## Known issues and their actual fixes

### "Could not start git" / a tool that was just installed isn't found

If you install something (Git, an SDK) while Visual Studio/IIS Express is already running, the
already-running process inherited its environment *before* the install — it won't see the update
until fully restarted. A brand-new terminal window works because it's a fresh process; the
already-open IDE doesn't. **Fully close and reopen the IDE**, not just recycle the current session.

### `pip install` fails with "externally-managed-environment" (macOS)

Modern macOS Python (via Homebrew) blocks direct `pip install`. Use a virtual environment:
```bash
python3 -m venv venv && source venv/bin/activate
```

### `huggingface-cli` prints a deprecation warning and does nothing

It's been replaced by `hf`. Use `hf auth login` instead of `huggingface-cli login` — this is not
optional, the old command doesn't actually authenticate anymore despite appearing to run.

### Hugging Face download/upload fails with a `CAS`/`xet` connection error

HF's newer Xet transfer backend can be flaky on some networks, especially slow/unstable ones. Force
the older plain HTTP path:
```bash
export HF_HUB_DISABLE_XET=1
```

### The ONNX model needs more than just `input_ids`/`attention_mask`

This specific model export is KV-cache-enabled (built for autoregressive generation), not a plain
single-pass embedding graph — it also requires `position_ids` and a `past_key_values.N.key`/
`.value` pair **per layer**, even for one uncached pass. **Always run with `--inspect-only`** (in
the `ganjoor-embeddings` generation script) or the equivalent model introspection before assuming
tensor names/shapes — this project's assumptions were wrong on the first attempt, and the model
itself told us the real signature faster than searching documentation did.

### `BpeTokenizer.Create(vocabStream, mergesStream)` silently produces zero tokens for Persian

The simple two-argument overload doesn't do byte-level pre-tokenization — it works for the ASCII
examples in Microsoft's own docs and silently fails (not errors — just produces empty output) for
non-Latin scripts. The fix, found via reflection against the actual installed package (not
documentation, which was stale for this):

```csharp
var bpeOptions = new BpeOptions(vocabPath, mergesPath) { ByteLevel = true };
var tokenizer = BpeTokenizer.Create(bpeOptions);
```

If you hit a similarly unclear `Microsoft.ML.Tokenizers` API question, reflecting the actual
installed assembly's real method signatures beats searching — see the pattern in
`ganjoor-embeddings`' `VERIFICATION.md` for exactly how.

### `new Tokenizer(new Bpe(...))` doesn't compile ("cannot create an instance of abstract type")

An older code example (a 2022 ML.NET blog post) used a construction pattern that no longer applies
to the current `Microsoft.ML.Tokenizers` package — `Tokenizer` is abstract now. Use
`BpeTokenizer.Create(...)` (above), not this older pattern.

### Query embeddings don't match document embeddings even with "correct" tokenization

Compare token ids directly — Python (`tokenizers.Tokenizer.from_file(...)`, the same tokenizer
that generated the corpus) vs. whatever the new consumer's language produces, on several real
query strings, not just one. The specific gap found this way: Python's tokenizer always appends
one extra token (id `151643`, Qwen's end-of-sequence marker) that a from-vocab-files C#
construction doesn't add automatically. Since pooling happens on the *last* token, missing this
token means pooling from a different position than every document embedding used — not a subtle
difference, a real one. Add it manually if your tokenizer setup doesn't include it:
```csharp
var fullTokenIds = new List<int>(tokenIds) { 151643 };
```

### Everything runs without error but the whole IIS site crashes when the endpoint is actually called

A native crash (Windows Event Viewer: `w3wp.exe` faulting in a native DLL, exception code
`c0000005` — an access violation) can't be caught by any C# `try`/`catch` — the process dies
outright. **Test any new native-backed code path (ONNX Runtime, or anything similar) as a
disposable local console app first**, on the same OS/architecture the real deployment uses, before
it goes anywhere near a shared or production environment. In this project's case, the actual root
cause turned out to be an outdated Visual C++ Redistributable on the server — install the latest
from `https://aka.ms/vs/17/release/vc_redist.x64.exe`. If that doesn't resolve a similar crash,
also check the IIS Application Pool's "Enable 32-Bit Applications" setting — a 32-bit worker
process trying to load a 64-bit-only native library fails exactly the same way.

### After a crash, the site stays down even after the underlying cause is fixed

IIS's Rapid-Fail Protection automatically disables an entire Application Pool after repeated
crashes in a short window, and won't restart it on its own. **IIS Manager → Application Pools →
find it → Start** — this is usually all that's needed, not evidence of a deeper unresolved problem.

### Running multiple IIS worker processes multiplies this feature's memory use

Each worker process is a fully separate .NET process — "singleton" only means one instance *per
process*. `EmbeddingIndex` (hundreds of MB) and `QueryEmbedder` (a loaded ONNX model) both get
reloaded, in full, per worker process. 8 worker processes means 8 independent copies. If this
feature is enabled on an app pool running more than one worker process, do the math on whether
that's actually intended before assuming a load failure is the interesting problem.

### `dotnet run` fails with `Interop.Sys.GetCwd()` / "unable to find the specified file"

Not a code problem — the shell's current directory got deleted/replaced out from under it
(typically from re-extracting a zip over a folder you're currently `cd`'d into). `cd` to an
absolute path to get a fresh reference, then retry.
