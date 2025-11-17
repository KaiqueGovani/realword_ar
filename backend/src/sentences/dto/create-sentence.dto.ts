import { ApiProperty } from '@nestjs/swagger';
import { IsNotEmpty, IsOptional, IsString } from 'class-validator';

export class CreateSentenceDto {
  @ApiProperty({
    description: 'The object or item to generate sentences about',
    example: 'chair',
  })
  @IsString()
  @IsNotEmpty()
  public object: string;

  @ApiProperty({
    description: 'The target language for the sentences',
    example: 'português',
    default: 'português',
    required: false,
  })
  @IsOptional()
  @IsString()
  public language?: string = 'português';
}
