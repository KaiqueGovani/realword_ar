import { CACHE_MANAGER } from '@nestjs/cache-manager';
import { Test, TestingModule } from '@nestjs/testing';
import axios from 'axios';
import { LlmService } from './llm.service';
import { CreateSentenceDto } from 'src/sentences/dto/create-sentence.dto';
import { SentencesDto } from 'src/sentences/dto/sentences.dto';

jest.mock('axios');

describe('LlmService', () => {
  let service: LlmService;
  let cache: Map<string, any>;

  const buildModule = async () => {
    cache = new Map<string, any>();
    return Test.createTestingModule({
      providers: [
        LlmService,
        {
          provide: CACHE_MANAGER,
          useValue: {
            get: (key: string) => Promise.resolve(cache.get(key)),
            set: (key: string, value: any) => {
              cache.set(key, value);
              return Promise.resolve();
            },
          },
        },
      ],
    }).compile();
  };

  beforeEach(async () => {
    jest.resetAllMocks();
    process.env.GOOGLE_API_KEY = 'test-key';
    const module: TestingModule = await buildModule();
    service = module.get<LlmService>(LlmService);
  });

  const makeDto = (overrides: Partial<CreateSentenceDto> = {}): CreateSentenceDto => ({
    object: 'ball',
    language: 'pt',
    ...overrides,
  });

  it('should be defined', () => {
    expect(service).toBeDefined();
  });

  it('returns cached value when present (cache hit)', async () => {
    const dto = makeDto();
    const expected: SentencesDto = {
      phrases: ['cached 1', 'cached 2'],
      translations: ['trad 1', 'trad 2'],
    };
    cache.set(`${dto.object}_${dto.language}`, expected);

    const result = await service.generateSentences(dto);

    expect(result).toEqual(expected);
    expect(axios.post).not.toHaveBeenCalled();
  });

  it('calls external API on cache miss and caches result (happy path)', async () => {
    const dto = makeDto();
    const rawJson = JSON.stringify({
      phrases: ['This is a ball.', 'The ball is red.'],
      translations: ['Isto é uma bola.', 'A bola é vermelha.'],
    });
    (axios.post as jest.Mock).mockResolvedValue({
      data: {
        candidates: [
          {
            content: {
              parts: [
                {
                  text: rawJson,
                },
              ],
            },
          },
        ],
      },
    });

    const result = await service.generateSentences(dto);

    expect(result.phrases.length).toBe(2);
    expect(axios.post).toHaveBeenCalledTimes(1);
    expect(cache.get(`${dto.object}_${dto.language}`)).toEqual(result);
  });

  it('parses fenced JSON responses (cleanResponse)', async () => {
    const dto = makeDto();
    const fenced = '```json\n{"phrases":["This is a ball.","The ball is blue."],"translations":["Isto é uma bola.","A bola é azul."]}\n```';
    (axios.post as jest.Mock).mockResolvedValue({
      data: {
        candidates: [
          {
            content: {
              parts: [
                {
                  text: fenced,
                },
              ],
            },
          },
        ],
      },
    });

    const result = await service.generateSentences(dto);
    expect(result.translations[1]).toContain('azul');
  });

  it('falls back when response is not valid JSON', async () => {
    const dto = makeDto({ object: 'chair' });
    (axios.post as jest.Mock).mockResolvedValue({
      data: {
        candidates: [
          {
            content: {
              parts: [
                {
                  text: 'not-json-here',
                },
              ],
            },
          },
        ],
      },
    });

    const result = await service.generateSentences(dto);
    expect(result.phrases[0]).toMatch(/chair/);
    expect(result.phrases[0]).toContain('chair');
  });

  it('falls back when API key is missing (sendGoogleRequest throws)', async () => {
    delete process.env.GOOGLE_API_KEY;
    const module = await buildModule();
    service = module.get<LlmService>(LlmService);
    const dto = makeDto({ object: 'lamp' });
    const result = await service.generateSentences(dto);
    expect(result.phrases[0]).toContain('lamp');
    expect(axios.post).not.toHaveBeenCalled();
  });

  describe('internal helpers', () => {
    it('extractText chooses candidates first, then output_text fallback', () => {
      const svc: any = service;
      const candidateData = {
        candidates: [
          {
            content: { parts: [{ text: 'candidate-text' }] },
          },
        ],
      };
      expect(svc.extractText(candidateData)).toBe('candidate-text');
      const outputTextData = { output_text: 'output-text' };
      expect(svc.extractText(outputTextData)).toBe('output-text');
      const emptyData = {};
      expect(svc.extractText(emptyData)).toBe('');
    });

    it('cleanResponse strips markdown fences', () => {
      const svc: any = service;
      expect(svc.cleanResponse('```json\n{"a":1}\n```')).toBe('{"a":1}');
      expect(svc.cleanResponse('{"a":1}')).toBe('{"a":1}');
    });

    it('parseResponse returns null for invalid JSON and SentencesDto for valid', () => {
      const svc: any = service;
      const dtoValid = JSON.stringify({ phrases: ['a', 'b'], translations: ['c', 'd'] });
      expect(svc.parseResponse(dtoValid, 'obj')).toEqual({ phrases: ['a', 'b'], translations: ['c', 'd'] });
      expect(svc.parseResponse('not-json', 'obj')).toBeNull();
    });
  });
});

