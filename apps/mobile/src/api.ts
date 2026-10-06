import { Platform } from "react-native";

export function apiBaseUrl() {
  const configured = process.env.EXPO_PUBLIC_API_URL ?? "http://localhost:8080";
  if (Platform.OS === "android" && configured.includes("localhost")) {
    return configured.replace("localhost", "10.0.2.2");
  }
  return configured;
}

export class ApiError extends Error {
  constructor(
    public status: number,
    public code: string,
    public balanceCents?: number,
    public priceCents?: number,
  ) {
    super(code);
  }
}

type RequestOptions = {
  method?: string;
  token?: string | null;
  body?: unknown;
};

export async function api<T>(path: string, options: RequestOptions = {}): Promise<T> {
  const headers: Record<string, string> = {};
  if (options.body !== undefined) {
    headers["Content-Type"] = "application/json";
  }
  if (options.token) {
    headers.Authorization = `Bearer ${options.token}`;
  }

  let response: Response;
  try {
    response = await fetch(`${apiBaseUrl()}${path}`, {
      method: options.method ?? "GET",
      headers,
      body: options.body !== undefined ? JSON.stringify(options.body) : undefined,
    });
  } catch {
    throw new ApiError(0, "network");
  }

  const text = await response.text();
  const data = text ? JSON.parse(text) : null;
  if (!response.ok) {
    throw new ApiError(response.status, data?.code ?? "request_failed", data?.balanceCents, data?.priceCents);
  }
  return data as T;
}

export type AuthResponse = {
  accessToken: string;
  expiresIn: number;
};

export type WalletResponse = {
  balanceCents: number;
};

export type LaundryService = {
  id: string;
  name: string;
  priceCents: number;
};

export type PurchaseResponse = {
  serviceId: string;
  serviceName: string;
  priceCents: number;
  balanceCents: number;
};
