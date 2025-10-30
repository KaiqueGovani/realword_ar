export interface LLMMessage {
  content: string;
}

export interface LLMResponse {
  message?: LLMMessage;
  response?: string;
}