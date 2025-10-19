import { Module } from '@nestjs/common';
import { CacheModule } from '@nestjs/cache-manager';
import { AppController } from './app.controller';
import { AppService } from './app.service';
import { SentencesModule } from './sentences/sentences.module';
import { LlmModule } from './llm/llm.module';

@Module({
  imports: [SentencesModule, LlmModule, CacheModule.register({ isGlobal: true, ttl: 86400 })],
  controllers: [AppController],
  providers: [AppService],
})
export class AppModule {}
