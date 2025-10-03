import { Module } from '@nestjs/common';
import { SentencesController } from './sentences.controller';
import { SentencesService } from './sentences.service';

@Module({
  controllers: [SentencesController],
  providers: [SentencesService]
})
export class SentencesModule {}