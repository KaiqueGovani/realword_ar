import { Module } from '@nestjs/common';
import { APP_FILTER } from '@nestjs/core';
import { SentryGlobalFilter, SentryModule } from '@sentry/nestjs/setup';
import { LlmModule } from './llm/llm.module';
import { SentencesModule } from './sentences/sentences.module';

@Module({
  imports: [SentryModule.forRoot(), SentencesModule, LlmModule],
  controllers: [],
  providers: [
    {
      provide: APP_FILTER,
      useClass: SentryGlobalFilter,
    },
  ],
})
export class AppModule {}
