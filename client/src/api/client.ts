import { Api } from "./Api";
import { getToken } from "./token";

export const api = new Api({
  baseUrl: process.env.BUN_PUBLIC_API_URL ?? "http://localhost:5194",
  securityWorker: () => {
    const t = getToken();
    return t ? { headers: { Authorization: `Bearer ${t}` } } : {};
  },
});