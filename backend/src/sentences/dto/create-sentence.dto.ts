import { IsString, IsOptional, IsNotEmpty } from 'class-validator';

export class CreateSentenceDto {
  @IsString()
  @IsNotEmpty()
  object: string;

  @IsOptional()
  @IsString()
  language?: string;
}