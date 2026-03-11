export interface RegisterRequest {
  email: string;
  password: string;
  tenantId: string;
}

export interface LoginRequest {
  email: string;
  password: string;
  tenantId: string;
}

export interface LoginResponse {
  token: string;
  userId: string;
  email: string;
  tenantId: string;
}

export interface AuthMeResponse {
  userId: string;
  email: string;
  tenantId: string;
}
