import { Body, Controller, Post, BadRequestException } from '@nestjs/common';
import { SentencesService } from './sentences.service';
import { CreateSentenceDto } from './dto/create-sentence.dto';

@Controller('sentences')
export class SentencesController {
  constructor(private readonly sentencesService: SentencesService) {}

  @Post()
  async create(@Body() body: CreateSentenceDto) {
    if (!body || !body.object) {
      throw new BadRequestException('Missing required field: object');
    }

    const object = body.object.trim();
    const language = (body.language || 'en').trim().toLowerCase();

    const result = await this.sentencesService.generateSentences(object, language);
    return result;
  }
}