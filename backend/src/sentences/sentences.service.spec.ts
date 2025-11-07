import { Test, TestingModule } from '@nestjs/testing';
import { SentencesService } from './sentences.service';
import { LlmService } from '../llm/llm.service';
import { CreateSentenceDto } from './dto/create-sentence.dto';

describe('SentencesService', () => {
  let service: SentencesService;
  let llm: { generateSentences: jest.Mock };

  beforeEach(async () => {
    llm = { generateSentences: jest.fn() };
    const module: TestingModule = await Test.createTestingModule({
      providers: [
        SentencesService,
        {
          provide: LlmService,
          useValue: llm,
        },
      ],
    }).compile();

    service = module.get<SentencesService>(SentencesService);
  });

  it('should be defined', () => {
    expect(service).toBeDefined();
  });

  it('delegates to LlmService.generateSentences', async () => {
    const dto: CreateSentenceDto = { object: 'book', language: 'pt' };
    llm.generateSentences.mockResolvedValue({ phrases: ['a', 'b'], translations: ['c', 'd'] });
    const res = await service.generateSentences(dto);
    expect(llm.generateSentences).toHaveBeenCalledWith(dto);
    expect(res.phrases).toHaveLength(2);
  });
});
