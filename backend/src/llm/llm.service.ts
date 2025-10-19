import { Injectable, Logger, Inject } from '@nestjs/common';
import { CACHE_MANAGER } from '@nestjs/cache-manager';
import { Cache } from 'cache-manager';
import axios from 'axios';

@Injectable()
export class LlmService {
  private readonly logger = new Logger(LlmService.name);
  private readonly ollamaUrl =
    process.env.OLLAMA_URL || 'http://localhost:11434/api/chat';
  private readonly model = process.env.OLLAMA_MODEL || 'phi3';

  constructor(@Inject(CACHE_MANAGER) private cacheManager: Cache) {}

  async generateSentences(object: string): Promise<string[]> {
    const cached = await this.cacheManager.get<string[]>(object);
    if (cached) {
      this.logger.debug(`💾 Cache hit for "${object}"`);
      return cached;
    }

    this.logger.debug(`🚀 Cache miss for "${object}" — calling Ollama...`);

    const sentences = await this.callOllama(object);

    await this.cacheManager.set(object, sentences, 86400);

    return sentences;
  }

  private async callOllama(object: string): Promise<string[]> {
    const prompt = `Generate two EXTREMELY simple sentences including "${object}", for English learners who have never spoken English before. Output ONLY a valid JSON array like ["Sentence one.","Sentence two."].`;

    try {
      const response = await axios.post(this.ollamaUrl, {
        model: this.model,
        messages: [
          {
            role: 'user',
            content: prompt,
          },
        ],
        stream: false,
      });

      let text =
        response.data.message?.content ||
        response.data.response?.trim() ||
        '';

      this.logger.debug(`🧠 LLM raw response for "${object}": ${text}`);

      let cleaned = text.trim();
      if (cleaned.startsWith('```')) {
        cleaned = cleaned.replace(/```json|```/gi, '').trim();
      }

      this.logger.debug(`🧩 Cleaned response for "${object}": ${cleaned}`);

      try {
        const parsed = JSON.parse(cleaned);

        const sentences = Array.isArray(parsed[0]) ? parsed[0] : parsed;

        if (Array.isArray(sentences)) {
          this.logger.debug(`✅ Parsed sentences for "${object}": ${sentences}`);
          return sentences;
        }
      } catch (err) {
        this.logger.warn(
          `⚠️ Response was not valid JSON even after cleaning. Returning fallback for "${object}".`,
        );
      }

      return [
        `A ${object} was mentioned in a conversation.`,
        `Someone used the ${object} recently.`,
      ];
    } catch (error) {
      this.logger.error(
        `❌ Error calling Ollama for "${object}": ${error.message}`,
      );
      return [
        `This is a ${object}.`,
        `The ${object} is being used.`,
      ];
    }
  }
}
