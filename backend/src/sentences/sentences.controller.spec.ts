import { Test, TestingModule } from '@nestjs/testing';
import { SentencesController } from './sentences.controller';

describe('SentencesController', () => {
  let controller: SentencesController;

  beforeEach(async () => {
    const module: TestingModule = await Test.createTestingModule({
      controllers: [SentencesController],
    }).compile();

    controller = module.get<SentencesController>(SentencesController);
  });

  it('should be defined', () => {
    expect(controller).toBeDefined();
  });
});
