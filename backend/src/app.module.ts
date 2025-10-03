import { Module } from '@nestjs/common';
import { AppController } from './app.controller';
import { AppService } from './app.service';
import { SentencesModule } from './sentences/sentences.module';

@Module({
  imports: [SentencesModule],
  controllers: [AppController],
  providers: [AppService],
})
export class AppModule {}
