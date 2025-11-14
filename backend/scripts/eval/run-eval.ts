import 'dotenv/config';
import { NestFactory } from '@nestjs/core';
import { Module } from '@nestjs/common';
import { SentencesService } from '../../src/sentences/sentences.service';
import { LlmModule } from '../../src/llm/llm.module';
import { promises as fs } from 'fs';
import { join } from 'path';
import { judgeWithGemini } from '../../src/eval/judge';

async function main() {
  const projectRoot = join(__dirname, '..', '..');
  const datasetPath = join(projectRoot, 'eval', 'dataset.json');
  const reportsDir = join(projectRoot, 'eval', 'reports');

  @Module({ imports: [LlmModule], providers: [SentencesService], exports: [SentencesService] })
  class EvalModule {}

  const appContext = await NestFactory.createApplicationContext(EvalModule);
  const sentences = appContext.get(SentencesService);

  await fs.mkdir(reportsDir, { recursive: true });
  const datasetRaw = await fs.readFile(datasetPath, 'utf-8');
  const dataset: Array<{ object: string; language?: string }> = JSON.parse(datasetRaw);

  const results: any[] = [];
  for (const item of dataset) {
    const payload = { object: item.object, language: item.language ?? 'pt' } as const;

    const output = await sentences.generateSentences(payload as any);
    const scores = await judgeWithGemini(output);

    results.push({ input: payload, output, scores });
  }

  const avg = (k: 'quality' | 'correctness' | 'usefulness') => {
    const vals = results.map(r => r.scores[k]).filter((n: number) => Number.isFinite(n));
    return vals.length ? Number((vals.reduce((a: number, b: number) => a + b, 0) / vals.length).toFixed(2)) : 0;
  };

  const summary = {
    when: new Date().toISOString(),
    size: results.length,
    averages: {
      quality: avg('quality'),
      correctness: avg('correctness'),
      usefulness: avg('usefulness'),
    },
  };

  const stamp = new Date().toISOString().replace(/[:.]/g, '-');
  const outJson = join(reportsDir, `eval-${stamp}.json`);
  const outMd = join(reportsDir, `eval-${stamp}.md`);

  await fs.writeFile(outJson, JSON.stringify({ summary, results }, null, 2), 'utf-8');
  await fs.writeFile(outMd, renderMarkdown(summary, results.slice(0, 5)), 'utf-8');

  await appContext.close();
  console.log(`\nEvaluation complete. Report saved to:\n- ${outJson}\n- ${outMd}`);
}

function renderMarkdown(summary: any, examples: any[]): string {
  const lines: string[] = [];
  lines.push(`# LLM Evaluation Report`);
  lines.push('');
  lines.push(`Date: ${summary.when}`);
  lines.push('');
  lines.push(`- Size: ${summary.size}`);
  lines.push(`- Averages: quality=${summary.averages.quality}, correctness=${summary.averages.correctness}, usefulness=${summary.averages.usefulness}`);
  lines.push('');
  lines.push('## Sample examples');
  for (const r of examples) {
    lines.push('-'.repeat(40));
    lines.push(`Input: ${JSON.stringify(r.input)}`);
    lines.push(`Output: ${JSON.stringify(r.output)}`);
    lines.push(`Scores: ${JSON.stringify(r.scores)}`);
  }
  lines.push('');
  lines.push('Notes: Scores are 1-5. Higher is better.');
  return lines.join('\n');
}

main().catch(err => {
  console.error('Evaluation failed:', err?.message || err);
  process.exitCode = 1;
});
