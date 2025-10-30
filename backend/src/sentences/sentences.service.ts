import { Injectable } from '@nestjs/common';
import { LlmService } from '../llm/llm.service';

@Injectable()
export class SentencesService {
  constructor(private readonly llmService: LlmService) {}

  async generateSentences(object: string, language: string) {
    const result = await this.llmService.generateSentences(object, language);
    return result;
  }
}
