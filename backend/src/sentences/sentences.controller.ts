import { Controller, Post, Body } from '@nestjs/common';
import { SentencesService } from './sentences.service';

@Controller('sentences')
export class SentencesController {
  constructor(private readonly sentencesService: SentencesService) {}

  @Post()
  getSentences(@Body('object') object: string) {
    const sentences = this.sentencesService.getSentences(object);
    return { sentences };
  }
}