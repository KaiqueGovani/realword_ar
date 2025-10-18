import { Module } from '@nestjs/common';
import { AppController } from './app.controller';
import { AppService } from './app.service';
import { SentencesModule } from './sentences/sentences.module';
import { LlmModule } from './llm/llm.module';

@Module({
  imports: [SentencesModule, LlmModule],
  controllers: [AppController],
  providers: [AppService],
})
export class AppModule {}
