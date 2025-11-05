import { Injectable } from '@nestjs/common';
import { LlmService } from '../llm/llm.service';
import { CreateSentenceDto } from './dto/create-sentence.dto';

@Injectable()
export class SentencesService {
  public constructor(private readonly llmService: LlmService) {}

  public async generateSentences(params: CreateSentenceDto) {
    return this.llmService.generateSentences(params);
  }
}
