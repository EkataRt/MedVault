export interface User {
  email: string;
  id: string;
  password?: string;
  username: string;
}

export interface AuthResponse {
  token: string;
  user: User;
}

export interface LoginRequest {
  password: string;
  username: string;
}

export interface SignupRequest {
  email: string;
  password: string;
  username: string;
}
