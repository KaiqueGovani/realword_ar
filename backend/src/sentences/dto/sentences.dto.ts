import { IsString } from 'class-validator';

export class SentencesDto {
  @IsString({ each: true })
  public phrases: string[];

  @IsString({ each: true })
  public translations: string[];
}
