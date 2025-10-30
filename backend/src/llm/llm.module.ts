import { HttpModule } from '@nestjs/axios';
import { CacheModule } from '@nestjs/cache-manager';
import { Module } from '@nestjs/common';
import { LlmService } from './llm.service';

@Module({
  imports: [HttpModule, CacheModule.register({ isGlobal: true, ttl: 86400 })],
  providers: [LlmService],
  exports: [LlmService],
})
export class LlmModule {}
