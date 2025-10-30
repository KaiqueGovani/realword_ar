import { IsNotEmpty, IsOptional, IsString } from 'class-validator';

export class CreateSentenceDto {
  @IsString()
  @IsNotEmpty()
  public object: string;

  @IsOptional()
  @IsString()
  public language?: string;
}
