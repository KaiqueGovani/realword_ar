import { validate } from 'class-validator';
import { CreateSentenceDto } from './create-sentence.dto';

describe('CreateSentenceDto', () => {
  it('validates a correct dto', async () => {
    const dto = new CreateSentenceDto();
    dto.object = 'ball';
    dto.language = 'pt';
    const errors = await validate(dto);
    expect(errors).toHaveLength(0);
  });

  it('applies default language when not provided', async () => {
    const dto = new CreateSentenceDto();
    dto.object = 'tree';
    const errors = await validate(dto);
    expect(errors).toHaveLength(0);
    expect(dto.language).toBe('português');
  });

  it('fails validation when object missing', async () => {
    const dto = new CreateSentenceDto();
    const errors = await validate(dto);
    expect(errors.length).toBeGreaterThan(0);
    expect(errors[0].property).toBe('object');
  });
});
