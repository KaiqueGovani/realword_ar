export interface LLMResponseCandidatePart {
  text?: string;
}

export interface LLMResponseCandidateContent {
  parts?: LLMResponseCandidatePart[];
}

export interface LLMResponseCandidate {
  content?: LLMResponseCandidateContent;
}

export interface LLMResponse {
  candidates?: LLMResponseCandidate[];
  output_text?: string;
  text?: string;
  error?: { message?: string };
}
