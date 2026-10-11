# Measuring memory-context token costs

The LongMemEval harness reports the tokens used by assembled memory context alongside the full
transcript for the same question. Compare both with answer quality; a smaller context alone does
not establish better retrieval or equivalent answers.

## Reproducing

```bash
dotnet run --project tools/AgentMemory.LongMemEval -c Release -- \
  --prepared-pair --questions 50 --description "Token breakdown"
```

Every telemetry row carries `TokenBreakdown`: per-section costs, the full-history denominator,
and the counting method. `Exact` uses `Microsoft.ML.Tokenizers` with the model's tiktoken encoding.
When no encoding resolves, the counter reports `CharacterHeuristic`; keep that label with the result.

Report the distribution across a frozen question set and retain the run's configuration and quality
verdict. A two-question verification run does not establish a representative compression ratio.
The full-transcript denominator is not a truncated context window or a guarantee that every model
can accept that transcript.

## Related

- [Performance baselines](README.md)
- [LongMemEval results and limitations](longmemeval-results.md)
- [Bulk ingestion](bulk-ingestion.md)
