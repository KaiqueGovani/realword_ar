import { BadRequestException, Body, Controller, Post } from '@nestjs/common';
import { CreateSentenceDto } from './dto/create-sentence.dto';
import { SentencesService } from './sentences.service';

@Controller('sentences')
export class SentencesController {
  public constructor(private readonly sentencesService: SentencesService) {}

  @Post()
  public async create(@Body() body: CreateSentenceDto) {
    if (!body || !body.object) {
      throw new BadRequestException('Missing required field: object');
    }

    const object = body.object.trim();
    const language = (body.language || 'en').trim().toLowerCase();

    const result = await this.sentencesService.generateSentences(object, language);
    return result;
  }
}
