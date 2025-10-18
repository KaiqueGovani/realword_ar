import { Injectable } from '@nestjs/common';
import { LlmService } from 'src/llm/llm.service';

@Injectable()
export class SentencesService {
  constructor(private readonly llmService: LlmService) {}

  getSentences(object: string): string[] {
    return [
      `This is a ${object}.`,
      `The ${object} is on the table.`,
    ];
  }

  async generateFor(object: string){
    if (!object) return [];
    return await this.llmService.generateSentences(object);
  }
}