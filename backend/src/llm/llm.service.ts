import { CACHE_MANAGER } from '@nestjs/cache-manager';
import { Inject, Injectable, Logger } from '@nestjs/common';
import axios from 'axios';
import type { Cache } from 'cache-manager';
import { CreateSentenceDto } from 'src/sentences/dto/create-sentence.dto';
import { SentencesDto } from 'src/sentences/dto/sentences.dto';
import { LLMResponse } from './llm.interface';

@Injectable()
export class LlmService {
  private readonly logger = new Logger(LlmService.name);
  private readonly ollamaUrl = process.env.OLLAMA_URL || 'http://localhost:11434/api/chat';
  private readonly model = process.env.OLLAMA_MODEL || 'phi3';

  public constructor(@Inject(CACHE_MANAGER) private readonly cacheManager: Cache) {}

  public async generateSentences(params: CreateSentenceDto): Promise<SentencesDto> {
    const { object, language } = params;

    const cacheKey = `${object}_${language}`;
    const cached = await this.cacheManager.get(cacheKey);

    if (cached) {
      this.logger.debug(`💾 Cache hit for "${cacheKey}"`);
      return cached as SentencesDto;
    }

    this.logger.debug(`🚀 Cache miss for "${cacheKey}" — calling Ollama...`);
    const result = await this.callOllama(params);

    await this.cacheManager.set(cacheKey, result, 86400);
    return result;
  }

  private async callOllama(params: CreateSentenceDto): Promise<SentencesDto> {
    const { object } = params;
    const prompt = this.createPrompt(params);

    try {
      const rawResponse = await this.sendOllamaRequest(prompt);
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
      this.logger.error(`❌ Error calling Ollama: ${errorMessage}`);
      return this.generateFallbackSentences(params);
    }
  }

  private async sendOllamaRequest(prompt: string): Promise<string> {
    const response = await axios.post<LLMResponse>(this.ollamaUrl, {
      model: this.model,
      messages: [{ role: 'user', content: prompt }],
      stream: false,
      options: {
        temperature: 0.2,
      },
    });

    return response.data.message?.content || response.data.response?.trim() || '';
  }

  private cleanResponse(rawText: string): string {
    const text = rawText.trim();

    if (text.startsWith('```')) {
      return text.replace(/```json|```/gi, '').trim();
    }

    return text;
  }

  private parseResponse(text: string, object: string): SentencesDto | null {
    this.logger.debug(`� LLM raw response for "${object}": ${text}`);

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
