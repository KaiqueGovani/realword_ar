import { Body, Controller, Post } from '@nestjs/common';
import { ApiOperation, ApiResponse, ApiTags } from '@nestjs/swagger';
import { CreateSentenceDto } from './dto/create-sentence.dto';
import { SentencesDto } from './dto/sentences.dto';
import { SentencesService } from './sentences.service';

@ApiTags('sentences')
@Controller('sentences')
export class SentencesController {
  public constructor(private readonly sentencesService: SentencesService) {}

  @Post()
  @ApiOperation({
    summary: 'Generate sentences about an object',
    description: 'Creates sentences and translations for a given object in the specified language',
  })
  @ApiResponse({
    status: 201,
    description: 'Sentences successfully generated',
    type: SentencesDto,
  })
  @ApiResponse({
    status: 400,
    description: 'Bad request - invalid input data',
  })
  public async create(@Body() body: CreateSentenceDto) {
    return this.sentencesService.generateSentences(body);
  }
}
