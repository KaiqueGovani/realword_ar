import { Test, TestingModule } from '@nestjs/testing';
import { SentencesController } from './sentences.controller';
import { SentencesService } from './sentences.service';
import { CreateSentenceDto } from './dto/create-sentence.dto';

describe('SentencesController', () => {
  let controller: SentencesController;

  let sentencesService: { generateSentences: jest.Mock };

  beforeEach(async () => {
    sentencesService = { generateSentences: jest.fn() };
    const module: TestingModule = await Test.createTestingModule({
      controllers: [SentencesController],
      providers: [
        {
          provide: SentencesService,
          useValue: sentencesService,
        },
      ],
    }).compile();

    controller = module.get<SentencesController>(SentencesController);
  });

  it('should be defined', () => {
    expect(controller).toBeDefined();
  });

  it('delegates create to service.generateSentences', async () => {
    const dto: CreateSentenceDto = { object: 'phone', language: 'pt' };
    sentencesService.generateSentences.mockResolvedValue({ phrases: ['a'], translations: ['b'] });
    const res = await controller.create(dto);
    expect(sentencesService.generateSentences).toHaveBeenCalledWith(dto);
    expect(res.phrases).toEqual(['a']);
  });
});
