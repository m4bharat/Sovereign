export interface AiConversationDecisionRequest {
  userId: string;
  contactId: string;
  message: string;
  relationshipRole: string;
}

export interface AiDecisionResponse {
  action: string;
  reply: string;
  memoryKey: string;
  memoryValue: string;
  summary: string;
  confidence: number;
}
