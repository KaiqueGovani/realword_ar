import { Controller, Post, Body } from '@nestjs/common';
import { SentencesService } from './sentences.service';

@Controller('sentences')
export class SentencesController {
  constructor(private readonly sentencesService: SentencesService) {}

  @Post()
  getSentences(@Body('object') object: string) {
    console.log('Received object:', object);
    const sentences = this.sentencesService.getSentences(object || 'object');
    return { sentences };
  }
}