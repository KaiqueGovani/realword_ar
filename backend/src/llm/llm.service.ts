import { CACHE_MANAGER } from '@nestjs/cache-manager';
import { Inject, Injectable, Logger } from '@nestjs/common';
import axios from 'axios';
import type { Cache } from 'cache-manager';
import { CreateSentenceDto } from 'src/sentences/dto/create-sentence.dto';
import { SentencesDto } from 'src/sentences/dto/sentences.dto';
import { LLMResponse } from './llm.interface';
import { performance } from 'node:perf_hooks';

@Injectable()
export class LlmService {
  private readonly logger = new Logger(LlmService.name);
  private readonly googleModel = process.env.GOOGLE_MODEL || 'gemini-2.5-flash';
  private readonly googleApiKey = process.env.GOOGLE_API_KEY || process.env.GEMINI_API_KEY || '';
  private readonly apiVersion: 'v1' | 'v1beta' =
    process.env.GOOGLE_API_VERSION === 'v1beta' ? 'v1beta' : 'v1';

  public constructor(@Inject(CACHE_MANAGER) private readonly cacheManager: Cache) {}

  public async generateSentences(params: CreateSentenceDto): Promise<SentencesDto> {
    const { object, language } = params;

    const cacheKey = `${object}_${language}`;
    const cached = await this.cacheManager.get(cacheKey);

    if (cached) {
      this.logger.debug(`💾 Cache hit for "${cacheKey}"`);
      return cached as SentencesDto;
    }

  this.logger.debug(`🚀 Cache miss for "${cacheKey}" — calling Google AI (Gemini)...`);
    const result = await this.callGemini(params);

    await this.cacheManager.set(cacheKey, result);
    return result;
  }

  private async callGemini(params: CreateSentenceDto): Promise<SentencesDto> {
    const { object } = params;
    const prompt = this.createPrompt(params);

    try {
      const { value: rawResponse, latencyMs } = await this.callWithLatency(() => this.sendGoogleRequest(prompt));
      this.logger.debug(`⏱️ Gemini latency: ${latencyMs.toFixed(0)} ms`);
      const cleanedText = this.cleanResponse(rawResponse);

      this.logger.debug(`🧩 Cleaned response: ${cleanedText}`);

      const parsedResponse = this.parseResponse(cleanedText, object);

      if (parsedResponse) {
        return parsedResponse;
      }

      this.logger.warn(`⚠️ Could not parse response as JSON for "${object}". Returning fallback.`);
      return this.generateFallbackSentences(params);
    } catch (error) {
      const errorMessage = error instanceof Error ? error.message : 'Unknown error';
      this.logger.error(`❌ Error calling Google AI: ${errorMessage}`);
      return this.generateFallbackSentences(params);
    }
  }

  private async sendGoogleRequest(prompt: string): Promise<string> {
    if (!this.googleApiKey) {
      throw new Error('Google API key not found. Set GOOGLE_API_KEY (or GEMINI_API_KEY) in your environment.');
    }

    const url = `https://generativelanguage.googleapis.com/${this.apiVersion}/models/${encodeURIComponent(
      this.googleModel,
    )}:generateContent`;

    const body = this.buildRequestBody(prompt);

    const response = await axios.post<LLMResponse>(url, body, {
      headers: {
        'Content-Type': 'application/json',
        'x-goog-api-key': this.googleApiKey,
      },
      timeout: 30000,
    });
    return this.extractText(response.data);
  }

  private async callWithLatency<T>(fn: () => Promise<T>): Promise<{ value: T; latencyMs: number }> {
    const start = performance.now();
    try {
      const value = await fn();
      const latencyMs = performance.now() - start;
      return { value, latencyMs };
    } catch (err) {
      const latencyMs = performance.now() - start;
      this.logger.warn(`⏱️ Gemini call failed after ${latencyMs.toFixed(0)} ms`);
      throw err;
    }
  }

  private buildRequestBody(prompt: string) {
    return {
      contents: [
        {
          role: 'user',
          parts: [{ text: prompt }],
        },
      ],
      generationConfig: {
        temperature: 0.2,
      },
    } as const;
  }

  private extractText(data: LLMResponse): string {
    const candidates = data?.candidates;
    if (Array.isArray(candidates) && candidates.length > 0) {
      const parts = candidates[0]?.content?.parts;
      if (Array.isArray(parts) && parts.length > 0) {
        const text = parts[0]?.text ?? '';
        if (typeof text === 'string' && text.trim().length > 0) return text;
      }
    }

    if (typeof data?.output_text === 'string' && data.output_text.trim().length > 0) {
      return data.output_text;
    }
    if (typeof data?.text === 'string' && data.text.trim().length > 0) {
      return data.text;
    }

    return '';
  }

  private cleanResponse(rawText: string): string {
    const text = rawText.trim();

    if (text.startsWith('```')) {
      return text.replace(/```json|```/gi, '').trim();
    }

    return text;
  }

  private parseResponse(text: string, object: string): SentencesDto | null {
    this.logger.debug(`📝 LLM raw response for "${object}": ${text}`);

    try {
      const parsed: unknown = JSON.parse(text);

      if (this.isValidSentencesDto(parsed)) {
        this.logger.debug(`✅ Parsed structured response for "${object}"`);
        return parsed as SentencesDto;
      }

      return null;
    } catch (_err) {
      return null;
    }
  }

  private isValidSentencesDto(parsed: unknown): boolean {
    return (
      parsed !== null &&
      typeof parsed === 'object' &&
      'phrases' in parsed &&
      'translations' in parsed &&
      Array.isArray(parsed.phrases) &&
      Array.isArray(parsed.translations)
    );
  }

  private generateFallbackSentences(params: CreateSentenceDto): SentencesDto {
    const { object, language } = params;

    return {
      phrases: [`This is a ${object}.`, `The ${object} is on the table.`],
      translations:
        language === 'pt'
          ? ['Isto é um(a) ' + object + '.', 'O(a) ' + object + ' está na mesa.']
          : [`This is a ${object}.`, `The ${object} is on the table.`],
    };
  }

  private createPrompt(params: CreateSentenceDto): string {
    const { object, language } = params;

    return `You are an assistant for English learners who are absolute beginners.
            Generate exactly two extremely simple English sentences that include the noun "${object}".
            Then, translate each sentence into ${language}.

            Rules:
            - Use correct and natural grammar in both languages.
            - Keep each English sentence under 10 words.
            - Make sure each translation matches its English sentence exactly in meaning.
            - Avoid literal errors (for example, do not translate "sit" as "consigo").
            - Use common, natural verbs for ${language}, not word-for-word mistakes.
            - Do not add explanations, extra text, or markdown.

            Output ONLY a valid JSON object in this format:
            {
              "phrases": ["English sentence 1.", "English sentence 2."],
              "translations": ["Translation 1.", "Translation 2."]
            }`;
  }
}
