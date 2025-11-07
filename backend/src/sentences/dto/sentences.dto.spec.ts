import { validate } from 'class-validator';
import { SentencesDto } from './sentences.dto';

describe('SentencesDto', () => {
  it('validates correct arrays', async () => {
    const dto = new SentencesDto();
    dto.phrases = ['a', 'b'];
    dto.translations = ['c', 'd'];
    const errors = await validate(dto);
    expect(errors).toHaveLength(0);
  });

  it('fails when phrases not array of strings', async () => {
    const dto: any = new SentencesDto();
    dto.phrases = ['a', 2];
    dto.translations = ['c', 'd'];
    const errors = await validate(dto);
    expect(errors.length).toBeGreaterThan(0);
    const phrasesErr = errors.find(e => e.property === 'phrases');
    expect(phrasesErr).toBeDefined();
  });

  it('fails when translations not array of strings', async () => {
    const dto: any = new SentencesDto();
    dto.phrases = ['a', 'b'];
    dto.translations = ['c', 5];
    const errors = await validate(dto);
    expect(errors.length).toBeGreaterThan(0);
    const translationsErr = errors.find(e => e.property === 'translations');
    expect(translationsErr).toBeDefined();
  });
});
