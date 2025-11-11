import { INestApplication } from '@nestjs/common';
import { Test, TestingModule } from '@nestjs/testing';
import * as request from 'supertest';
import { App } from 'supertest/types';
import axios from 'axios';
import { AppModule } from './../src/app.module';

jest.mock('axios');

describe('Sentences (e2e)', () => {
  let app: INestApplication<App>;

  beforeAll(async () => {
    process.env.GOOGLE_API_KEY = process.env.GOOGLE_API_KEY || 'test-api-key';

    const moduleFixture: TestingModule = await Test.createTestingModule({
      imports: [AppModule],
    }).compile();

    app = moduleFixture.createNestApplication();
    await app.init();
  });

  beforeEach(() => {
    jest.clearAllMocks();
  });

  afterAll(async () => {
    await app.close();
  });

  const postSentences = (payload: any) => (request as any)(app.getHttpServer()).post('/sentences').send(payload);

  it('POST /sentences - happy path returns generated sentences', async () => {
    const raw = JSON.stringify({
      phrases: ['This is a key.', 'The key is small.'],
      translations: ['Isto é uma chave.', 'A chave é pequena.'],
    });
    (axios.post as jest.Mock).mockResolvedValueOnce({
      data: {
        candidates: [
          {
            content: { parts: [{ text: raw }] },
          },
        ],
      },
    });

    const res = await postSentences({ object: 'key', language: 'pt' });
    expect([200, 201]).toContain(res.status);
    expect(res.body.phrases).toHaveLength(2);
  });

  it('POST /sentences - cache prevents a second axios call', async () => {
    (axios.post as jest.Mock).mockResolvedValueOnce({
      data: {
        candidates: [
          {
            content: {
              parts: [
                {
                  text: JSON.stringify({
                    phrases: ['This is a ball.', 'The ball is red.'],
                    translations: ['Isto é uma bola.', 'A bola é vermelha.'],
                  }),
                },
              ],
            },
          },
        ],
      },
    });

    const payload = { object: 'ball', language: 'pt' };
    const res1 = await postSentences(payload);
    const res2 = await postSentences(payload);
    expect([200, 201]).toContain(res1.status);
    expect([200, 201]).toContain(res2.status);
    expect((axios.post as jest.Mock).mock.calls.length).toBe(1);
  });

  it('POST /sentences - timeout leads to fallback sentences', async () => {
    (axios.post as jest.Mock).mockRejectedValueOnce({ code: 'ECONNABORTED', message: 'timeout' });

    const res = await postSentences({ object: 'chair', language: 'pt' });
    expect([200, 201]).toContain(res.status);
    expect(res.body.phrases[0]).toMatch(/chair/i);
    expect(res.body.translations[0]).toMatch(/Isto é/);
  });

  it('POST /sentences - generic error returns fallback', async () => {
    (axios.post as jest.Mock).mockRejectedValueOnce(new Error('boom'));

    const res = await postSentences({ object: 'lamp', language: 'pt' });
    expect([200, 201]).toContain(res.status);
    expect(res.body.phrases[0]).toMatch(/lamp/i);
  });

  it('POST /sentences - invalid JSON returns fallback', async () => {
    (axios.post as jest.Mock).mockResolvedValueOnce({
      data: {
        candidates: [
          {
            content: { parts: [{ text: 'not-json' }] },
          },
        ],
      },
    });

    const res = await postSentences({ object: 'door', language: 'pt' });
    expect([200, 201]).toContain(res.status);
    expect(res.body.phrases[0]).toMatch(/door/i);
  });
});
