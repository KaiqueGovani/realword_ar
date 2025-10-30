import { Injectable } from '@nestjs/common';
import { LlmService } from '../llm/llm.service';

@Injectable()
export class SentencesService {
  constructor(private readonly llmService: LlmService) {}

  async generateSentences(object: string, language: string) {
    return this.llmService.generateSentences(object, language);
  }
}
