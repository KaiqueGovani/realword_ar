import { Module } from '@nestjs/common';
import { SentencesController } from './sentences.controller';
import { SentencesService } from './sentences.service';
import { LlmModule } from 'src/llm/llm.module';
import { LlmService } from 'src/llm/llm.service';

@Module({
  imports: [LlmModule],
  controllers: [SentencesController],
  providers: [SentencesService, LlmService],
})
export class SentencesModule {}
