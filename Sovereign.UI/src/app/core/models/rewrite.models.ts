export interface RewriteMessageRequest {
  userId: string;
  contactId: string;
  draft: string;
  relationshipRole: string;
  goal: string;
  platform: string;
}

export interface MessageRewriteVariant {
  stance: string;
  message: string;
}

export interface RewriteMessageResponse {
  variants: MessageRewriteVariant[];
}
