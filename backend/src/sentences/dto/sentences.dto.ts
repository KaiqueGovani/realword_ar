import { ApiProperty } from '@nestjs/swagger';
import { IsString } from 'class-validator';

export class SentencesDto {
  @ApiProperty({
    description: 'Array of generated sentences in the target language',
    example: ['Esta é uma cadeira confortável', 'A cadeira está na sala'],
    type: [String],
  })
  @IsString({ each: true })
  public phrases: string[];

  @ApiProperty({
    description: 'Array of translations for the generated sentences',
    example: ['This is a comfortable chair', 'The chair is in the room'],
    type: [String],
  })
  @IsString({ each: true })
  public translations: string[];
}
