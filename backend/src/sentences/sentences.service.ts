import { Injectable } from '@nestjs/common';

@Injectable()
export class SentencesService {
  getSentences(object: string): string[] {
    return [
      `This is a ${object}.`,
      `The ${object} is on the table.`,
    ];
  }
}