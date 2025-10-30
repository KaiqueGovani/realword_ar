import { Module } from '@nestjs/common';
import { AppController } from './app.controller';
import { AppService } from './app.service';
import { LlmModule } from './llm/llm.module';
import { SentencesModule } from './sentences/sentences.module';

@Module({
  imports: [SentencesModule, LlmModule],
  controllers: [AppController],
  providers: [AppService],
})
export class AppModule {}
