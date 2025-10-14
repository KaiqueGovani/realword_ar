import { Injectable, Logger } from '@nestjs/common';
import axios from 'axios';

@Injectable()
export class LlmService {
  private readonly logger = new Logger(LlmService.name);
  private readonly ollamaUrl = process.env.OLLAMA_URL || 'http://localhost:11434/api/chat';
  private readonly model = process.env.OLLAMA_MODEL || 'phi3';

  async generateSentences(object: string): Promise<string[]> {
    const prompt = `Generate two EXTREMELY simple sentences including "${object}", for English learners. Output ONLY a valid JSON array like ["Sentence one.","Sentence two."].`;

    this.logger.debug(`🚀 Sending request to Ollama for "${object}"...`);

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
        const sentences = JSON.parse(cleaned);
        if (Array.isArray(sentences[0])) {
          return sentences[0];
        }
        if (Array.isArray(sentences)){
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
