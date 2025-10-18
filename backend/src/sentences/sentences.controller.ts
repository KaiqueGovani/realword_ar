import { Controller, Post, Body } from '@nestjs/common';
import { LlmService } from '../llm/llm.service';

@Controller('sentences')
export class SentencesController {
  constructor(private readonly llmService: LlmService) {}

  @Post()
  async createSentence(@Body() body: { object: string }) {
    const { object } = body;
    console.log('Received object:', object);

    const sentences = await this.llmService.generateSentences(object);

    return { sentences };
  }
}