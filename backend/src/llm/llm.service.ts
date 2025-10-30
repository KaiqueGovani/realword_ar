import { CACHE_MANAGER } from '@nestjs/cache-manager';
import { Inject, Injectable, Logger } from '@nestjs/common';
import axios from 'axios';
import type { Cache } from 'cache-manager';
import { LLMResponse } from './llm.interface';

@Injectable()
export class LlmService {
  private readonly logger = new Logger(LlmService.name);
  private readonly ollamaUrl = process.env.OLLAMA_URL || 'http://localhost:11434/api/chat';
  private readonly model = process.env.OLLAMA_MODEL || 'phi3';

  constructor(@Inject(CACHE_MANAGER) private readonly cacheManager: Cache) {}

  async generateSentences(
    object: string,
    language: string = 'en',
  ): Promise<{ phrases: string[]; translations: string[] }> {
    const cacheKey = `${object}_${language}`;
    const cached = await this.cacheManager.get(cacheKey);

    if (cached) {
      this.logger.debug(`💾 Cache hit for "${cacheKey}"`);
      return cached as { phrases: string[]; translations: string[] };
    }

    this.logger.debug(`🚀 Cache miss for "${cacheKey}" — calling Ollama...`);
    const result = await this.callOllama(object, language);

    await this.cacheManager.set(cacheKey, result, 86400);
    return result;
  }

  private async callOllama(object: string, language: string): Promise<{ phrases: string[]; translations: string[] }> {
    const prompt = `You are an assistant for English learners who are absolute beginners.
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

    try {
      const response = await axios.post<LLMResponse>(this.ollamaUrl, {
        model: this.model,
        messages: [{ role: 'user', content: prompt }],
        stream: false,
        options: {
          temperature: 0.2,
        },
      });

      const text = response.data.message?.content || response.data.response?.trim() || '';

      this.logger.debug(`🧠 LLM raw response for "${object}": ${text}`);

      let cleaned = text.trim();
      if (cleaned.startsWith('```')) {
        cleaned = cleaned.replace(/```json|```/gi, '').trim();
      }

      this.logger.debug(`🧩 Cleaned response: ${cleaned}`);

      try {
        const parsed: unknown = JSON.parse(cleaned);

        if (
          parsed &&
          typeof parsed === 'object' &&
          'phrases' in parsed &&
          'translations' in parsed &&
          Array.isArray(parsed.phrases) &&
          Array.isArray(parsed.translations)
        ) {
          this.logger.debug(`✅ Parsed structured response for "${object}"`);
          return parsed as { phrases: string[]; translations: string[] };
        }
      } catch (_err) {
        this.logger.warn(`⚠️ Could not parse response as JSON for "${object}". Returning fallback.`);
      }

      return {
        phrases: [`This is a ${object}.`, `The ${object} is on the table.`],
        translations:
          language === 'pt'
            ? ['Isto é um(a) ' + object + '.', 'O(a) ' + object + ' está na mesa.']
            : [`This is a ${object}.`, `The ${object} is on the table.`],
      };
    } catch (error) {
      const errorMessage = error instanceof Error ? error.message : 'Unknown error';
      this.logger.error(`❌ Error calling Ollama: ${errorMessage}`);
      return {
        phrases: [`This is a ${object}.`, `The ${object} is being used.`],
        translations:
          language === 'pt'
            ? ['Isto é um(a) ' + object + '.', 'O(a) ' + object + ' está sendo usado(a).']
            : [`This is a ${object}.`, `The ${object} is being used.`],
      };
    }
  }
}
