import { Test, TestingModule } from '@nestjs/testing';
import { SentencesService } from './sentences.service';

describe('SentencesService', () => {
  let service: SentencesService;

  beforeEach(async () => {
    const module: TestingModule = await Test.createTestingModule({
      providers: [SentencesService],
    }).compile();

    service = module.get<SentencesService>(SentencesService);
  });

  it('should be defined', () => {
    expect(service).toBeDefined();
  });
});
