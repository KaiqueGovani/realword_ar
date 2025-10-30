import { Body, Controller, Post } from '@nestjs/common';
import { CreateSentenceDto } from './dto/create-sentence.dto';
import { SentencesService } from './sentences.service';

@Controller('sentences')
export class SentencesController {
  public constructor(private readonly sentencesService: SentencesService) {}

  @Post()
  public async create(@Body() body: CreateSentenceDto) {
    return this.sentencesService.generateSentences(body);
  }
}
